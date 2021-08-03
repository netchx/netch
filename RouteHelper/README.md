# RouteHelper
```cpp
ULONG ConvertLuidToIndex(ULONG64 id);
BOOL CreateIPv4(const char* address, const char* netmask, ULONG index);
BOOL CreateUnicastIP(USHORT inet, const char* address, UINT8 cidr, ULONG index);
BOOL RefreshIPTable(USHORT inet, ULONG index);
BOOL CreateRoute(USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric);
BOOL DeleteRoute(USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric);
```

```csharp
[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern ulong ConvertLuidToIndex(ulong id);

[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool CreateIPv4(string address, string netmask, ulong index);

[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool CreateUnicastIP(AddressFamily inet, string address, byte cidr, ulong index);

[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool RefreshIPTable(AddressFamily inet, ulong index);

[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, ulong metric);

[DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, ulong metric);
```
