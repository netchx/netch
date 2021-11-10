#pragma once
#ifndef WAITGROUP_H
#define WAITGROUP_H
#include "Based.h"

class WaitGroup {
public:
	void Add(int size);
	void Done();
	void Wait();

private:
	std::mutex mutex;
	std::atomic_int counter;
	std::condition_variable condition;
};

#endif
