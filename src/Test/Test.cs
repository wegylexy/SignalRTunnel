using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlyByWireless.SignalRTunnel.Test
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public interface ITestClient
    {
        Task ClientMethod1(string a);

        Task MsgPackTimeVoid(DateTime a);
    }

    sealed class TestHub : Hub<ITestClient>
    {
        static readonly Action<ILogger, string, Exception?>
            _logConnected = LoggerMessage.Define<string>(LogLevel.Information,
                new(1, nameof(OnConnectedAsync)),
                "Connected: {ConnectionId}"
            ),
            _logDisconnected = LoggerMessage.Define<string>(LogLevel.Information,
                new(1, nameof(OnDisconnectedAsync)),
                "Disconnected: {ConnectionId}"
            );

        readonly ILogger<TestHub> _logger;

        public TestHub(ILogger<TestHub> logger) => _logger = logger;

        public override Task OnConnectedAsync()
        {
            _logConnected(_logger, Context.ConnectionId, null);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logDisconnected(_logger, Context.ConnectionId, exception);
            return Task.CompletedTask;
        }

        public Task HubMethod1(string a) => Clients.Caller.ClientMethod1(a);

#pragma warning disable CA1822 // Mark members as static
        [Authorize(Roles = "A")]
        public Task AOnly() => Task.CompletedTask;

        [Authorize(Roles = "B,C")]
        public Task BOrCOnly() => Task.CompletedTask;
#pragma warning restore CA1822 // Mark members as static

        public Task Abort()
        {
            Context.Abort();
            return Task.CompletedTask;
        }
    }
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

    public class Test
    {
        [Fact]
        public async Task TunnelAsync()
        {
            using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddSignalR().AddJsonProtocol().AddMessagePackProtocol()
            ).Build();
            await app.StartAsync();
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            using (clientStream)
            using (serverStream)
            {
                var client = new HubConnectionBuilder()
                    .AddMessagePackProtocol()
                    .WithTunnel(clientStream)
                    .Build();
                TaskCompletionSource<string>? tcs = null;
                _ = client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs!.SetResult(a));
                var handler = app.Services.GetRequiredService<HubConnectionHandler<TestHub>>();
                var start = client.StartAsync();
                DuplexContext? connectionContext = null;
                var connection = handler.OnConnectedAsync(serverStream, user: new(new ClaimsIdentity(new Claim[]
                {
                    new(ClaimTypes.Role, "C")
                }, "Mock")), configure: c => connectionContext = c);
                Assert.NotNull(connectionContext?.ConnectionId);
                await start;
                Assert.Equal(HubConnectionState.Connected, client.State);
                {
                    var expected = Guid.NewGuid().ToString();
                    tcs = new();
                    await client.InvokeAsync(nameof(TestHub.HubMethod1), expected);
                    Assert.Equal(expected, await tcs.Task);
                }
                {
                    var hubContext = app.Services.GetRequiredService<IHubContext<TestHub, ITestClient>>();
                    var expected = Guid.NewGuid().ToString();
                    tcs = new();
                    await hubContext.Clients.All.ClientMethod1(expected);
                    Assert.Equal(expected, await tcs.Task);
                }
                await Assert.ThrowsAsync<HubException>(() => client.InvokeAsync(nameof(TestHub.AOnly)));
                await client.InvokeAsync(nameof(TestHub.BOrCOnly));
                await Assert.ThrowsAsync<TaskCanceledException>(() => client.InvokeAsync(nameof(TestHub.Abort)));
                await connection;
            }
            await app.StopAsync();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task NamedPipeAsync(int maxNumberOfServerInstances)
        {
            var pipeName = Guid.NewGuid().ToString("N");
            using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddSignalR().AddMessagePackProtocol().AddNamedPipe<TestHub>(pipeName, maxNumberOfServerInstances)
            ).Build();
            await app.StartAsync();
            async Task<HubConnection> TestClientAsync()
            {
                var client = new HubConnectionBuilder().AddMessagePackProtocol().WithNamedPipe(pipeName).Build();
                await client.StartAsync();
                {
                    var expected = Guid.NewGuid().ToString();
                    TaskCompletionSource<string> tcs = new();
                    client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs.SetResult(a));
                    await client.InvokeAsync(nameof(TestHub.HubMethod1), expected);
                    Assert.Equal(expected, await tcs.Task);
                }
                return client;
            };
            var clients = await Task.WhenAll(Enumerable.Range(0, maxNumberOfServerInstances)
                .Select(_ => TestClientAsync()));
            var extraTask = TestClientAsync();
            // TODO: test hub methods and events
            Assert.False(extraTask.IsCompleted, "Exceeded max number of server instances.");
            Assert.True(clients.All(c => c.State == HubConnectionState.Connected), "Max number of server instances not reached.");
            await Task.WhenAll(clients.Select(c => c.StopAsync()));
            var extra = await extraTask;
            Assert.True(extra.State == HubConnectionState.Connected);
            await extra.StopAsync();
            Assert.True(extra.State == HubConnectionState.Disconnected);
            await app.StopAsync();
        }

        [Fact]
        public void FunctionPointer()
        {
            TaskCompletionSource tcs = new();
            var p = new Func<nint>(() =>
            {
                GCHandle h = default;
                Action a = () =>
                {
                    tcs.SetResult();
                    h.Free();
                };
                h = GCHandle.Alloc(a);
                return Marshal.GetFunctionPointerForDelegate(a);
            })();
            GC.Collect();
            unsafe
            {
                ((delegate* unmanaged<void>)p)();
            }
            Assert.True(tcs.Task.IsCompletedSuccessfully);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData((byte)3)]
        [InlineData((byte)254)]
        [InlineData((short)259)]
        [InlineData((short)-259)]
        [InlineData(123456789)]
        [InlineData(-123456789)]
        [InlineData(1234567890123)]
        [InlineData(-1234567890123)]
        [InlineData(123.45f)]
        [InlineData(123.45)]
        [InlineData("Hello, world!")]
        public void Deserialize<T>(T? value)
        => Assert.Equal(value, Convert.ChangeType(MessagePackSerializer.Deserialize(typeof(object), MessagePackSerializer.Serialize(typeof(T), value)), typeof(T)));

        [Fact]
        public void DeserializeDateTime() => Deserialize(DateTime.UtcNow);

        [Fact]
        public void DeserializeArray()
        {
            var e = new object?[]
            {
                null,
                true, false,
                (byte)3, (byte)254, (short)259, (short)-259, 123456789, -123456789, 1234567890123, -1234567890123, 123.45f, 123.45,
                "Hello, world!",
                DateTime.UtcNow
            };
            Assert.Equal(e, ((object[])MessagePackSerializer.Deserialize(typeof(object), MessagePackSerializer.Serialize(e)))
                .Select((o, i) => Convert.ChangeType(o, e[i]?.GetType() ?? typeof(object)))
            );
        }
    }

    public class NativeTest
    {

        [Fact]
        public async Task NativeAsync()
        {
            var path = Path.Join(Environment.CurrentDirectory
                .Replace("net6.0", new Regex(@"\d+(?=-)").Replace(RuntimeInformation.RuntimeIdentifier, string.Empty))
                .Replace("Test", "NativeTest")
            , "NativeTest");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path += ".exe";
            }
            Assert.True(File.Exists(path), $"{path} does not exist.");
            var pipeName = Guid.NewGuid().ToString("N");
            using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddSignalR().AddMessagePackProtocol().AddNamedPipe<TestHub>(pipeName, 2)
            ).Build();
            await app.StartAsync();
            var context = app.Services.GetRequiredService<IHubContext<TestHub, ITestClient>>();
            using Process client = new()
            {
                StartInfo = new(path)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };
            client.StartInfo.ArgumentList.Add(pipeName);
            client.StartInfo.ArgumentList.Add(".");
            var expected = Guid.NewGuid().ToString();
            client.StartInfo.ArgumentList.Add(expected);
            Assert.True(client.Start());
            var errorTask = client.StandardError.ReadToEndAsync();
            try
            {
                using CancellationTokenSource cts = new(5000);
                using var sweeper = cts.Token.Register(() =>
                {
                    if (!client.HasExited)
                    {
                        client.Kill(true);
                    }
                });
                Assert.Equal("Ready", await client.StandardOutput.ReadLineAsync());
                await context.Clients.All.MsgPackTimeVoid(DateTime.UtcNow);
                Assert.Equal("Completed", await client.StandardOutput.ReadLineAsync());
                await client.WaitForExitAsync(cts.Token);
            }
            finally
            {
                var exited = client.HasExited;
                if (!exited)
                {
                    client.Kill(true);
                    await client.WaitForExitAsync();
                }
                var error = await errorTask;
                Assert.True(exited && client.ExitCode == default && string.IsNullOrEmpty(error),
                    $"Exited with {client.ExitCode}" + Environment.NewLine + error.TrimEnd('\n', '\r')
                );
            }
            await app.StopAsync();
        }
    }
}