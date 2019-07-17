namespace DNS.Protocol
{
    public enum OperationCode
    {
        Query = 0,
        IQuery,
        Status,
        // Reserved = 3
        Notify = 4,
        Update,
    }
}
