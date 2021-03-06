# SignalR Tunnel
Transport injection for ASP.NET Core SignalR

## Server Usage
`FlyByWireless.SignalRTunnel`

### Named Pipe
```cs
// Runs a light weight SignalR server without HTTP
using var app = Host.CreateDefaultBuilder().ConfigureServices(services =>
    services.AddSignalR()
        .AddMessagePackProtocol()
        .AddNamedPipe<MyHub>("My Hub", maxNumberOfServerInstances)
        .AddNamedPipe<AnotherHub>("Another Hub", maxNumberOfServerInstances)
        //.AddMyQuicStream<MyHub>(MyQuicStreamType.MyHub, roles: "admins,managers")
).Build();
app.Run();
```
### Custom Stream / IDuplexPipe
Refer to source code of `FlyByWireless.SignalRTunnel.NamedPipeServer<THub>` or `FlyByWireless.SignalRTunnel.Test`.

An authenticated user may be set during `OnConnectedAsync()` to use the `[Authorize]` attribute on hub methods.

## Client Usage
`FlyByWireless.SignalRTunnel.Client`

### Named Pipe
```cs
await using var client = new HubConnectionBuilder()
    .WithNamedPipe("My Hub"
        //, serverName: "."
    )
    .Build();
// TODO: register event handlers
await client.StartAsync();
// TODO: invoke methods
await client.StopAsync();
```

### Custom Stream / IDuplexPipe
```cs
var stream = myQuicConnection.OpenBidirectionalStream();
await stream.AuthAsync("admin", "password");
await using var client = new HubConnectionBuilder()
    .WithTunnel(stream)
    .Build();
// TODO: register event handlers
await client.StartAsync();
await Task.Delay(1); // avoid race condition
// TODO: invoke methods
await client.StopAsync();
```