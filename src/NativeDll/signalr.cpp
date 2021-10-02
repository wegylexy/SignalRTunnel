#include "pch.h"
#include "signalr.hpp"
#include "signalr.h"
#include <msgpack/unpack.hpp>

using namespace std;
using namespace concurrency;
using namespace FlyByWireless::SignalRTunnel;

static void ThenCallback(const concurrency::task<void>& task, void(*callback)())
{
	task.then(
		[callback](const concurrency::task<void>& task)
		{
			try
			{
				task.get();
			}
			catch (...) {}
			callback();
		}
	);
}

static void ErrorCallback(void* context, const char* error)
{
	auto p = static_cast<concurrency::task_completion_event<void>*>(context);
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

concurrency::task<void> HubConnection::OnClosed(const std::string& error)
{
	return concurrency::task_from_result();
}

concurrency::task<void> HubConnection::OnReconnected()
{
	return concurrency::task_from_result();
}

concurrency::task<void> HubConnection::OnReconnecting(const std::string& error)
{
	return concurrency::task_from_result();
}

HubConnection::HubConnection(const std::string& pipeName, const std::string& serverName)
{
	handle_ = signalr_build_with_named_pipe(pipeName.c_str(), serverName.c_str(), &handlers, this);
}

HubConnection::HubConnection(const std::string& url, const std::function<concurrency::task<std::string>()>& accessTokenProvider) :
	accessTokenProvider_{ accessTokenProvider }
{
	handle_ = signalr_build_with_url(url.c_str(),
		[](void* context, void (*callback)(const char* access_token))
		{
			const auto& p = static_cast<HubConnection*>(context)->accessTokenProvider_;
			if (p)
			{
				p().then(
					[callback](const concurrency::task<std::string>& task)
					{
						try
						{
							const auto& accessToken = task.get();
							callback(accessToken.empty() ? nullptr : accessToken.c_str());
						}
						catch (...)
						{
							callback(nullptr);
						}
					}
				);
			}
			callback(nullptr);
		}, &handlers, this);
}

HubConnection::~HubConnection()
{
	try
	{
		Dispose().get();
	}
	catch (...) {}
}

concurrency::task<void> HubConnection::Dispose()
{
	if (handle_)
	{
		const auto tce = new concurrency::task_completion_event<void>{};
		signalr_dispose(handle_, &ErrorCallback, tce);
		handle_ = nullptr;
		ons_.clear();
		return concurrency::create_task(*tce);
	}
	return concurrency::task_from_result();
}

void HubConnection::Remove(const std::string& methodName)
{
	if (handle_)
	{
		signalr_remove(handle_, methodName.c_str());
		ons_.erase(methodName);
	}
	else
	{
		throw disposed;
	}
}

std::function<void()> HubConnection::On(const std::string& methodName, const int32_t argc, const OnHandler& handler)
{
	const auto s = make_shared<const OnHandler>(handler);
	const auto i = ons_.insert(make_pair(methodName, s));
	const auto r = signalr_on(handle_, methodName.c_str(), argc,
		[](void* context, const char* buffer, const int bufferSize, void(*callback)())
		{
			const auto m = msgpack::unpack(buffer, bufferSize);
			(*static_cast<OnHandler*>(context))(m.get()).then(
				[callback](const concurrency::task<void>& task)
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
		ons_.erase(i);
	};
}

concurrency::task<void> HubConnection::Start(const concurrency::cancellation_token& cancellationToken)
{
	if (handle_)
	{
		const auto tce = new concurrency::task_completion_event<void>{};
		const auto c = signalr_start(handle_, &ErrorCallback, tce);
		if (cancellationToken.is_cancelable())
		{
			cancellationToken.register_callback(c);
		}
		return concurrency::create_task(*tce, cancellationToken);
	}
	else
	{
		return concurrency::task_from_exception<void>(std::make_exception_ptr(disposed));
	}
}

concurrency::task<void> HubConnection::Stop(const concurrency::cancellation_token& cancellationToken)
{
	if (handle_)
	{
		const auto tce = new concurrency::task_completion_event<void>{};
		const auto c = signalr_stop(handle_, &ErrorCallback, tce);
		if (cancellationToken.is_cancelable())
		{
			cancellationToken.register_callback(c);
		}
		return concurrency::create_task(*tce, cancellationToken);
	}
	else
	{
		return concurrency::task_from_exception<void>(std::make_exception_ptr(disposed));
	}
}