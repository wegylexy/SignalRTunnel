using MessagePack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace FlyByWireless.SignalRTunnel;

sealed class NativeMessagePackHubProtocol : IHubProtocol
{
    public string Name => "messagepack";
    public int Version => 1;
    public TransferFormat TransferFormat => TransferFormat.Binary;

    readonly MessagePackHubProtocol _p = new();

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    => _p.GetMessageBytes(message);

    public bool IsVersionSupported(int version) => version == Version;

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
    {
        if (!input.IsEmpty)
        {
            var buffer = input.Slice(0, Math.Min(5, input.Length));
            var span = buffer.IsSingleSegment ? buffer.FirstSpan : buffer.ToArray();
            var length = 0U;
            var read = 0;
            byte b;
            bool m;
            do
            {
                b = span[read];
                length |= ((uint)(b & 0x7F)) << (read * 7);
                ++read;
                m = (b & 0x80) == 0;
            } while (read < span.Length && !m);
            if (m && (read < 5 || b <= 7) && input.Length >= length + read)
            {
                try
                {
                    MessagePackReader reader = new(input.Slice(read, length));
                    var count = reader.ReadArrayHeader();
                    switch (reader.ReadInt32())
                    {
                        case HubProtocolConstants.InvocationMessageType:
                            {
                                Dictionary<string, string>? headers = null;
                                if (reader.ReadMapHeader() is > 0 and var headerCount)
                                {
                                    headers = new(StringComparer.Ordinal);
                                    for (var i = 0; i < headerCount; ++i)
                                    {
                                        var key = reader.ReadString();
                                        var value = reader.ReadString();
                                        headers.Add(key, value);
                                    }
                                }
                                message = new InvocationMessage
                                (
                                    reader.ReadString() is { Length: > 0 } and var id ? id : null,
                                    reader.ReadString(),
                                    new[] { input.Slice(read + reader.Consumed).ToArray() }
                                )
                                {
                                    Headers = headers
                                };
                            }
                            break;
                        case HubProtocolConstants.PingMessageType:
                            message = PingMessage.Instance;
                            break;
                        case HubProtocolConstants.CloseMessageType:
                            {
                                var error = reader.ReadString();
                                var allowReconnect = count > 2 && reader.ReadBoolean();
                                message = error == null && !allowReconnect ?
                                    CloseMessage.Empty :
                                    new(error, allowReconnect);
                            }
                            break;
                        default:
                            message = null;
                            break;
                    }
                    return message != null;
                }
                finally
                {
                    input = input.Slice(read + length);
                }
            }
        }
        message = null;
        return false;
    }

    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    => _p.WriteMessage(message, output);
}
