# SignalR Tunnel
Transport injection for ASP.NET Core SignalR

## Server Usage
`FlyByWireless.SignalRTunnel`
```cs
class MyService : IHostedService
{
	readonly HubConnectionHandler<MyHub> _handler;

	public int TunnelCount { get; private set; }

	public MyService(HubConnectionHandler<MyHub> handler)
	{
		_handler = handler;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var stream = await AcceptStreamAsync(cancellationToken);
			Interlocked.Increment(ref TunnelCount);
			_ = handler.OnConnectedAsync(stream)
				.ContinueWith(_ => Interlocked.Decrement(ref TunnelCount));
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	=> Task.CompletedTask;

	Task<Stream> AcceptStreamAsync() => throw new NotImplementedException();
}
```

## Client Usage
`FlyByWireless.SignalRTunnel.Client`
```cs
var stream = quicConnection.OpenBidirectionalStream();
var client = new HubConnectionBuilder()
	.WithTunnel(stream)
	.Build();
```