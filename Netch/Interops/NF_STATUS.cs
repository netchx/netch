namespace Netch.Interops
{
    public enum NF_STATUS : int
    {
        NF_STATUS_SUCCESS = 0,
        NF_STATUS_FAIL = -1,
        NF_STATUS_INVALID_ENDPOINT_ID = -2,
        NF_STATUS_NOT_INITIALIZED = -3,
        NF_STATUS_IO_ERROR = -4
    }
}