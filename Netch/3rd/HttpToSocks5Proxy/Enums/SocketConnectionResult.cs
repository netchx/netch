namespace MihaZupan.Enums
{
    enum SocketConnectionResult
    {
        OK = 0,
        GeneralSocksServerFailure = 1,
        ConnectionNotAllowedByRuleset = 2,
        NetworkUnreachable = 3,
        HostUnreachable = 4,
        ConnectionRefused = 5,
        TTLExpired = 6,
        CommandNotSupported = 7,
        AddressTypeNotSupported = 8,

        // Library specific
        InvalidRequest = int.MinValue,
        UnknownError,
        AuthenticationError,
        ConnectionReset,
        ConnectionError,
        InvalidProxyResponse
    }
}