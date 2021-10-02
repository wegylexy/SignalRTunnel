using MessagePack;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlyByWireless.SignalRTunnel;

sealed class Native : IAsyncDisposable
{
    internal unsafe readonly struct EventHandlers
    {
        public readonly delegate* unmanaged<nint, nint, nint, void> Closed = default;
        public readonly delegate* unmanaged<nint, nint, void> Reconnected = default;
        public readonly delegate* unmanaged<nint, nint, nint, void> Reconnecting = default;
    }

    static Native FromHandle(nint handle) => (Native)GCHandle.FromIntPtr(handle).Target!;

    static nint CancelFunctionPointer(CancellationTokenSource source)
    {
        GCHandle h = default;
        Action c = () =>
        {
            h.Free();
            source.Cancel();
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
        var m = Marshal.StringToCoTaskMemUTF8(t.IsFaulted ? t.Exception?.InnerException?.Message ?? string.Empty : null);
        callback(context, m);
        Marshal.ZeroFreeCoTaskMemUTF8(m);
    });

    [UnmanagedCallersOnly(EntryPoint = "signalr_build_with_named_pipe")]
    internal static unsafe nint BuildNamedPipe(nint pipeName, nint serverName, in EventHandlers handlers, nint context)
    {
        try
        {
            return (IntPtr)new Native(Marshal.PtrToStringAnsi(pipeName)!, Marshal.PtrToStringAnsi(serverName)!, handlers, context)._handle;
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
            return (IntPtr)new Native(Marshal.PtrToStringAnsi(url)!, accessTokenProvider, handlers, context)._handle;
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
        var types = new Type[argc];
        Array.Fill(types, typeof(object));
        var d = n._connection.On(m, types, (args, _) =>
        {
            TaskCompletionSource tcs = new();
            var b = MessagePackSerializer.Serialize(args);
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

    [UnmanagedCallersOnly(EntryPoint = "signalr_start")]
    internal static unsafe nint Start(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        var p = CancelFunctionPointer(cts);
        Callback(FromHandle(handle)._connection.StartAsync(cts.Token), callback, context);
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

    // TODO: on, invoke, send

    bool _disposing;
    readonly GCHandle _handle;
    readonly HubConnection _connection;
    readonly EventHandlers _handlers;
    readonly ConcurrentDictionary<string, HashSet<Action>> _ons = new();

    unsafe Native(IHubConnectionBuilder builder, in EventHandlers handlers, nint context)
    {
        _handle = GCHandle.Alloc(this);
        _connection = builder.Build();
        _handlers = handlers;
        static Task E(Exception? ex, delegate* unmanaged<nint, nint, nint, void> callback, nint context)
        {
            TaskCompletionSource tcs = new();
            GCHandle h = default;
            var m = Marshal.StringToCoTaskMemUTF8(ex?.Message);
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

    unsafe Native(string pipeName, string serverName, in EventHandlers handlers, nint context) : this(new HubConnectionBuilder()
        .AddMessagePackProtocol()
        .WithNamedPipe(pipeName, serverName)
    , handlers, context)
    { }

    unsafe Native(string url, delegate* unmanaged<nint, nint, void> accessTokenProvider, in EventHandlers handlers, nint context) : this(new HubConnectionBuilder()
        .AddMessagePackProtocol()
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