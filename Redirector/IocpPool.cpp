#include "IocpPool.h"

#include "Utils.h"

namespace
{
	constexpr ULONG_PTR ShutdownCompletionKey = 1;

	mutex g_iocpLock;
	condition_variable g_callCv;
	HANDLE g_iocp = NULL;
	vector<thread> g_workers;
	int g_refCount = 0;
	int g_activeCalls = 0;

	void WorkerLoop(HANDLE port)
	{
		BoostRelayThread();

		for (;;)
		{
			DWORD bytes = 0;
			ULONG_PTR key = 0;
			LPOVERLAPPED overlapped = nullptr;
			const BOOL ok = GetQueuedCompletionStatus(port, &bytes, &key, &overlapped, INFINITE);
			if (key == ShutdownCompletionKey && overlapped == nullptr)
				break;

			if (overlapped == nullptr)
			{
				if (!ok)
					break;
				continue;
			}

			auto* op = reinterpret_cast<IocpOperation*>(overlapped);
			const DWORD error = ok ? NO_ERROR : GetLastError();
			if (op->complete != nullptr)
				op->complete(op, error, bytes);
		}
	}

	HANDLE AcquireActivePort()
	{
		lock_guard<mutex> lg(g_iocpLock);
		if (g_iocp == NULL)
			return NULL;
		++g_activeCalls;
		return g_iocp;
	}

	void ReleaseActivePort()
	{
		lock_guard<mutex> lg(g_iocpLock);
		if (g_activeCalls > 0)
			--g_activeCalls;
		if (g_activeCalls == 0)
			g_callCv.notify_all();
	}
}

bool IocpPool::Init()
{
	lock_guard<mutex> lg(g_iocpLock);
	if (g_iocp != NULL)
	{
		++g_refCount;
		return true;
	}

	HANDLE port = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
	if (port == NULL)
	{
		printf("[Redirector][IocpPool::Init] CreateIoCompletionPort failed: %lu\n", GetLastError());
		return false;
	}

	const unsigned hw = thread::hardware_concurrency();
	const unsigned count = (std::clamp)(hw == 0 ? 4u : hw, 2u, 8u);

	vector<thread> workers;
	workers.reserve(count);
	try
	{
		for (unsigned i = 0; i < count; ++i)
			workers.emplace_back(WorkerLoop, port);
	}
	catch (...)
	{
		for (size_t i = 0; i < workers.size(); ++i)
			PostQueuedCompletionStatus(port, 0, ShutdownCompletionKey, nullptr);
		for (auto& w : workers)
		{
			if (w.joinable())
				w.join();
		}
		CloseHandle(port);
		return false;
	}

	g_iocp = port;
	g_workers.swap(workers);
	g_refCount = 1;
	return true;
}

void IocpPool::Shutdown()
{
	HANDLE port = NULL;
	vector<thread> workers;
	{
		unique_lock<mutex> ul(g_iocpLock);
		if (g_iocp == NULL)
			return;

		if (g_refCount > 1)
		{
			--g_refCount;
			return;
		}

		g_refCount = 0;
		port = g_iocp;
		g_iocp = NULL;
		workers.swap(g_workers);
		g_callCv.wait(ul, []() { return g_activeCalls == 0; });
	}

	for (size_t i = 0; i < workers.size(); ++i)
		PostQueuedCompletionStatus(port, 0, ShutdownCompletionKey, nullptr);

	for (auto& w : workers)
	{
		if (w.joinable())
			w.join();
	}

	CloseHandle(port);
}

bool IocpPool::Associate(HANDLE handle)
{
	if (handle == NULL || handle == INVALID_HANDLE_VALUE)
		return false;

	const HANDLE port = AcquireActivePort();
	if (port == NULL)
		return false;

	const bool ok = CreateIoCompletionPort(handle, port, 0, 0) != NULL;
	if (!ok)
	{
		printf("[Redirector][IocpPool::Associate] CreateIoCompletionPort failed: %lu\n", GetLastError());
		ReleaseActivePort();
		return false;
	}

	// Keep IOCP notifications uniform (always via GetQueuedCompletionStatus),
	// while avoiding unnecessary handle event signaling overhead.
	SetFileCompletionNotificationModes(handle, FILE_SKIP_SET_EVENT_ON_HANDLE);
	ReleaseActivePort();
	return true;
}

bool IocpPool::Post(IocpOperation* op, DWORD bytes)
{
	if (op == nullptr)
		return false;

	const HANDLE port = AcquireActivePort();
	if (port == NULL)
		return false;

	const bool ok = PostQueuedCompletionStatus(port, bytes, 0, &op->overlapped) != FALSE;
	ReleaseActivePort();
	return ok;
}
