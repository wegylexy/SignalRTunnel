#pragma once

struct SignalREventHandlers
{
	void(Closed*)(void* context, const wchar_t* error, void(*callback)());
	void(Reconnected*)(void* context, void(*callback)());
	void(Reconnecting*)(void* context, const wchar_t* error, void(*callback)());
};
typedef void (SignalRCancel*)();
typedef void (SignalRAccessTokenProvider*)(void* context, void (*callback)(const wchar_t* access_token));
typedef void (SignalRCallback*)(void* context, const wchar_t* error);

#ifdef __cplusplus
extern "C"
{
#endif

	void* signalr_build_with_named_pipe(const wchar_t* pipeName, const wchar_t* serverName, const SignalREventHandlers* handlers, void* context);

	void* signalr_build_with_url(const wchar_t* url, SignalRAccessTokenProvider accessTokenProvider, const SignalREventHandlers* handlers, void* context);

	void signalr_dispose(void* handle, SignalRCallback callback, void* context);

	SignalRCancel signalr_start(void* handle, SignalRCallback callback, void* context);

	SignalRCancel signalr_stop(void* handle, SignalRCallback callback, void* context);

	// TODO: on, invoke, send

#ifdef __cplusplus
}

// TODO: C++
#endif