using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FlyByWireless.SignalRTunnel;

sealed class Native : IAsyncDisposable
{
    internal unsafe readonly struct EventHandlers
    {
        public readonly delegate* unmanaged<nint, nint, nint, void> Closed;
        public readonly delegate* unmanaged<nint, nint, void> Reconnected;
        public readonly delegate* unmanaged<nint, nint, nint, void> Reconnecting;
    }

    static Native FromHandle(nint handle) => (Native)GCHandle.FromIntPtr(handle).Target!;

    static unsafe Task Callback(Task task, delegate* unmanaged<nint, nint, void> callback, nint context)
    => task.ContinueWith(t =>
    {
        var m = Marshal.StringToHGlobalUni(t.IsFaulted ? t.Exception?.Message ?? string.Empty : null);
        callback(context, m);
        Marshal.FreeHGlobal(m);
    });

    [UnmanagedCallersOnly(EntryPoint = "signalr_build_with_named_pipe")]
    internal static unsafe nint BuildNamedPipe(char* pipeName, char* serverName, in EventHandlers handlers, nint context)
    {
        try
        {
            return (IntPtr)new Native(new string(pipeName), new string(serverName), handlers, context)._handle;
        }
        catch
        {
            return default;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_build_with_url")]
    internal static unsafe nint BuildWebSockets(char* url, delegate* unmanaged<nint, nint, void> accessTokenProvider, in EventHandlers handlers, nint context)
    {
        try
        {
            return (IntPtr)new Native(new(url), accessTokenProvider, handlers, context)._handle;
        }
        catch
        {
            return default;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_dispose")]
    internal static unsafe void Dispose(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    => Callback(FromHandle(handle).DisposeAsync().AsTask(), callback, context);

    [UnmanagedCallersOnly(EntryPoint = "signalr_start")]
    internal static unsafe nint Start(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        void C() => cts.Cancel();
        Callback(FromHandle(handle)._connection.StartAsync(cts.Token), callback, context)
            .ContinueWith(_ => GCHandle.Alloc(C));
        return Marshal.GetFunctionPointerForDelegate(C);
    }

    [UnmanagedCallersOnly(EntryPoint = "signalr_stop")]
    internal static unsafe nint Stop(nint handle, delegate* unmanaged<nint, nint, void> callback, nint context)
    {
        CancellationTokenSource cts = new();
        void C() => cts.Cancel();
        Callback(FromHandle(handle)._connection.StopAsync(cts.Token), callback, context)
            .ContinueWith(_ => GCHandle.Alloc(C));
        return Marshal.GetFunctionPointerForDelegate(C);
    }

    // TODO: on, invoke, send

    bool _disposing;
    readonly GCHandle _handle;
    readonly HubConnection _connection;
    readonly EventHandlers _handlers;

    unsafe Native(IHubConnectionBuilder builder, in EventHandlers handlers, nint context)
    {
        _handle = GCHandle.Alloc(this);
        _connection = builder.Build();
        _handlers = handlers;
        static Task E(Exception? ex, delegate* unmanaged<nint, nint, nint, void> callback, nint context)
        {
            TaskCompletionSource tcs = new();
            void R() => tcs.SetResult();
            var h = GCHandle.Alloc(R);
            var m = Marshal.StringToHGlobalUni(ex?.Message);
            callback(context, m, Marshal.GetFunctionPointerForDelegate(R));
            return tcs.Task.ContinueWith(_ =>
            {
                Marshal.FreeHGlobal(m);
                h.Free();
            });
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
                    void R() => tcs.SetResult();
                    var h = GCHandle.Alloc(R);
                    d(context, Marshal.GetFunctionPointerForDelegate(R));
                    return tcs.Task.ContinueWith(_ => h.Free());
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
                    void R(nint p) => tcs.SetResult(Marshal.PtrToStringUni(p));
                    var h = GCHandle.Alloc(R);
                    accessTokenProvider(context, Marshal.GetFunctionPointerForDelegate(R));
                    return tcs.Task.ContinueWith(t =>
                    {
                        h.Free();
                        return t.Result;
                    });
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
            GC.SuppressFinalize(this);
        }
    }
}