using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlyByWireless.SignalRTunnel
{
    public sealed class DuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        public DuplexPipe(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public DuplexPipe(Stream stream) :
            this(PipeReader.Create(stream), PipeWriter.Create(stream))
        { }
    }

    public class DuplexContext : ConnectionContext, IConnectionUserFeature
    {
        public override IDuplexPipe Transport { get; set; }
        public override string ConnectionId { get; set; } = null!;
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
        public ClaimsPrincipal? User { get; set; }

        public DuplexContext(IDuplexPipe transport)
        {
            Transport = transport;
            Features.Set<IConnectionUserFeature>(this);
        }

        public DuplexContext(Stream transport) : this(new DuplexPipe(transport)) { }

        public override ValueTask DisposeAsync()
        => Transport.Input.AsStream(false).DisposeAsync();
    }
}