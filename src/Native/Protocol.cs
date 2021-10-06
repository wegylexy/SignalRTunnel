using MessagePack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.IO;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace FlyByWireless.SignalRTunnel;

sealed class NativeMessagePackHubProtocol : IHubProtocol
{
    static readonly RecyclableMemoryStreamManager _rmsm = new();

    public string Name => "messagepack";
    public int Version => 1;
    public TransferFormat TransferFormat => TransferFormat.Binary;

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    => throw new NotSupportedException($"{nameof(GetMessageBytes)} not supported on client.");

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
    {
        using RecyclableMemoryStream stream = new(_rmsm);
        var writer = new MessagePackWriter(stream);
        static void H(IDictionary<string, string>? headers, ref MessagePackWriter writer)
        {
            if (headers != null)
            {
                writer.WriteMapHeader(headers.Count);
                if (headers.Count > 0)
                {
                    foreach (var h in headers)
                    {
                        writer.Write(h.Key);
                        writer.Write(h.Value);
                    }
                }
            }
            else
            {
                writer.WriteNil();
            }
        }
        static void S(string[]? streamIds, ref MessagePackWriter writer)
        {
            if (streamIds != null)
            {
                writer.WriteArrayHeader(streamIds.Length);
                foreach (var s in streamIds)
                {
                    writer.Write(s);
                }
            }
            else
            {
                writer.WriteArrayHeader(0);
            }
        }
        switch (message)
        {
            case InvocationMessage invocation:
                writer.WriteArrayHeader(6);
                writer.Write(HubProtocolConstants.InvocationMessageType);
                H(invocation.Headers, ref writer);
                if (string.IsNullOrEmpty(invocation.InvocationId))
                {
                    writer.WriteNil();
                }
                else
                {
                    writer.Write(invocation.InvocationId);
                }
                writer.Write(invocation.Target);
                writer.WriteRaw((byte[])invocation.Arguments[0]!);
                S(invocation.StreamIds, ref writer);
                break;
            case StreamInvocationMessage:
            case StreamItemMessage:
            case CompletionMessage:
                throw new PlatformNotSupportedException("Not yet supported with NativeAOT.");
            case CancelInvocationMessage cancel:
                writer.WriteArrayHeader(3);
                writer.Write(HubProtocolConstants.CancelInvocationMessageType);
                H(cancel.Headers, ref writer);
                writer.Write(cancel.InvocationId);
                break;
            case PingMessage ping:
                writer.WriteArrayHeader(1);
                writer.Write(HubProtocolConstants.PingMessageType);
                break;
            case CloseMessage close:
                writer.WriteArrayHeader(3);
                writer.Write(HubProtocolConstants.CloseMessageType);
                if (string.IsNullOrWhiteSpace(close.Error))
                {
                    writer.WriteNil();
                }
                else
                {
                    writer.Write(close.Error);
                }
                writer.Write(close.AllowReconnect);
                break;
            default:
                throw new InvalidDataException(FormattableString.Invariant($"Unexpected messgae type: {message.GetType().Name}"));
        }
        writer.Flush();
        var length = (int)stream.Length;
        {
            Span<byte> prefix = stackalloc byte[5];
            var bytes = 0;
            for (var l = length; ;)
            {
                ++bytes;
                ref var b = ref prefix[bytes];
                b = (byte)(l & 0x7F);
                l >>= 7;
                if (l == 0)
                {
                    break;
                }
                b |= 0x80;
            }
            output.Write(prefix[..bytes]);
        }
        stream.Position = 0;
        do
        {
            length -= stream.Read(output.GetSpan(Math.Min(length, 4096)));
        } while (length > 0);

    }
}
