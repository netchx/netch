#pragma once
#ifndef IOCPPOOL_H
#define IOCPPOOL_H
#include "Based.h"

struct IocpOperation
{
	OVERLAPPED overlapped{};
	void (*complete)(IocpOperation* op, DWORD error, DWORD bytes) = nullptr;
};

static_assert(offsetof(IocpOperation, overlapped) == 0, "OVERLAPPED must be at offset 0 of IocpOperation");

namespace IocpPool
{
	bool Init();
	void Shutdown();
	bool Associate(HANDLE handle);
	bool Post(IocpOperation* op, DWORD bytes = 0);
}

#endif
