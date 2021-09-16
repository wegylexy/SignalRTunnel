using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRTunnelServerExtensions
    {
        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, IDuplexPipe transport, out string connectionId) where THub : Hub
        {
            Span<byte> buffer = stackalloc byte[16];
            RandomNumberGenerator.Fill(buffer);
            return handler.OnConnectedAsync(new DuplexContext(transport)
            {
                ConnectionId = connectionId = AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(buffer)
            }).ContinueWith(task =>
            {
                var e = task.Exception?.InnerException;
                transport.Output.Complete(e);
                transport.Input.Complete(e);
            });
        }

        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, Stream transport, out string connectionId) where THub : Hub
        => handler.OnConnectedAsync(new DuplexPipe(transport), out connectionId);

        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, IDuplexPipe transport) where THub : Hub
        => handler.OnConnectedAsync(transport, out _);

        public static Task OnConnectedAsync<THub>(this HubConnectionHandler<THub> handler, Stream transport) where THub : Hub
        => handler.OnConnectedAsync(transport, out _);
    }
}