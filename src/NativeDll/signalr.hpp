#pragma once
#include <msgpack/object.hpp>
#if __has_include(<ppltasks.h>)
#include <ppltasks.h>
#else
#include <pplx/pplxtasks.h>
#endif

#ifdef SIGNALRPP_EXPORTS
#define SIGNALR_API __declspec(dllexport)
#else
#define SIGNALR_API __declspec(dllimport)
#endif

namespace FlyByWireless
{
	namespace SignalRTunnel
	{
		class HubConnection
		{
		public:
			typedef std::function<concurrency::task<void>(const msgpack::object& object)> OnHandler;

			SIGNALR_API virtual concurrency::task<void> OnClosed(const char* error);

			SIGNALR_API virtual concurrency::task<void> OnReconnected();

			SIGNALR_API virtual concurrency::task<void> OnReconnecting(const char* error);

			SIGNALR_API HubConnection(const char* pipeName, const char* serverName = ".");

			SIGNALR_API HubConnection(const char* url, const std::function<concurrency::task<const char*>()>& accessTokenProvider);

			SIGNALR_API ~HubConnection();

			SIGNALR_API concurrency::task<void> Dispose();

			SIGNALR_API void Remove(const char* methodName);

			SIGNALR_API std::function<void()> On(const char* methodName, int32_t argc, const OnHandler& handler);

			SIGNALR_API concurrency::task<void> Start(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

			SIGNALR_API concurrency::task<void> Stop(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

		private:
			const void* state_;
		};
	}
}