#include <signalr.hpp>
#include <sstream>

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
		Client hub{ argv[1], argv[2] };
		task_completion_event<string> tce{};
		hub.On("MsgPackTask", function{ [tce]() { return task_from_result(); } });
		hub.On("MsgPackVoid", function{ [tce]() {} });
		hub.On("MsgPackAsTask", function{ [tce](const bool&, const char&, const short&, const int&, const float&, const double&) { return task_from_result(); } });
		hub.On("MsgPackAsVoid", function{ [tce](const bool&, const char&, const short&, const int&, const float&, const double&) {} });
		hub.On("MsgPackTimeTask", function{ [tce](const chrono::system_clock::time_point&) { return task_from_result(); } });
		hub.On("MsgPackTimeVoid", // .NET DateTime => MessagePack timestamp => C++ time_point
			function{ [tce](const chrono::system_clock::time_point& tp)
			{
				const auto received = chrono::system_clock::now();
				auto t = chrono::system_clock::to_time_t(tp);
				if (abs(chrono::duration_cast<chrono::seconds>(received - tp).count()) > 1)
				{
					tm m;
					if (!gmtime_s(&m, &t))
					{
						ostringstream stream{};
						stream << put_time(&m, "%FT%TZ");
						fprintf(stderr, "Sent: %s\n", stream.str().c_str());
						stream.clear();
						t = chrono::system_clock::to_time_t(received);
						gmtime_s(&m, &t);
						stream << put_time(&m, "%FT%TZ");
						fprintf(stderr, "Recevied: %s\n", stream.str().c_str());
					}
				}
			} }
		);
		hub.On("ClientMethod1",
			function{ [&hub, tce](const string& a)
			{
				tce.set(a);
			} }
		);
		hub.Start().then([expected = argv[3], &hub, tce]()
		{
			puts("Ready");
			return hub.Invoke<void>("HubMethod1", expected).then(
				[tce]()
				{
					thread{ [tce]() {
						this_thread::sleep_for(chrono::milliseconds(1000));
						tce.set_exception(make_exception_ptr(task_canceled{"Timeout"}));
					} }.detach();
					return create_task(tce);
				}
			).then(
				[expected](const string& actual)
				{
					if (actual != expected)
					{
						throw invalid_argument{ "Unexpected value" };
					}
				}
			)
					;
		}).then(
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
	puts("Completed");
	return 0;
}