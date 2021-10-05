#define _HAS_ITERATOR_DEBUGGING 0
#include <signalr.hpp>
#include <string>

using namespace std;
using namespace concurrency;
using namespace FlyByWireless::SignalRTunnel;

class Client : public HubConnection
{
public:
	task<void> OnClosed(const char* error) override
	{
		fputs("Closed\n", stderr);
		return HubConnection::OnClosed(error);
	}

	task<void> OnReconnected() override
	{
		fputs("Reconnected\n", stderr);
		return HubConnection::OnReconnected();
	}

	task<void> OnReconnecting(const char* error) override
	{
		fputs("Reconnecting\n", stderr);
		return HubConnection::OnReconnecting(error);
	}

	Client(const char* pipeName, const char* serverName = ".") : HubConnection{ pipeName, serverName }
	{ }
};

int main(int argc, const char** argv)
{
	try
	{
		Client hub{ argv[1], argc > 2 ? argv[2] : "." };
		task_completion_event<void> tce{};
		hub.On("ClientMethod1", 1,
			[tce](const msgpack::object& args)
			{
				const auto& s = args.via.array.ptr[0].via.str;
				string a{ s.ptr, static_cast<size_t>(s.size) };
				fprintf(stderr, "[\"%s\"]\n", a.c_str());
				// TODO: invoke server method
				return task_from_result().then(
					[tce]()
					{
						tce.set();
					}
				);
			});
		hub.Start().then(
			[&hub, tce]()
			{
				thread{ [tce]() {
					this_thread::sleep_for(chrono::milliseconds(1000));
					tce.set_exception(make_exception_ptr(task_canceled{"Timeout"}));
				} }.detach();
				return create_task(tce);
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
	catch (...)
	{
		fputs("Crashed\n", stderr);
		return 2;
	}
	fputs("Completed\n", stderr);
	return 0;
}