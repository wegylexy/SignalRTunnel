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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

    public class ManagedTest
    {
        public enum DisconnectType
        {
            ClientStop,
            ClientAbort,
            ServerAbort,
            InvokeAbort,
            BothAbort
        }

        [Theory]
        [InlineData(DisconnectType.ClientStop)]
        [InlineData(DisconnectType.ClientAbort)]
        [InlineData(DisconnectType.ServerAbort)]
        [InlineData(DisconnectType.InvokeAbort)]
        [InlineData(DisconnectType.BothAbort)]
        public async Task TunnelAsync(DisconnectType disconnectType)
        {
            using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddSignalR().AddMessagePackProtocol()
            ).Build();
            await app.StartAsync();
            var (serverStream, clientStream) = FullDuplexStream.CreatePair();
            await using (serverStream)
            {
                var disconnection = Task.Run(async () =>
                {
                    var handler = app.Services.GetRequiredService<HubConnectionHandler<TestHub>>();
                    DuplexContext? connectionContext = null;
                    var connection = handler.OnConnectedAsync(serverStream, user: new(new ClaimsIdentity(new Claim[]
                    {
                        new(ClaimTypes.Role, "C")
                    }, "Mock")), configure: c => connectionContext = c);
                    Assert.NotNull(connectionContext?.ConnectionId);
                    await using (serverStream)
                    await using (connectionContext)
                    {
                        await connection;
                    }
                });
                await using (clientStream)
                {
                    var client = new HubConnectionBuilder()
                        .AddMessagePackProtocol()
                        .WithTunnel(clientStream)
                        .Build();
                    client.HandshakeTimeout = client.ServerTimeout = TimeSpan.FromSeconds(1);
                    await client.StartAsync();
                    Assert.Equal(HubConnectionState.Connected, client.State);
                    {
                        var expected = Guid.NewGuid().ToString();
                        TaskCompletionSource<string> tcs = new();
                        {
                            using var on = client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs.SetResult(a));
                            await client.InvokeAsync(nameof(TestHub.HubMethod1), expected).WaitAsync(TimeSpan.FromSeconds(1));
                            Assert.Equal(expected, await tcs.Task);
                        }
                    }
                    {
                        var hubContext = app.Services.GetRequiredService<IHubContext<TestHub, ITestClient>>();
                        var expected = Guid.NewGuid().ToString();
                        TaskCompletionSource<string> tcs = new();
                        {
                            using var on = client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs.SetResult(a));
                            await hubContext.Clients.All.ClientMethod1(expected).WaitAsync(TimeSpan.FromSeconds(1));
                            Assert.Equal(expected, await tcs.Task);
                        }
                    }
                    await Assert.ThrowsAsync<HubException>(() => client.InvokeAsync(nameof(TestHub.AOnly))).WaitAsync(TimeSpan.FromSeconds(1));
                    await client.InvokeAsync(nameof(TestHub.BOrCOnly)).WaitAsync(TimeSpan.FromSeconds(1));
                    Assert.Equal(HubConnectionState.Connected, client.State);
                    switch (disconnectType)
                    {
                        case DisconnectType.ClientStop:
                            await client.StopAsync().WaitAsync(TimeSpan.FromSeconds(1));
                            break;
                        case DisconnectType.ClientAbort:
                            await clientStream.DisposeAsync();
                            await Task.Delay(client.ServerTimeout); // TODO: IConnectionLifetimeFeature
                            break;
                        case DisconnectType.ServerAbort:
                            await serverStream.DisposeAsync();
                            await Task.Delay(client.ServerTimeout); // TODO: IConnectionLifetimeFeature
                            break;
                        case DisconnectType.InvokeAbort:
                            await Assert.ThrowsAsync<TaskCanceledException>(() => client.InvokeAsync(nameof(TestHub.Abort))).WaitAsync(TimeSpan.FromSeconds(1));
                            break;
                        case DisconnectType.BothAbort:
                            {
                                var c = clientStream.DisposeAsync();
                                var s = serverStream.DisposeAsync();
                                await c;
                                await s;
                                await Task.Delay(client.ServerTimeout); // TODO: IConnectionLifetimeFeature
                            }
                            break;
                    }
                    Assert.Equal(HubConnectionState.Disconnected, client.State);
                }
                await disconnection.WaitAsync(TimeSpan.FromSeconds(1));
            }
            await app.StopAsync();
        }

        [Theory]
        [InlineData(1, false, false)]
        [InlineData(2, false, false)]
        [InlineData(1, false, true)]
        [InlineData(2, false, true)]
        [InlineData(1, true, false)]
        [InlineData(2, true, false)]
        [InlineData(1, true, true)]
        [InlineData(2, true, true)]
        public async Task NamedPipeAsync(int maxNumberOfServerInstances, bool delay, bool invokeOnStart)
        {
            var pipeName = Guid.NewGuid().ToString("N");
            using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddSignalR().AddMessagePackProtocol().AddNamedPipe<TestHub>(pipeName, maxNumberOfServerInstances)
            ).Build();
            await app.StartAsync();
            async Task<HubConnection> NewClientAsync()
            {
                var client = new HubConnectionBuilder().AddMessagePackProtocol().WithNamedPipe(pipeName).Build();
                client.HandshakeTimeout = client.ServerTimeout = TimeSpan.FromSeconds(5);
                await client.StartAsync().WaitAsync(TimeSpan.FromSeconds(1));
                if (delay)
                {
                    await Task.Delay(1);
                }
                if (invokeOnStart)
                {
                    var expected = Guid.NewGuid().ToString();
                    TaskCompletionSource<string> tcs = new();
                    using var on = client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs.SetResult(a));
                    await client.InvokeAsync(nameof(TestHub.HubMethod1), expected).WaitAsync(TimeSpan.FromSeconds(1));
                    Assert.Equal(expected, await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1)));
                }
                return client;
            };
            var clients = await Task.WhenAll(Enumerable.Range(0, maxNumberOfServerInstances)
                .Select(_ => NewClientAsync()))
                .WaitAsync(TimeSpan.FromSeconds(1));
            var extraTask = NewClientAsync();
            Assert.False(extraTask.IsCompleted, "Exceeded max number of server instances.");
            Assert.True(clients.All(c => c.State == HubConnectionState.Connected), "Max number of server instances not reached.");
            await Task.WhenAll(clients.Select(async client =>
            {
                {
                    var expected = Guid.NewGuid().ToString();
                    TaskCompletionSource<string> tcs = new();
                    using var on = client.On(nameof(ITestClient.ClientMethod1), (string a) => tcs.SetResult(a));
                    await client.InvokeAsync(nameof(TestHub.HubMethod1), expected).WaitAsync(TimeSpan.FromSeconds(1));
                    Assert.Equal(expected, await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1)));
                }
                await client.StopAsync().WaitAsync(TimeSpan.FromSeconds(1));
                await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            })).WaitAsync(TimeSpan.FromSeconds(1));
            {
                var extra = await extraTask;
                Assert.Equal(HubConnectionState.Connected, extra.State);
                await extra.StopAsync().WaitAsync(TimeSpan.FromSeconds(1));
                Assert.Equal(HubConnectionState.Disconnected, extra.State);
                await extra.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            }
            await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    public class NativeTest
    {
        readonly ITestOutputHelper _output;

        public NativeTest(ITestOutputHelper output) => _output = output;

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
                services.AddSignalR().AddMessagePackProtocol().AddNamedPipe<TestHub>(pipeName, 1)
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
            client.ErrorDataReceived += (_, e) => _output.WriteLine(e.Data);
            Assert.True(client.Start());
            client.BeginErrorReadLine();
            try
            {
                Assert.Equal("Ready", await client.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(1)));
                await context.Clients.All.MsgPackTimeVoid(DateTime.UtcNow).WaitAsync(TimeSpan.FromSeconds(1));
                Assert.Equal("Completed", await client.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)));
                await client.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                var exited = client.HasExited;
                if (!exited)
                {
                    client.Kill(true);
                    await client.WaitForExitAsync();
                }
                Assert.True(exited && client.ExitCode == default, $"Exited with {client.ExitCode}");
            }
            await app.StopAsync();
        }
    }
}