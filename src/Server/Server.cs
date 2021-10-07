using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FlyByWireless.SignalRTunnel
{
    sealed class NamedPipeServer<THub> : IHostedService where THub : Hub
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
                    using SemaphoreSlim ss = new SemaphoreSlim(1);
                    ss.Wait();
                    using var r = cancellationToken.Register(() => ss.Release());
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        NamedPipeServerStream server;
                        try
                        {
                            server = await _factory(_provider, cancellationToken);
                        }
                        catch (IOException ex) when ((uint)ex.HResult == 0x800700E7) // ERROR_PIPE_BUSY
                        {
                            await ss.WaitAsync(cancellationToken);
                            continue;
                        }
                        if (!server.IsConnected)
                        {
                            await server.WaitForConnectionAsync(cancellationToken);
                        }
                        DuplexContext context = null!;
                        _ = _handler.OnConnectedAsync(server, configure: c => context = c).ContinueWith(async _ =>
                        {
                            try
                            {
                                await using (server)
                                await using (context)
                                { }
                            }
                            finally
                            {
                                ss.Release();
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

    static class IdGenerator
    {
        static readonly char[] _c = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

        static long _seed = DateTime.UtcNow.Ticks;

        public static string Generate(long seed) => string.Create(13, seed, (buffer, seed) =>
        {
            buffer[0] = _c[(seed >> 60) & 31];
            buffer[1] = _c[(seed >> 55) & 31];
            buffer[2] = _c[(seed >> 50) & 31];
            buffer[3] = _c[(seed >> 45) & 31];
            buffer[4] = _c[(seed >> 40) & 31];
            buffer[5] = _c[(seed >> 35) & 31];
            buffer[6] = _c[(seed >> 30) & 31];
            buffer[7] = _c[(seed >> 25) & 31];
            buffer[8] = _c[(seed >> 20) & 31];
            buffer[9] = _c[(seed >> 15) & 31];
            buffer[10] = _c[(seed >> 10) & 31];
            buffer[11] = _c[(seed >> 5) & 31];
            buffer[12] = _c[seed & 31];
        });

        public static string NextId() => Generate(Interlocked.Increment(ref _seed));

    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRTunnelServerExtensions
    {
        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, IDuplexPipe transport, ClaimsPrincipal? user = null, Action<DuplexContext>? configure = null) where THub : Hub
        {
            DuplexContext context = new DuplexContext(transport)
            {
                ConnectionId = IdGenerator.NextId(),
                User = user
            };
            configure?.Invoke(context);
            return handler.OnConnectedAsync(context);
        }

        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, Stream transport, ClaimsPrincipal? user = null, Action<DuplexContext>? configure = null) where THub : Hub
        => handler.OnConnectedAsync(new DuplexPipe(transport), user, configure);

        public static ISignalRServerBuilder AddNamedPipe<THub>(this ISignalRServerBuilder builder, string pipeName, int maxNumberOfServerInstances = NamedPipeServerStream.MaxAllowedServerInstances) where THub : Hub
        => builder.AddNamedPipe<THub>((provider, _) =>
            Task.FromResult(new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough))
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