#include "Utils.h"

void BoostRelayThread()
{
	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_HIGHEST);
}

string ws2s(const wstring& str)
{
	if (str.empty())
		return {};

	const int srcLen = static_cast<int>(str.length());
	const int length = WideCharToMultiByte(CP_UTF8, 0, str.data(), srcLen, NULL, 0, NULL, NULL);
	if (length <= 0)
		return {};

	string data(static_cast<size_t>(length), '\0');
	WideCharToMultiByte(CP_UTF8, 0, str.data(), srcLen, data.data(), length, NULL, NULL);
	return data;
}

wstring s2ws(const string& str)
{
	if (str.empty())
		return {};

	const int srcLen = static_cast<int>(str.length());
	const int length = MultiByteToWideChar(CP_UTF8, 0, str.data(), srcLen, NULL, 0);
	if (length <= 0)
		return {};

	wstring data(static_cast<size_t>(length), L'\0');
	MultiByteToWideChar(CP_UTF8, 0, str.data(), srcLen, data.data(), length);
	return data;
}
