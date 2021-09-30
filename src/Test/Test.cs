using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlyByWireless.SignalRTunnel.Test
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public interface ITestClient
    {
        Task ClientMethod1(string a);
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
            async Task<HubConnection> NewClient()
            {
                var client = new HubConnectionBuilder().AddMessagePackProtocol().WithNamedPipe(pipeName).Build();
                await client.StartAsync();
                return client;
            };
            var clients = await Task.WhenAll(Enumerable.Range(0, maxNumberOfServerInstances)
                .Select(_ => NewClient()));
            var extraTask = NewClient();
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
    }
}