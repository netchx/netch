namespace DnsClient.Protocol
{
    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending an SSH fingerprint
    /// <para>
    /// SSHFP RRs are used to hold SSH fingerprints. Upon connecting to a
    /// host an SSH client may choose to query for this to check the fingerprint(s)
    /// </para>
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4255">RFC 4255</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc6594">RFC 6594</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc7479">RFC 7479</seealso>
    public class SshfpRecord : DnsResourceRecord
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="algorithm">The algorithm.</param>
        /// <param name="fingerprintType">The fingerprint type.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public SshfpRecord(ResourceRecordInfo info, SshfpAlgorithm algorithm, SshfpFingerprintType fingerprintType, string fingerprint) : base(info)
        {
            Algorithm = algorithm;
            FingerprintType = fingerprintType;
            Fingerprint = fingerprint;
        }

        /// <summary>
        /// Algorithm used for the fingerprint
        /// </summary>
        public SshfpAlgorithm Algorithm { get; }

        /// <summary>
        /// Fingerprint type used for the fingerprint
        /// </summary>
        public SshfpFingerprintType FingerprintType { get; }

        /// <summary>
        /// Fingerprint as defined in the RR
        /// </summary>
        public string Fingerprint { get; }

        private protected override string RecordToString()
        {
            return $"{(int)Algorithm} {(int)FingerprintType} {Fingerprint}";
        }
    }

    /// <summary>
    /// Algorithm used by <see cref="SshfpRecord"/>
    /// </summary>
    public enum SshfpAlgorithm
    {
        /// <summary>
        /// Reserved for later use
        /// </summary>
        Reserved = 0,

        /// <summary>
        /// RSA
        /// </summary>
        RSA = 1,

        /// <summary>
        /// DSS
        /// </summary>
        DSS = 2,

        /// <summary>
        /// Eliptic Curve DSA
        /// </summary>
        ECDSA = 3,

        /// <summary>
        /// Edwards-curve DSA
        /// </summary>
        Ed25519 = 4,
    }

    /// <summary>
    /// Fingerprint type used by <see cref="SshfpRecord"/>
    /// </summary>
    public enum SshfpFingerprintType
    {
        /// <summary>
        /// Reserved for later use
        /// </summary>
        Reserved = 0,

        /// <summary>
        /// SHA-1 fingerprint
        /// </summary>
        SHA1 = 1,

        /// <summary>
        /// SHA-256 fingerprint
        /// </summary>
        SHA256 = 2,
    }
}
