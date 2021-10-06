#include "pch.h"
#include "signalr.hpp"
#include "signalr.h"
#include <sstream>
#include <msgpack/unpack.hpp>

using namespace std;
using namespace concurrency;
using namespace FlyByWireless::SignalRTunnel;

static void ThenCallback(const task<void>& t, void(*callback)())
{
	t.then(
		[callback](const task<void>& t)
		{
			try
			{
				t.get();
			}
			catch (...) {}
			callback();
		}
	);
}

static void ErrorCallback(void* context, const char* error)
{
	auto p = static_cast<task_completion_event<void>*>(context);
	if (error)
	{
		p->set_exception(make_exception_ptr(std::runtime_error(error)));
	}
	else
	{
		p->set();
	}
	delete p;
}

static const SignalREventHandlers handlers =
{
	[](void* context, const char* error, void(*callback)())
	{
		ThenCallback(static_cast<HubConnection*>(context)->OnClosed(error), callback);
	},
	[](void* context, void(*callback)())
	{
		ThenCallback(static_cast<HubConnection*>(context)->OnReconnected(), callback);
	},
	[](void* context, const char* error, void(*callback)())
	{
		ThenCallback(static_cast<HubConnection*>(context)->OnReconnecting(error), callback);
	}
};

static const std::runtime_error disposed{ "Cannot access a disposed object." };

class HubConnectionState
{
	friend HubConnection;

	void* handle_;
	const std::function<task<const char*>()> accessTokenProvider_;
	std::unordered_multimap<std::string, std::shared_ptr<const HubConnection::OnHandler>> ons_;

	HubConnectionState(const char* pipeName, const char* serverName) :
		handle_{ signalr_build_with_named_pipe(pipeName, serverName, &handlers, this) },
		accessTokenProvider_{}
	{ }

	HubConnectionState(const char* url, const function<task<const char*>()>& accessTokenProvider) : handle_
	{
		signalr_build_with_url(url,
			[](void* context, void (*callback)(const char* access_token))
			{
				const auto& p = static_cast<HubConnectionState*>(context)->accessTokenProvider_;
				if (p)
				{
					p().then(
						[callback](const task<const char*>& task)
						{
							try
							{
								callback(task.get());
							}
							catch (...)
							{
								callback(nullptr);
							}
						}
					);
				}
				callback(nullptr);
			}, &handlers, this)
	}, accessTokenProvider_{ accessTokenProvider } { }
};

task<void> HubConnection::OnClosed(const char* error)
{
	return task_from_result();
}

task<void> HubConnection::OnReconnected()
{
	return task_from_result();
}

task<void> HubConnection::OnReconnecting(const char* error)
{
	return task_from_result();
}

HubConnection::HubConnection(const char* pipeName, const char* serverName) :
	state_{ new HubConnectionState{ pipeName, serverName } }
{ }

HubConnection::HubConnection(const char* url, const std::function<task<const char*>()>& accessTokenProvider) :
	state_{ new HubConnectionState{ url, accessTokenProvider} }
{ }

HubConnection::~HubConnection()
{
	try
	{
		Dispose().get();
		delete (HubConnectionState*)state_;
	}
	catch (...) {}
}

task<void> HubConnection::Dispose()
{
	auto& state = *((HubConnectionState*)state_);
	auto& handle = state.handle_;
	if (handle)
	{
		const auto tce = new task_completion_event<void>{};
		signalr_dispose(handle, &ErrorCallback, tce);
		handle = nullptr;
		state.ons_.clear();
		return create_task(*tce);
	}
	return task_from_result();
}

void HubConnection::Remove(const char* methodName)
{
	auto& state = *((HubConnectionState*)state_);
	const auto handle = state.handle_;
	if (handle)
	{
		signalr_remove(handle, methodName);
		state.ons_.erase(methodName);
	}
	else
	{
		throw disposed;
	}
}

function<void()> HubConnection::On(const char* methodName, const int32_t argc, const OnHandler& handler)
{
	auto& state = *((HubConnectionState*)state_);
	const auto handle = state.handle_;
	if (handle)
	{
		const auto s = make_shared<const OnHandler>(handler);
		const auto i = state.ons_.insert(make_pair(string{ methodName }, s));
		const auto r = signalr_on(handle, methodName, argc,
			[](void* context, const char* buffer, const int bufferSize, void(*callback)())
			{
				const auto m = msgpack::unpack(buffer, bufferSize);
				(*static_cast<OnHandler*>(context))(m.get()).then(
					[callback](const task<void>& task)
					{
						try
						{
							task.get();
						}
						catch (...) {}
						callback();
					});
			}, const_cast<void*>(static_cast<const void*>(s.get())));
		return [this, i, r]()
		{
			r();
			((HubConnectionState*)state_)->ons_.erase(i);
		};
	}
	else
	{
		throw disposed;
	}
}

task<void> HubConnection::Start(const cancellation_token& cancellationToken)
{
	const auto handle = ((HubConnectionState*)state_)->handle_;
	if (handle)
	{
		const auto tce = new task_completion_event<void>{};
		const auto c = signalr_start(handle, &ErrorCallback, tce);
		if (cancellationToken.is_cancelable())
		{
			cancellationToken.register_callback(c);
		}
		return create_task(*tce, cancellationToken);
	}
	else
	{
		throw disposed;
	}
}

task<void> HubConnection::Stop(const cancellation_token& cancellationToken)
{
	const auto handle = ((HubConnectionState*)state_)->handle_;
	if (handle)
	{
		const auto tce = new task_completion_event<void>{};
		const auto c = signalr_stop(handle, &ErrorCallback, tce);
		if (cancellationToken.is_cancelable())
		{
			cancellationToken.register_callback(c);
		}
		return create_task(*tce, cancellationToken);
	}
	else
	{
		throw disposed;
	}
}

task<shared_ptr<msgpack::object_handle>> HubConnection::InvokeCore(const char* methodName, const msgpack::object& args, const cancellation_token& cancellationToken)
{
	const auto handle = ((HubConnectionState*)state_)->handle_;
	if (handle)
	{
		stringstream stream{};
		msgpack::pack(stream, args);
		const auto buffer = stream.str();
		stream.clear();
		const auto tce = new task_completion_event<shared_ptr<msgpack::object_handle>>{};
		const auto c = signalr_invoke_core(handle, methodName, buffer.data(), buffer.size(),
			[](void* context, const char* error, const char* buffer, const int bufferSize)
			{
				auto p = static_cast<task_completion_event<shared_ptr<msgpack::object_handle>>*>(context);
				if (error)
				{
					p->set_exception(make_exception_ptr(std::runtime_error(error)));
				}
				else
				{
					const auto s = make_shared<msgpack::object_handle>();
					msgpack::unpack(*s, buffer, bufferSize);
					p->set(s);
				}
				delete p;
			}, tce);
		return create_task(*tce, cancellationToken);
	}
	else
	{
		throw disposed;
	}
}

task<void> HubConnection::SendCore(const char* methodName, const msgpack::object& args, const cancellation_token& cancellationToken)
{
	const auto handle = ((HubConnectionState*)state_)->handle_;
	if (handle)
	{
		stringstream stream{};
		msgpack::pack(stream, args);
		const auto buffer = stream.str();
		stream.clear();
		const auto tce = new task_completion_event<void>{};
		const auto c = signalr_send_core(handle, methodName, buffer.data(), buffer.size(), &ErrorCallback, tce);
		return create_task(*tce, cancellationToken);
	}
	else
	{
		throw disposed;
	}
}