#define _HAS_ITERATOR_DEBUGGING 0
#include <signalr.hpp>

using namespace std;
using namespace concurrency;
using namespace FlyByWireless::SignalRTunnel;

class Client : public HubConnection
{
public:
	task<void> OnClosed(const char* error) override
	{
		return HubConnection::OnClosed(error);
	}

	task<void> OnReconnected() override
	{
		return HubConnection::OnReconnected();
	}

	task<void> OnReconnecting(const char* error) override
	{
		return HubConnection::OnReconnecting(error);
	}

	Client(const char* pipeName, const char* serverName) : HubConnection{ pipeName, serverName }
	{ }
};

int main(int argc, const char** argv)
{
	try
	{
		Client hub{ argv[1], "." };
		// TODO: on
		hub.Start().then(
			[&hub]()
			{
				// TODO: invoke / send
			}
		).then(
			[&hub]()
			{
				return hub.Stop();
			}
		).then(
			[&hub]()
			{
				return hub.Dispose();
			}
		).get(
		);
	}
	catch (const exception& e)
	{
		fprintf(stderr, "%s\n", e.what());
		return 1;
	}
	return 0;
}