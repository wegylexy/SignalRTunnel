using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FlyByWireless.SignalRTunnel
{
    sealed class Client : IConnectionFactory, IRetryPolicy
    {
        readonly IDuplexPipe _transport;

        public Client(IDuplexPipe transport) => _transport = transport;

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        => new ValueTask<ConnectionContext>(new DuplexContext(_transport) { ConnectionId = null! });

        public TimeSpan? NextRetryDelay(RetryContext retryContext) => null;
    }

    sealed class NamedPipeClient : IConnectionFactory
    {
        readonly IServiceProvider _provider;
        readonly Func<IServiceProvider, CancellationToken, NamedPipeClientStream> _factory;

        public NamedPipeClient(IServiceProvider provider, Func<IServiceProvider, CancellationToken, NamedPipeClientStream> factory)
        {
            _provider = provider;
            _factory = factory;
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var client = _factory(_provider, cancellationToken);
            if (!client.IsConnected)
            {
                await client.ConnectAsync(cancellationToken);
            }
            return new DuplexContext(client);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRTunnelClientExtensions
    {
        static readonly EndPoint _endPoint = new DnsEndPoint(".", 0);

        public static IHubConnectionBuilder WithTunnel(this IHubConnectionBuilder builder, IDuplexPipe transport)
        {
            Client client = new Client(transport);
            builder.Services.AddSingleton(_endPoint).AddSingleton<IConnectionFactory>(client);
            return builder.WithAutomaticReconnect(client);
        }

        public static IHubConnectionBuilder WithTunnel(this IHubConnectionBuilder hubConnectionBuilder, Stream transport)
        => WithTunnel(hubConnectionBuilder, new DuplexPipe(transport));

        public static IHubConnectionBuilder WithNamedPipe(this IHubConnectionBuilder builder, Func<IServiceProvider, CancellationToken, NamedPipeClientStream> factory)
        {
            builder.Services.AddSingleton(_endPoint).AddSingleton(factory).AddSingleton<IConnectionFactory, NamedPipeClient>();
            return builder;
        }

        public static IHubConnectionBuilder WithNamedPipe(this IHubConnectionBuilder builder, string pipeName, string serverName = ".")
        => builder.WithNamedPipe((provider, _) => new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough));
    }
}