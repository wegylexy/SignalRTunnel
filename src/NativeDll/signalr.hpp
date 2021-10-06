#pragma once
#include <msgpack.hpp>
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
			typedef std::function<concurrency::task<void>(const msgpack::object& args)> OnHandler;

			SIGNALR_API virtual concurrency::task<void> OnClosed(const char* error);

			SIGNALR_API virtual concurrency::task<void> OnReconnected();

			SIGNALR_API virtual concurrency::task<void> OnReconnecting(const char* error);

			SIGNALR_API HubConnection(const char* pipeName, const char* serverName = ".");

			SIGNALR_API HubConnection(const char* url, const std::function<concurrency::task<const char*>()>& accessTokenProvider);

			SIGNALR_API ~HubConnection();

			SIGNALR_API concurrency::task<void> Dispose();

			SIGNALR_API void Remove(const char* methodName);

			SIGNALR_API std::function<void()> On(const char* methodName, int32_t argc, const OnHandler& handler);

			inline std::function<void()> On(const char* methodName, const std::function<void()>& handler)
			{
				return On(methodName, 0,
					[handler](const msgpack::object& o)
					{
						handler();
						return concurrency::task_from_result();
					}
				);
			}

			inline std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>()>& handler)
			{
				return On(methodName, 0,
					[handler](const msgpack::object& o)
					{
						return handler();
					}
				);
			}

			template<typename T1>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&)>& handler)
			{
				return On(methodName, 1,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						msgpack::type::make_define_array(a1).msgpack_unpack(o);
						handler(a1);
					}
				);
			}

			template<typename T1>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&)>& handler)
			{
				return On(methodName, 1,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						msgpack::type::make_define_array(a1).msgpack_unpack(o);
						return handler(a1);
					}
				);
			}

			template<typename T1, typename T2>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&)>& handler)
			{
				return On(methodName, 2,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						msgpack::type::make_define_array(a1, a2).msgpack_unpack(o);
						handler(a1, a2);
					}
				);
			}

			template<typename T1, typename T2>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&)>& handler)
			{
				return On(methodName, 2,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						msgpack::type::make_define_array(a1, a2).msgpack_unpack(o);
						return handler(a1, a2);
					}
				);
			}

			template<typename T1, typename T2, typename T3>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&)>& handler)
			{
				return On(methodName, 3,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						msgpack::type::make_define_array(a1, a2, a3).msgpack_unpack(o);
						handler(a1, a2, a3);
					}
				);
			}

			template<typename T1, typename T2, typename T3>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&)>& handler)
			{
				return On(methodName, 3,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						msgpack::type::make_define_array(a1, a2, a3).msgpack_unpack(o);
						return handler(a1, a2, a3);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&, const T4&)>& handler)
			{
				return On(methodName, 4,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						msgpack::type::make_define_array(a1, a2, a3, a4).msgpack_unpack(o);
						handler(a1, a2, a3, a4);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&, const T4&)>& handler)
			{
				return On(methodName, 4,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						msgpack::type::make_define_array(a1, a2, a3, a4).msgpack_unpack(o);
						return handler(a1, a2, a3, a4);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&, const T4&, const T5&)>& handler)
			{
				return On(methodName, 5,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5).msgpack_unpack(o);
						handler(a1, a2, a3, a4, a5);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&, const T4&, const T5&)>& handler)
			{
				return On(methodName, 5,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5).msgpack_unpack(o);
						return handler(a1, a2, a3, a4, a5);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&)>& handler)
			{
				return On(methodName, 6,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6).msgpack_unpack(o);
						handler(a1, a2, a3, a4, a5, a6);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&)>& handler)
			{
				return On(methodName, 6,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6).msgpack_unpack(o);
						return handler(a1, a2, a3, a4, a5, a6);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&, const T7&)>& handler)
			{
				return On(methodName, 7,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						T7 a7{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6, a7).msgpack_unpack(o);
						handler(a1, a2, a3, a4, a5, a6, a7);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&, const T7&)>& handler)
			{
				return On(methodName, 7,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						T7 a7{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6, a7).msgpack_unpack(o);
						return handler(a1, a2, a3, a4, a5, a6, a7);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8>
			std::function<void()> On(const char* methodName, const std::function<void(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&, const T7&, const T8&)>& handler)
			{
				return On(methodName, 8,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						T7 a7{};
						T8 a8{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6, a7, a8).msgpack_unpack(o);
						handler(a1, a2, a3, a4, a5, a6, a7, a8);
					}
				);
			}

			template<typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8>
			std::function<void()> On(const char* methodName, const std::function<concurrency::task<void>(const T1&, const T2&, const T3&, const T4&, const T5&, const T6&, const T7&, const T8&)>& handler)
			{
				return On(methodName, 8,
					[handler](const msgpack::object& o)
					{
						T1 a1{};
						T2 a2{};
						T3 a3{};
						T4 a4{};
						T5 a5{};
						T6 a6{};
						T7 a7{};
						T8 a8{};
						msgpack::type::make_define_array(a1, a2, a3, a4, a5, a6, a7, a8).msgpack_unpack(o);
						return handler(a1, a2, a3, a4, a5, a6, a7, a8);
					}
				);
			}

			SIGNALR_API concurrency::task<void> Start(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

			SIGNALR_API concurrency::task<void> Stop(const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

			SIGNALR_API concurrency::task<msgpack::object> InvokeCore(const char* methodName, const msgpack::object& args, const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

			SIGNALR_API concurrency::task<void> SendCore(const char* methodName, const msgpack::object& args, const concurrency::cancellation_token& cancellationToken = concurrency::cancellation_token::none());

		private:
			const void* state_;
		};
	}
}