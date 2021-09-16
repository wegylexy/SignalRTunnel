using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRTunnelClientExtensions
    {
        public static IHubConnectionBuilder WithTunnel(this IHubConnectionBuilder hubConnectionBuilder, IDuplexPipe transport)
        {
            hubConnectionBuilder.Services.AddSingleton<EndPoint>(new DnsEndPoint(".", 0));
            Client client = new Client(transport);
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(client);
            return hubConnectionBuilder.WithAutomaticReconnect(client);
        }

        public static IHubConnectionBuilder WithTunnel(this IHubConnectionBuilder hubConnectionBuilder, Stream transport)
        => WithTunnel(hubConnectionBuilder, new DuplexPipe(transport));
    }
}