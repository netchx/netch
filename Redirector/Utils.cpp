#include "Utils.h"

string ws2s(const wstring str)
{
	auto length = WideCharToMultiByte(CP_ACP, 0, str.c_str(), (int)str.length(), NULL, 0, NULL, NULL);
	auto buffer = new char[length]();

	WideCharToMultiByte(CP_ACP, 0, str.c_str(), (int)str.length(), buffer, length, NULL, NULL);

	auto data = string(buffer);
	delete[] buffer;

	return data;
}

wstring s2ws(const string str)
{
	auto length = MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.length(), NULL, 0);
	auto buffer = new wchar_t[length]();

	MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.length(), buffer, length);

	auto data = wstring(buffer);
	delete[] buffer;

	return data;
}
