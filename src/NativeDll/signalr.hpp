#pragma once
#include <yvals.h>
#if _HAS_ITERATOR_DEBUGGING != 0
#error _HAS_ITERATOR_DEBUGGING must be defined 0 when _DEBUG is not defined
#endif
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

			SIGNALR_API virtual concurrency::task<void> OnClosed(const std::string& error);

			SIGNALR_API virtual concurrency::task<void> OnReconnected();

			SIGNALR_API virtual concurrency::task<void> OnReconnecting(const std::string& error);

			SIGNALR_API HubConnection(const std::string& pipeName, const std::string& serverName);

			SIGNALR_API HubConnection(const std::string& url, const std::function<concurrency::task<std::string>()>& accessTokenProvider);

			SIGNALR_API ~HubConnection();

			SIGNALR_API concurrency::task<void> Dispose();

			SIGNALR_API void Remove(const std::string& methodName);

			SIGNALR_API std::function<void()> On(const std::string& methodName, int32_t argc, const OnHandler& handler);

			SIGNALR_API concurrency::task<void> Start(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

			SIGNALR_API concurrency::task<void> Stop(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

		private:
			const std::function<concurrency::task<std::string>()> accessTokenProvider_;
			void* handle_;
			std::unordered_multimap<std::string, std::shared_ptr<const OnHandler>> ons_;
		};
	}
}