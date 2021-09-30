using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FlyByWireless.SignalRTunnel
{
    class NamedPipeServer<THub> : IHostedService where THub : Hub
    {
        CancellationTokenSource? _cts;
        Task? _loop;
        readonly IServiceProvider _provider;
        readonly HubConnectionHandler<THub> _handler;
        readonly Func<IServiceProvider, CancellationToken, Task<NamedPipeServerStream>> _factory;

        public NamedPipeServer(IServiceProvider provider, HubConnectionHandler<THub> handler, Func<IServiceProvider, CancellationToken, Task<NamedPipeServerStream>> factory)
        {
            _provider = provider;
            _handler = handler;
            _factory = factory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken = (_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)).Token;
            _loop = Task.Run(async () =>
            {
                try
                {
                    ManualResetEventSlim mres = new ManualResetEventSlim();
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        NamedPipeServerStream server;
                        try
                        {
                            server = await _factory(_provider, cancellationToken);
                        }
                        catch (IOException ex) when ((uint)ex.HResult == 0x800700E7) // ERROR_PIPE_BUSY
                        {
                            mres.Wait(cancellationToken);
                            mres.Reset();
                            continue;
                        }
                        if (!server.IsConnected)
                        {
                            await server.WaitForConnectionAsync(cancellationToken);
                        }
                        _ = _handler.OnConnectedAsync(server).ContinueWith(async _ =>
                        {
                            try
                            {
                                var t = server.DisposeAsync();
                                if (!t.IsCompleted)
                                {
                                    await t;
                                }
                            }
                            finally
                            {
                                mres.Set();
                            }
                        });
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts!.Cancel(false);
            return _loop ?? Task.CompletedTask;
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRTunnelServerExtensions
    {
        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, IDuplexPipe transport, ClaimsPrincipal? user = null, Action<DuplexContext>? configure = null) where THub : Hub
        {
            Span<byte> buffer = stackalloc byte[16];
            RandomNumberGenerator.Fill(buffer);
            DuplexContext context = new DuplexContext(transport)
            {
                ConnectionId = AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(buffer),
                User = user
            };
            configure?.Invoke(context);
            return handler.OnConnectedAsync(context).ContinueWith(task =>
            {
                var e = task.Exception?.InnerException;
                transport.Output.Complete(e);
                transport.Input.Complete(e);
            });
        }

        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, Stream transport, ClaimsPrincipal? user = null, Action<DuplexContext>? configure = null) where THub : Hub
        => handler.OnConnectedAsync(new DuplexPipe(transport), user, configure);

        public static ISignalRServerBuilder AddNamedPipe<THub>(this ISignalRServerBuilder builder, string pipeName, int maxNumberOfServerInstances = NamedPipeServerStream.MaxAllowedServerInstances) where THub : Hub
        => builder.AddNamedPipe<THub>((provider, _) =>
            Task.FromResult(new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous))
        );

        public static ISignalRServerBuilder AddNamedPipe<THub>(this ISignalRServerBuilder builder, Func<IServiceProvider, CancellationToken, Task<NamedPipeServerStream>> factory) where THub : Hub
        {
            builder.Services
                .AddSingleton(factory)
                .AddSingleton<NamedPipeServer<THub>>().AddHostedService<NamedPipeServer<THub>>();
            return builder;
        }
    }
}