# Redirector
```c++
typedef enum _AIO_TYPE {
	AIO_FILTERLOOPBACK,
	AIO_FILTERINTRANET,
	AIO_FILTERPARENT,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,
	AIO_FILTERDNS,

	AIO_ICMPING,

	AIO_DNSONLY,
	AIO_DNSPROX,
	AIO_DNSHOST,
	AIO_DNSPORT,

	AIO_TGTHOST,
	AIO_TGTPORT,
	AIO_TGTUSER,
	AIO_TGTPASS,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME
} AIO_TYPE;

__declspec(dllexport) BOOL __cdecl aio_register(LPWSTR value);
__declspec(dllexport) BOOL __cdecl aio_unregister(LPWSTR value);
__declspec(dllexport) BOOL __cdecl aio_dial(int name, LPWSTR value);
__declspec(dllexport) BOOL __cdecl aio_init();
__declspec(dllexport) void __cdecl aio_free();
__declspec(dllexport) ULONG64 __cdecl aio_getUP();
__declspec(dllexport) ULONG64 __cdecl aio_getDL();
```

```c#
private enum NameList : int
{
	AIO_FILTERLOOPBACK,
	AIO_FILTERINTRANET,
	AIO_FILTERPARENT,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,
	AIO_FILTERDNS,

	AIO_ICMPING,

	AIO_DNSONLY,
	AIO_DNSPROX,
	AIO_DNSHOST,
	AIO_DNSPORT,

	AIO_TGTHOST,
	AIO_TGTPORT,
	AIO_TGTUSER,
	AIO_TGTPASS,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME
}

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool aio_register([MarshalAs(UnmanagedType.LPWStr)] string value);

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool aio_unregister([MarshalAs(UnmanagedType.LPWStr)] string value);

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool aio_init();

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern void aio_free();

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern ulong aio_getUP();

[DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern ulong aio_getDL();
```
