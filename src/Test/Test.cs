using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FlyByWireless.SignalRTunnel.Test
{
    public interface ITestClient
    {
        Task ClientMethod1(string a);
    }

    sealed class TestHub : Hub<ITestClient>
    {
        public override Task OnConnectedAsync()
        {
            Debug.WriteLine("Connected: " + Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Debug.WriteLine("Disconnected: " + Context.ConnectionId);
            return Task.CompletedTask;
        }

        public Task HubMethod1(string a)
        {
            Clients.Caller.ClientMethod1(a);
            return Task.CompletedTask;
        }

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

    public class Test
    {
        [Fact]
        public async Task TunnelAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = Environments.Development
            });
            builder.Logging.ClearProviders();
            builder.Services.AddSignalR();
            var app = builder.Build();
            await app.StartAsync();
            var pipeName = Guid.NewGuid().ToString("N");
            // TODO: replace the named pipe with a more efficient buffer for testing
            using NamedPipeClientStream clientStream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            using NamedPipeServerStream serverStream = new(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await Task.WhenAll(
                clientStream.ConnectAsync(),
                serverStream.WaitForConnectionAsync()
            );
            Assert.True(clientStream.IsConnected && serverStream.IsConnected);
            var client = new HubConnectionBuilder()
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
            var hubContext = app.Services.GetRequiredService<IHubContext<TestHub, ITestClient>>();
            {
                var expected = Guid.NewGuid().ToString();
                tcs = new();
                await hubContext.Clients.All.ClientMethod1(expected);
                Assert.Equal(expected, await tcs.Task);
            }
            {
                var expected = Guid.NewGuid().ToString();
                tcs = new();
                await client.InvokeAsync(nameof(TestHub.HubMethod1), expected);
                Assert.Equal(expected, await tcs.Task);
            }
            await Assert.ThrowsAsync<HubException>(() => client.InvokeAsync(nameof(TestHub.AOnly)));
            await client.InvokeAsync(nameof(TestHub.BOrCOnly));
            await Assert.ThrowsAsync<TaskCanceledException>(() => client.InvokeAsync(nameof(TestHub.Abort)));
            await connection;
            await app.StopAsync();
        }
    }
}