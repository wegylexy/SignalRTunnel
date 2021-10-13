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
		hub.On("MsgPackTask", function{ []() { return task_from_result(); } });
		hub.On("MsgPackVoid", function{ []() {} });
		hub.On("MsgPackAsTask", function{ [](const bool&, const char&, const short&, const int&, const float&, const double&) { return task_from_result(); } });
		hub.On("MsgPackAsVoid", function{ [](const bool&, const char&, const short&, const int&, const float&, const double&) {} });
		hub.On("MsgPackTimeTask", function{ [](const chrono::system_clock::time_point&) { return task_from_result(); } });
		task_completion_event<chrono::system_clock::time_point> tceTime{};
		const auto offTime = hub.On("MsgPackTimeVoid", // .NET DateTime => MessagePack timestamp => C++ time_point
			function{ [tceTime](const chrono::system_clock::time_point& a)
			{
				fputs("Setting time.\n", stderr);
				fflush(stderr);
				tceTime.set(a);
				fputs("Set time.\n", stderr);
				fflush(stderr);
			} }
		);
		const auto expected = argv[3];
		hub.Start().then(
			[]()
			{
				puts("Ready");
				fflush(stdout);
			}
		).then(
			[&hub, tceTime, offTime]()
			{
				fputs("Waiting for time...\n", stderr);
				fflush(stderr);
				return create_task(tceTime).then(
					[offTime](const chrono::system_clock::time_point& a)
					{
						fputs("Time received.\n", stderr);
						fflush(stderr);
						offTime(); // TODO: test against further invocations
						const auto expected = chrono::system_clock::now();
						auto t = chrono::system_clock::to_time_t(a);
						if (abs(chrono::duration_cast<chrono::seconds>(expected - a).count()) > 1)
						{
							tm m;
							if (!gmtime_s(&m, &t))
							{
								ostringstream stream{};
								stream << put_time(&m, "%FT%TZ");
								fprintf(stderr, "Actual: %s\n", stream.str().c_str());
								stream.clear();
								t = chrono::system_clock::to_time_t(expected);
								gmtime_s(&m, &t);
								stream << put_time(&m, "%FT%TZ");
								fprintf(stderr, "Expected: %s\n", stream.str().c_str());
								fflush(stderr);
							}
							throw invalid_argument{ "Unexpected time." };
						}
					}
				);
			}
		).then(
			[expected, &hub]()
			{
				task_completion_event<string> tceString{};
				const auto offString = hub.On("ClientMethod1",
					function{ [&hub, tceString](const string& a)
					{
						fputs("Setting string.\n", stderr);
						fflush(stderr);
						tceString.set(a);
						fputs("Set string.\n", stderr);
						fflush(stderr);
					} }
				);
				fputs("Invoking...\n", stderr);
				fflush(stderr);
				return hub.Send("HubMethod1", expected).then(
					[tceString]()
					{
						fputs("Invoked.\n", stderr);
						fflush(stderr);
						thread{ [tceString]() {
							this_thread::sleep_for(200ms);
							tceString.set_exception(make_exception_ptr(task_canceled{"Timeout"}));
						} }.detach();
						fputs("Waiting for string...\n", stderr);
						fflush(stderr);
						return create_task(tceString);
					}
				).then(
					[expected, offString](const string& actual)
					{
						fputs("String received.\n", stderr);
						fflush(stderr);
						offString(); // TODO: test against further invocations
						if (actual != expected)
						{
							fprintf(stderr, "Expected: %s\n", expected);
							fprintf(stderr, "Actual: %s\n", actual.c_str());
							fflush(stderr);
							throw invalid_argument{ "Unexpected value" };
						}
					}
				)
						;
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
		).get()
				;
	}
	catch (const exception& e)
	{
		fprintf(stderr, "Exception: %s\n", e.what());
		fflush(stderr);
		return 1;
	}
	catch (...)
	{
		fputs("Crashed\n", stderr);
		fflush(stderr);
		return 2;
	}
	puts("Completed");
	fflush(stdout);
	return 0;
}