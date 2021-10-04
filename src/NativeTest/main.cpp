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
		printf("Creating for %s at %s\n", argv[1], ".");
		Client hub{ argv[1], "." };
		// TODO: on
		puts("Starting");
		hub.Start().then(
			[&hub]()
			{
				puts("Started");
				// TODO: invoke / send
			}
		).then(
			[&hub]()
			{
				puts("Stopping");
				return hub.Stop();
			}
		).then(
			[&hub](const task<void>& task)
			{
				try
				{
					task.get();
					puts("Stopped");
				}
				catch (const exception& ex)
				{
					fprintf(stderr, "%s\n", ex.what());
				}
				catch (...)
				{
					fputs("Crashed\n", stderr);
				}
				return hub.Dispose();
			}
		).get(
		);
			puts("Disposed");
	}
	catch (const exception& e)
	{
		fprintf(stderr, "%s\n", e.what());
	}
	return 0;
}