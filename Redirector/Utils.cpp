#include "Utils.h"

string ws2s(const wstring str)
{
	char buffer[1024];
	memset(buffer, 0, sizeof(buffer));

	if (WideCharToMultiByte(CP_ACP, 0, str.c_str(), (int)str.length(), NULL, 0, NULL, NULL) > 1024)
	{
		return "Convert Failed";
	}

	WideCharToMultiByte(CP_ACP, 0, str.c_str(), (int)str.length(), buffer, 1024, NULL, NULL);
	return buffer;
}

wstring s2ws(const string str)
{
	wchar_t buffer[1024];
	memset(buffer, 0, sizeof(buffer));

	if (MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.length(), NULL, 0) > 1024)
	{
		return L"Convert Failed";
	}

	MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.length(), buffer, 1024);
	return buffer;
}
