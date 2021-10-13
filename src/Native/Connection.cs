using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlyByWireless.SignalRTunnel;

sealed class Connection : IAsyncDisposable
{
    internal unsafe readonly struct EventHandlers
    {
        public readonly delegate* unmanaged<nint, nint, nint, void> Closed = null;
        public readonly delegate* unmanaged<nint, nint, void> Reconnected = null;
        public readonly delegate* unmanaged<nint, nint, nint, void> Reconnecting = null;
    }

    static Connection FromHandle(nint handle) => (Connection)GCHandle.FromIntPtr(handle).Target!;

    static nint CancelFunctionPointer(CancellationTokenSource source)
    {
        GCHandle h = default;
        Action c = () =>
        {
            h.Free();
            source.Cancel();
            source.Dispose();
        };
        h = GCHandle.Alloc(c);
        return Marshal.GetFunctionPointerForDelegate(c);
    }

    static nint CompleteFunctionPointer(TaskCompletionSource source)
    {
        GCHandle h = default;
        Action c = () =>
        {
            h.Free();
            source.SetResult();
        };
        h = GCHandle.Alloc(c);
        return Marshal.GetFunctionPointerForDelegate(c);
    }

    static unsafe Task Callback(Task task, delegate* unmanaged<nint, nint, void> callback, nint context)
    => task.ContinueWith(t =>
    {
        var m = Marshal.StringToCoTaskMemUTF8(t.IsCompletedSuccessfully ? null : t.Exception?.InnerException?.ToString() ?? string.Empty);
        callback(context, m);
        Marshal.ZeroFreeCoTaskMemUTF8(m);
    });

    [UnmanagedCallersOnly(EntryPoint = "signalr_build_with_named_pipe")]
    internal static unsafe nint BuildNamedPipe(nint pipeName, nint serverName, in EventHandlers handlers, nint context)
    {
        try
        {
            return (IntPtr)new Connection(Marshal.PtrToStringAnsi(pipeName)!, Marshal.PtrToStringAnsi(serverName)!, handlers, context)._handle;
        }
        catch
        {
            return default;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_build_with_url")]
    internal static unsafe nint BuildWebSockets(nint url, delegate* unmanaged<nint, nint, void> accessTokenProvider, in EventHandlers handlers, nint context)
    {
        try
        {
            return (IntPtr)new Connection(Marshal.PtrToStringAnsi(url)!, accessTokenProvider, handlers, context)._handle;
        }
        catch
        {
            return default;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_dispose")]
    internal static unsafe void Dispose(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    => Callback(FromHandle(handle).DisposeAsync().AsTask(), callback, context);

    [UnmanagedCallersOnly(EntryPoint = "signalr_remove")]
    internal static unsafe void Remove(nint handle, nint methodName)
    {
        var m = Marshal.PtrToStringUTF8(methodName)!;
        var n = FromHandle(handle);
        n._connection.Remove(m);
        n._ons.TryRemove(m, out _);
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_on")]
    internal static unsafe nint On(nint handle, nint methodName, int argc, delegate* unmanaged<nint, byte*, int, nint, void> handler, nint context)
    {
        var m = Marshal.PtrToStringUTF8(methodName)!;
        var n = FromHandle(handle);
        var d = n._connection.On(m, Array.Empty<Type>(), (args, _) =>
        {
            TaskCompletionSource tcs = new();
            var b = (byte[])args[0]!;
            fixed (byte* p = b)
            {
                handler(context, p, b.Length, CompleteFunctionPointer(tcs));
            }
            return tcs.Task;
        }, null!);
        Action r = null!;
        var ons = n._ons.GetOrAdd(m, _ => new());
        var added = ons.Add(r = () =>
        {
            d.Dispose();
            ons.Remove(r);
        });
        Debug.Assert(added);
        return Marshal.GetFunctionPointerForDelegate(r);
    }

    // TODO: remove when https://github.com/dotnet/aspnetcore/issues/37340 is fixed
    static async Task _Start(nint handle, CancellationToken token)
    {
        await FromHandle(handle)._connection.StartAsync(token);
        await Task.Delay(1, token);
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_start")]
    internal static unsafe nint Start(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        var p = CancelFunctionPointer(cts);
        Callback(_Start(handle, cts.Token), callback, context);
        return p;
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_stop")]
    internal static unsafe nint Stop(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        var p = CancelFunctionPointer(cts);
        Callback(FromHandle(handle)._connection.StopAsync(cts.Token), callback, context);
        return p;
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_invoke_core")]
    internal static unsafe nint InvokeCore(nint handle, nint methodName, byte* buffer, int bufferSize, delegate* unmanaged<nint, nint, byte*, int, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        var p = CancelFunctionPointer(cts);
        FromHandle(handle)._connection
            .InvokeCoreAsync(Marshal.PtrToStringUTF8(methodName)!, typeof(object), new[] { new ReadOnlySpan<byte>(buffer, bufferSize).ToArray() }, cts.Token)
            .ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (t.Result is byte[] b)
                    {
                        fixed (byte* p = b)
                        {
                            callback(context, default, p, b.Length);
                        }
                    }
                    else
                    {
                        callback(context, default, null, default);
                    }
                }
                else
                {
                    var m = Marshal.StringToCoTaskMemUTF8(t.Exception?.InnerException?.ToString() ?? string.Empty);
                    callback(context, m, null, default);
                    Marshal.ZeroFreeCoTaskMemUTF8(m);
                }
            });
        return p;
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_send_core")]
    internal static unsafe nint SendCore(nint handle, nint methodName, byte* buffer, int bufferSize, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        var p = CancelFunctionPointer(cts);
        Callback(FromHandle(handle)._connection.SendCoreAsync(
            Marshal.PtrToStringUTF8(methodName)!,
            new[] { new ReadOnlySpan<byte>(buffer, bufferSize).ToArray() },
            cts.Token
        ), callback, context);
        return p;
    }

    bool _disposing;
    readonly HubConnection _connection;
    readonly GCHandle _handle;
    readonly EventHandlers _handlers;
    readonly ConcurrentDictionary<string, HashSet<Action>> _ons = new();

    unsafe Connection(IHubConnectionBuilder builder, in EventHandlers handlers, nint context)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NativeMessagePackHubProtocol>());
        _connection = builder.Build();
        _handle = GCHandle.Alloc(this);
        _handlers = handlers;
        static Task E(Exception? ex, delegate* unmanaged<nint, nint, nint, void> callback, nint context)
        {
            TaskCompletionSource tcs = new();
            GCHandle h = default;
            var m = Marshal.StringToCoTaskMemUTF8(ex?.ToString());
            Action r = () =>
            {
                h.Free();
                Marshal.ZeroFreeCoTaskMemUTF8(m);
                tcs.SetResult();
            };
            h = GCHandle.Alloc(r);
            callback(context, m, Marshal.GetFunctionPointerForDelegate(r));
            return tcs.Task;
        }
        {
            if (_handlers.Closed is not null and var d)
            {
                _connection.Closed += ex => E(ex, d, context);
            }
        }
        {
            if (_handlers.Reconnected is not null and var d)
            {
                _connection.Reconnected += ex =>
                {
                    TaskCompletionSource tcs = new();
                    d(context, CompleteFunctionPointer(tcs));
                    return tcs.Task;
                };
            }
        }
        {
            if (_handlers.Reconnecting is not null and var d)
            {
                _connection.Reconnecting += ex => E(ex, d, context);
            }
        }
    }

    unsafe Connection(string pipeName, string serverName, in EventHandlers handlers, nint context) : this(new HubConnectionBuilder()
        .WithNamedPipe(pipeName, serverName)
    , handlers, context)
    { }

    unsafe Connection(string url, delegate* unmanaged<nint, nint, void> accessTokenProvider, in EventHandlers handlers, nint context) : this(new HubConnectionBuilder()
        .WithUrl(url, HttpTransportType.WebSockets, o =>
        {
            if (accessTokenProvider != null)
            {
                o.AccessTokenProvider = () =>
                {
                    TaskCompletionSource<string?> tcs = new();
                    GCHandle h = default;
                    Action<nint> r = (nint p) =>
                    {
                        h.Free();
                        tcs.SetResult(Marshal.PtrToStringUTF8(p));
                    };
                    h = GCHandle.Alloc(r);
                    accessTokenProvider(context, Marshal.GetFunctionPointerForDelegate(r));
                    return tcs.Task;
                };
            }
        })
    , handlers, context)
    { }

    public async ValueTask DisposeAsync()
    {
        if (!_disposing)
        {
            _disposing = true;
            _handle.Free();
            await _connection.DisposeAsync();
            _ons.Clear();
            GC.SuppressFinalize(this);
        }
    }
}