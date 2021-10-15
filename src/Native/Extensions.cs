using FlyByWireless.SignalRTunnel;
using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

public static class SignalRTunnelNativeExtensions
{
    public static IHubConnectionBuilder AddNativeMessagePackProtocol(this IHubConnectionBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NativeMessagePackHubProtocol>());
        return builder;
    }

    public static IHubConnectionBuilder AddNativeMessagePackProtocol(this IHubConnectionBuilder builder, Action<MessagePackHubProtocolOptions> configure)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NativeMessagePackHubProtocol>());
        builder.Services.Configure(configure);
        return builder;
    }

    #region OnNative
    public static IDisposable OnNative(this HubConnection hub, string methodName, Action handler)
    => hub.OnNative(methodName, () =>
    {
        handler();
        return Task.CompletedTask;
    });

    public static IDisposable OnNative(this HubConnection hub, string methodName, Func<Task> handler)
    => hub.On(methodName, Array.Empty<Type>(), (args, _) =>
    {
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 0);
        return handler();
    }, null!);

    public static IDisposable OnNative<T1>(this HubConnection hub, string methodName, Action<T1> handler)
    => hub.OnNative<T1>(methodName, (a1) =>
    {
        handler(a1);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1>(this HubConnection hub, string methodName, Func<T1, Task> handler)
    => hub.On(methodName, new[] { typeof(T1) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 1);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2>(this HubConnection hub, string methodName, Action<T1, T2> handler)
    => hub.OnNative<T1, T2>(methodName, (a1, a2) =>
    {
        handler(a1, a2);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2>(this HubConnection hub, string methodName, Func<T1, T2, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 2);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3>(this HubConnection hub, string methodName, Action<T1, T2, T3> handler)
    => hub.OnNative<T1, T2, T3>(methodName, (a1, a2, a3) =>
    {
        handler(a1, a2, a3);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3>(this HubConnection hub, string methodName, Func<T1, T2, T3, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 3);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3, T4>(this HubConnection hub, string methodName, Action<T1, T2, T3, T4> handler)
    => hub.OnNative<T1, T2, T3, T4>(methodName, (a1, a2, a3, a4) =>
    {
        handler(a1, a2, a3, a4);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3, T4>(this HubConnection hub, string methodName, Func<T1, T2, T3, T4, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 4);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options),
            MessagePackSerializer.Deserialize<T4>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3, T4, T5>(this HubConnection hub, string methodName, Action<T1, T2, T3, T4, T5> handler)
    => hub.OnNative<T1, T2, T3, T4, T5>(methodName, (a1, a2, a3, a4, a5) =>
    {
        handler(a1, a2, a3, a4, a5);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3, T4, T5>(this HubConnection hub, string methodName, Func<T1, T2, T3, T4, T5, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 5);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options),
            MessagePackSerializer.Deserialize<T4>(ref reader, options),
            MessagePackSerializer.Deserialize<T5>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6>(this HubConnection hub, string methodName, Action<T1, T2, T3, T4, T5, T6> handler)
    => hub.OnNative<T1, T2, T3, T4, T5, T6>(methodName, (a1, a2, a3, a4, a5, a6) =>
    {
        handler(a1, a2, a3, a4, a5, a6);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6>(this HubConnection hub, string methodName, Func<T1, T2, T3, T4, T5, T6, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 6);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options),
            MessagePackSerializer.Deserialize<T4>(ref reader, options),
            MessagePackSerializer.Deserialize<T5>(ref reader, options),
            MessagePackSerializer.Deserialize<T6>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hub, string methodName, Action<T1, T2, T3, T4, T5, T6, T7> handler)
    => hub.OnNative<T1, T2, T3, T4, T5, T6, T7>(methodName, (a1, a2, a3, a4, a5, a6, a7) =>
    {
        handler(a1, a2, a3, a4, a5, a6, a7);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hub, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 7);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options),
            MessagePackSerializer.Deserialize<T4>(ref reader, options),
            MessagePackSerializer.Deserialize<T5>(ref reader, options),
            MessagePackSerializer.Deserialize<T6>(ref reader, options),
            MessagePackSerializer.Deserialize<T7>(ref reader, options)
        );
    }, null!);

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hub, string methodName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
    => hub.OnNative<T1, T2, T3, T4, T5, T6, T7, T8>(methodName, (a1, a2, a3, a4, a5, a6, a7, a8) =>
    {
        handler(a1, a2, a3, a4, a5, a6, a7, a8);
        return Task.CompletedTask;
    });

    public static IDisposable OnNative<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hub, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> handler)
    => hub.On(methodName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) }, (args, _) =>
    {
        var options = (MessagePackSerializerOptions)args[1]!;
        MessagePackReader reader = new((byte[])args[0]!);
        var count = reader.ReadArrayHeader();
        Debug.Assert(count == 8);
        return handler
        (
            MessagePackSerializer.Deserialize<T1>(ref reader, options),
            MessagePackSerializer.Deserialize<T2>(ref reader, options),
            MessagePackSerializer.Deserialize<T3>(ref reader, options),
            MessagePackSerializer.Deserialize<T4>(ref reader, options),
            MessagePackSerializer.Deserialize<T5>(ref reader, options),
            MessagePackSerializer.Deserialize<T6>(ref reader, options),
            MessagePackSerializer.Deserialize<T7>(ref reader, options),
            MessagePackSerializer.Deserialize<T8>(ref reader, options)
        );
    }, null!);
    #endregion

    #region Invoke
    public static Task<object?> InvokeNativeCoreAsync(this HubConnection hub, string methodName, SerializeDelegate serialize, CancellationToken cancellation)
    => hub.InvokeCoreAsync(methodName, typeof(object), new object[] { serialize }, cancellation);

    public static Task InvokeNativeAsync(this HubConnection hub, string methodName, CancellationToken cancellationToken = default)
    {
        static void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        => writer.WriteArrayHeader(0);
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1>(this HubConnection hub, string methodName, T1? arg1, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(1);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(5);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5, T6>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(6);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(7);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(8);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(9);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, T10? arg10, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(10);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg10 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg10, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken);
    }
    #endregion

    #region InvokeGeneric
    public static Task<TResult> InvokeNativeAsync<TResult>(this HubConnection hub, string methodName, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(0);
        }
        return hub.InvokeCoreAsync(methodName, typeof(object), new object[] { new SerializeDelegate(S) }, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, TResult>(this HubConnection hub, string methodName, T1? arg1, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(1);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(2);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(3);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(4);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(5);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, T6, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(6);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(7);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(8);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(9);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static Task<TResult> InvokeNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, T10? arg10, CancellationToken cancellationToken = default)
    {
        MessagePackSerializerOptions? o = null;
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            o = options;
            writer.WriteArrayHeader(10);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg10 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg10, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.InvokeNativeCoreAsync(methodName, S, cancellationToken)
            .ContinueWith(task => MessagePackSerializer.Deserialize<TResult>((byte[])task.Result!, o), TaskContinuationOptions.OnlyOnRanToCompletion);
    }
    #endregion

    #region Send
    public static Task SendNativeCoreAsync(this HubConnection hub, string methodName, SerializeDelegate serialize, CancellationToken cancellation)
    => hub.SendCoreAsync(methodName, new object[] { serialize }, cancellation);

    public static Task SendNativeAsync(this HubConnection hub, string methodName, CancellationToken cancellationToken = default)
    {
        static void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        => writer.WriteArrayHeader(0);
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1>(this HubConnection hub, string methodName, T1? arg1, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(1);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(5);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5, T6>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(6);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(7);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(8);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(9);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }

    public static Task SendNativeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this HubConnection hub, string methodName, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, T10? arg10, CancellationToken cancellationToken = default)
    {
        void S(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(10);
            if (arg1 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg1, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg2 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg2, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg3 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg3, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg4 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg4, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg5 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg5, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg6 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg6, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg7 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg7, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg8 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg8, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg9 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg9, options);
            }
            else
            {
                writer.WriteNil();
            }
            if (arg10 != null)
            {
                MessagePackSerializer.Serialize(ref writer, arg10, options);
            }
            else
            {
                writer.WriteNil();
            }
        }
        return hub.SendNativeCoreAsync(methodName, S, cancellationToken);
    }
    #endregion
}