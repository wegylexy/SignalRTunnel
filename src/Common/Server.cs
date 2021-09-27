using FlyByWireless.SignalRTunnel;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

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
    }
}