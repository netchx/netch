using System;
using System.Collections.Generic;

namespace DnsClient
{
    /*
     * Reference RFC6895#section-2.3
     */

    /// <summary>
    /// Response codes of the <see cref="IDnsQueryResponse"/>.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6895#section-2.3">RFC 6895</seealso>
    public enum DnsResponseCode : short
    {
        /// <summary>
        /// No error condition
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        NoError = 0,

        /// <summary>
        /// Format error. The name server was unable to interpret the query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        FormatError = 1,

        /// <summary>
        /// Server failure. The name server was unable to process this query due to a problem with the name server.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        ServerFailure = 2,

        /// <summary>
        /// Name Error. Meaningful only for responses from an authoritative name server,
        /// this code signifies that the domain name referenced in the query does not exist.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        NotExistentDomain = 3,

        /// <summary>
        /// Not Implemented. The name server does not support the requested kind of query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        NotImplemented = 4,

        /// <summary>
        /// Refused. The name server refuses to perform the specified operation for policy reasons.
        /// For example, a name server may not wish to provide the information to the particular requester,
        /// or a name server may not wish to perform a particular operation (e.g., zone transfer) for particular data.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        Refused = 5,

        /// <summary>
        /// Name Exists when it should not.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        ExistingDomain = 6,

        /// <summary>
        /// Resource record set exists when it should not.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        ExistingResourceRecordSet = 7,

        /// <summary>
        /// Resource record set that should exist but does not.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        MissingResourceRecordSet = 8,

        /// <summary>
        /// Server Not Authoritative for zone / Not Authorized.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc2845">RFC 2845</seealso>
        NotAuthorized = 9,

        /// <summary>
        /// Name not contained in zone.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        NotZone = 10,

        /// <summary>
        /// Bad OPT Version or TSIG Signature Failure.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2671">RFC 2671</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc2845">RFC 2845</seealso>
        BadVersionOrBadSignature = 16,

        /// <summary>
        /// Key not recognized.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2845">RFC 2845</seealso>
        BadKey = 17,

        /// <summary>
        /// Signature out of time window.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2845">RFC 2845</seealso>
        BadTime = 18,

        /// <summary>
        /// Bad TKEY Mode.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2930">RFC 2930</seealso>
        BadMode = 19,

        /// <summary>
        /// Duplicate key name.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2930">RFC 2930</seealso>
        BadName = 20,

        /// <summary>
        /// Algorithm not supported.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2930">RFC 2930</seealso>
        BadAlgorithm = 21,

        /// <summary>
        /// Bad Truncation.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4635">RFC 4635</seealso>
        BadTruncation = 22,

        /// <summary>
        /// Bad/missing Server Cookie
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7873">RFC 7873</seealso>
        BadCookie = 23,

        /// <summary>
        /// Unknown error.
        /// </summary>
        Unassigned = 666,

        /// <summary>
        /// Indicates a timeout error. Connection to the remote server couldn't be established.
        /// </summary>
        ConnectionTimeout = 999
    }

    /// <summary>
    /// A DnsClient specific exception transporting additional information about the query causing this exception.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class DnsResponseException : Exception
    {
        /// <summary>
        /// Gets the response code.
        /// </summary>
        /// <value>
        /// The response code.
        /// </value>
        public DnsResponseCode Code { get; }

        /// <summary>
        /// Gets the audit trail if <see cref="ILookupClient.EnableAuditTrail"/>. as set to <c>true</c>, <c>null</c> otherwise.
        /// </summary>
        /// <value>
        /// The audit trail.
        /// </value>
        public string AuditTrail { get; internal set; }

        /// <summary>
        /// Gets a human readable error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string DnsError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with <see cref="Code"/> set to <see cref="DnsResponseCode.Unassigned"/>.
        /// </summary>
        public DnsResponseException() : base(DnsResponseCodeText.Unassigned)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with <see cref="Code"/> set to <see cref="DnsResponseCode.Unassigned"/>
        /// and a custom <paramref name="message"/>.
        /// </summary>
        public DnsResponseException(string message) : base(message)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with the standard error text for the given <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code) : base(DnsResponseCodeText.GetErrorText(code))
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with <see cref="Code"/> set to <see cref="DnsResponseCode.Unassigned"/>
        /// and a custom <paramref name="message"/> and inner <see cref="Exception"/>.
        /// </summary>
        public DnsResponseException(string message, Exception innerException) : base(message, innerException)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with a custom <paramref name="message"/> and the given <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code, string message) : base(message)
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResponseException"/> class 
        /// with a custom <paramref name="message"/> and the given <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }
    }

    internal static class DnsResponseCodeText
    {
        internal const string BADALG = "Algorithm not supported";
        internal const string BADCOOKIE = "Bad/missing Server Cookie";
        internal const string BADKEY = "Key not recognized";
        internal const string BADMODE = "Bad TKEY Mode";
        internal const string BADNAME = "Duplicate key name";
        internal const string BADSIG = "TSIG Signature Failure";
        internal const string BADTIME = "Signature out of time window";
        internal const string BADTRUNC = "Bad Truncation";
        internal const string BADVERS = "Bad OPT Version";
        internal const string FormErr = "Format Error";
        internal const string NoError = "No Error";
        internal const string NotAuth = "Server Not Authoritative for zone or Not Authorized";
        internal const string NotImp = "Not Implemented";
        internal const string NotZone = "Name not contained in zone";
        internal const string NXDomain = "Non-Existent Domain";
        internal const string NXRRSet = "RR Set that should exist does not";
        internal const string Refused = "Query Refused";
        internal const string ServFail = "Server Failure";
        internal const string Unassigned = "Unknown Error";
        internal const string YXDomain = "Name Exists when it should not";
        internal const string YXRRSet = "RR Set Exists when it should not";

        private static readonly Dictionary<DnsResponseCode, string> errors = new Dictionary<DnsResponseCode, string>()
        {
            { DnsResponseCode.NoError, DnsResponseCodeText.NoError },
            { DnsResponseCode.FormatError, DnsResponseCodeText.FormErr },
            { DnsResponseCode.ServerFailure, DnsResponseCodeText.ServFail },
            { DnsResponseCode.NotExistentDomain, DnsResponseCodeText.NXDomain },
            { DnsResponseCode.NotImplemented, DnsResponseCodeText.NotImp },
            { DnsResponseCode.Refused, DnsResponseCodeText.Refused },
            { DnsResponseCode.ExistingDomain, DnsResponseCodeText.YXDomain },
            { DnsResponseCode.ExistingResourceRecordSet, DnsResponseCodeText.YXRRSet },
            { DnsResponseCode.MissingResourceRecordSet, DnsResponseCodeText.NXRRSet },
            { DnsResponseCode.NotAuthorized, DnsResponseCodeText.NotAuth },
            { DnsResponseCode.NotZone, DnsResponseCodeText.NotZone },
            { DnsResponseCode.BadVersionOrBadSignature, DnsResponseCodeText.BADVERS },
            { DnsResponseCode.BadKey, DnsResponseCodeText.BADKEY },
            { DnsResponseCode.BadTime, DnsResponseCodeText.BADTIME },
            { DnsResponseCode.BadMode, DnsResponseCodeText.BADMODE },
            { DnsResponseCode.BadName, DnsResponseCodeText.BADNAME },
            { DnsResponseCode.BadAlgorithm, DnsResponseCodeText.BADALG },
            { DnsResponseCode.BadTruncation, DnsResponseCodeText.BADTRUNC },
            { DnsResponseCode.BadCookie, DnsResponseCodeText.BADCOOKIE },
        };

        public static string GetErrorText(DnsResponseCode code)
        {
            if (!errors.ContainsKey(code))
            {
                return Unassigned;
            }

            return errors[code];
        }
    }
}