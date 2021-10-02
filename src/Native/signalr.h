#pragma once

struct SignalREventHandlers
{
	void (*Closed)(void* context, const char* error, void(*callback)());
	void (*Reconnected)(void* context, void(*callback)());
	void (*Reconnecting)(void* context, const char* error, void(*callback)());
};
typedef void (*SignalRAction)();
typedef void (*SignalRAccessTokenProvider)(void* context, void (*callback)(const char* access_token));
typedef void (*SignalRCallback)(void* context, const char* error);

#ifdef __cplusplus
extern "C"
{
#endif

	void* signalr_build_with_named_pipe(const char* pipeName, const char* serverName, const struct SignalREventHandlers* handlers, void* context);

	void* signalr_build_with_url(const char* url, SignalRAccessTokenProvider accessTokenProvider, const struct SignalREventHandlers* handlers, void* context);

	void signalr_dispose(void* handle, SignalRCallback callback, void* context);

	void signalr_remove(void* handle, const char* methodName);

	SignalRAction signalr_on(void* handle, const char* methodName, const int argc, void(*handler)(void* context, const char* buffer, const int bufferSize, void(*callback)()), void* context);

	SignalRAction signalr_start(void* handle, SignalRCallback callback, void* context);

	SignalRAction signalr_stop(void* handle, SignalRCallback callback, void* context);

	// TODO: invoke, send

#ifdef __cplusplus
}
#endif