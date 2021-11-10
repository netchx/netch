#include "WaitGroup.h"

void WaitGroup::Add(int size)
{
	this->counter += size;
}

void WaitGroup::Done()
{
	if (--this->counter <= 0)
		this->condition.notify_all();
}

void WaitGroup::Wait()
{
	std::unique_lock<std::mutex> lock(this->mutex);

	condition.wait(lock, [&] { return counter <= 0; });
}
