#include "Utils.h"

string ws2s(const wstring str)
{
	return wstring_convert<codecvt_utf8<wchar_t>, wchar_t>().to_bytes(str);
}

wstring s2ws(const string str)
{
	return wstring_convert<codecvt_utf8<wchar_t>, wchar_t>().from_bytes(str);
}
