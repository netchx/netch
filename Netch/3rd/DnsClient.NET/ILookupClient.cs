using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace DnsClient
{
    /// <summary>
    /// The contract for the LookupClient.
    /// <para>
    /// The interfaces for the query methods and the lookup client properties are separated so that one can
    /// inject or expose only the <see cref="IDnsQuery"/> without exposing the configuration options.
    /// </para>
    /// </summary>
    public interface ILookupClient : IDnsQuery
    {
        /// <summary>
        /// Gets the list of configured default name servers.
        /// </summary>
        IReadOnlyCollection<NameServer> NameServers { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        // all settings will be moved into DnsQueryOptions/LookupClientOptions
        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        TimeSpan? MinimumCacheTimeout { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool EnableAuditTrail { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseCache { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool Recursion { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        int Retries { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool ThrowDnsErrors { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseRandomNameServer { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool ContinueOnDnsError { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        TimeSpan Timeout { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseTcpFallback { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseTcpOnly { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQueryOptions
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private TimeSpan _timeout = s_defaultTimeout;

        // TODO: evalualte if defaulting resolveNameServers to false here and in the query plan, fall back to configured nameservers of the client
        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/> without name servers.
        /// </summary>
        /// <remarks>
        /// If no nameservers are configured, a query will fallback to the nameservers already configured on the <see cref="LookupClient"/> instance.
        /// </remarks>
        /// <param name="resolveNameServers">If set to <c>true</c>, <see cref="NameServer.ResolveNameServers(bool, bool)"/>
        /// will be used to get a list of nameservers.</param>
        public DnsQueryOptions(bool resolveNameServers = false)
            : this(resolveNameServers ? NameServer.ResolveNameServers() : null)
        {
            AutoResolvedNameServers = resolveNameServers;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/> with one name server.
        /// <see cref="IPAddress"/> or <see cref="IPEndPoint"/> can be used as well thanks to implicit conversion.
        /// </summary>
        /// <param name="nameServer">The name servers.</param>
        public DnsQueryOptions(NameServer nameServer)
            : this(new[] { nameServer })
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public DnsQueryOptions(IReadOnlyCollection<NameServer> nameServers)
        {
            if (nameServers != null && nameServers.Count > 0)
            {
                NameServers = nameServers.ToList();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public DnsQueryOptions(params NameServer[] nameServers)
        {
            if (nameServers != null && nameServers.Length > 0)
            {
                NameServers = nameServers.ToList();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public DnsQueryOptions(params IPEndPoint[] nameServers)
           : this(nameServers.Select(p => (NameServer)p).ToArray())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public DnsQueryOptions(params IPAddress[] nameServers)
            : this(nameServers.Select(p => (NameServer)p).ToArray())
        {
        }

        /// <summary>
        /// Gets or sets a flag indicating whether each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; set; } = false;

        /// <summary>
        /// Gets a flag indicating whether the name server collection was manually defined or automatically resolved
        /// </summary>
        public bool AutoResolvedNameServers { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// </remarks>
        public bool UseCache { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of name servers which should be used to query.
        /// </summary>
        public IList<NameServer> NameServers { get; set; } = new List<NameServer>();

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>5</c>.
        /// <para>
        /// If all configured <see cref="NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; set; } = 5;

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; set; } = true;

        /// <summary>
        /// Gets or sets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether Tcp should be used in case a Udp response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no or incomplete answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether Udp should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if Udp cannot be used because of your firewall rules for example.
        /// Also, zone transfers (see <see cref="QueryType.AXFR"/>) must use TCP only.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; set; } = false;

        /// <summary>
        /// Converts the query options into readonly settings.
        /// </summary>
        /// <param name="fromOptions">The options.</param>
        public static implicit operator DnsQuerySettings(DnsQueryOptions fromOptions)
        {
            if (fromOptions == null)
            {
                return null;
            }

            return new DnsQuerySettings(fromOptions);
        }
    }

    /// <summary>
    /// The options used to configure defaults in <see cref="LookupClient"/> and to optionally use specific settings per query.
    /// </summary>
    public class LookupClientOptions : DnsQueryOptions
    {
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;

        // max is 24 days
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private TimeSpan? _minimumCacheTimeout;

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/> without name servers.
        /// </summary>
        public LookupClientOptions(bool resolveNameServers = true)
            : base(resolveNameServers)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/> with one name server.
        /// <see cref="IPAddress"/> or <see cref="IPEndPoint"/> can be used as well thanks to implicit conversion.
        /// </summary>
        /// <param name="nameServer">The name servers.</param>
        public LookupClientOptions(NameServer nameServer)
            : base(nameServer)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public LookupClientOptions(params NameServer[] nameServers)
            : base(nameServers)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public LookupClientOptions(params IPEndPoint[] nameServers)
            : base(nameServers)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public LookupClientOptions(params IPAddress[] nameServers)
            : base(nameServers)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        public LookupClientOptions(IReadOnlyCollection<NameServer> nameServers)
            : base(nameServers)
        {
        }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in cases where the server retruns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets igonred in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/>.
        /// </remarks>
        public TimeSpan? MinimumCacheTimeout
        {
            get { return _minimumCacheTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _minimumCacheTimeout = value;
            }
        }

        /// <summary>
        /// Converts the options into readonly settings.
        /// </summary>
        /// <param name="fromOptions">The options.</param>
        public static implicit operator LookupClientSettings(LookupClientOptions fromOptions)
        {
            if (fromOptions == null)
            {
                return null;
            }

            return new LookupClientSettings(fromOptions);
        }
    }

    // TODO: revisit if we need this AND LookupClientSettings, might not be needed for per query options
    /// <summary>
    /// The readonly version of <see cref="DnsQueryOptions"/> used to customize settings per query.
    /// </summary>
    public class DnsQuerySettings : IEquatable<DnsQuerySettings>
    {
        private NameServer[] _endpoints;

        /// <summary>
        /// Gets a flag indicating whether each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; }

        /// <summary>
        /// Gets a flag indicating whether DNS queries should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// </remarks>
        public bool UseCache { get; }

        /// <summary>
        /// Gets a collection of name servers which should be used to query.
        /// </summary>
        public IReadOnlyCollection<NameServer> NameServers => _endpoints;

        /// <summary>
        /// Gets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; }

        /// <summary>
        /// Gets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>5</c>.
        /// <para>
        /// If all configured <see cref="NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; }

        /// <summary>
        /// Gets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; }

        /// <summary>
        /// Gets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; }

        /// <summary>
        /// Gets a flag indicating whether to query the next configured <see cref="NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; }

        /// <summary>
        /// Gets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets a flag indicating whether Tcp should be used in case a Udp response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no or incomplete answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; }

        /// <summary>
        /// Gets a flag indicating whether Udp should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if Udp cannot be used because of your firewall rules for example.
        /// Also, zone transfers (see <see cref="QueryType.AXFR"/>) must use TCP only.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; }

        /// <summary>
        /// Gets a flag indicating whether the name server collection was manually defined or automatically resolved
        /// </summary>
        public bool AutoResolvedNameServers { get; }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQuerySettings"/>.
        /// </summary>
        public DnsQuerySettings(DnsQueryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _endpoints = options.NameServers.ToArray();

            ContinueOnDnsError = options.ContinueOnDnsError;
            EnableAuditTrail = options.EnableAuditTrail;
            Recursion = options.Recursion;
            Retries = options.Retries;
            ThrowDnsErrors = options.ThrowDnsErrors;
            Timeout = options.Timeout;
            UseCache = options.UseCache;
            UseRandomNameServer = options.UseRandomNameServer;
            UseTcpFallback = options.UseTcpFallback;
            UseTcpOnly = options.UseTcpOnly;
            AutoResolvedNameServers = options.AutoResolvedNameServers;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQuerySettings"/>.
        /// </summary>
        public DnsQuerySettings(DnsQueryOptions options, IReadOnlyCollection<NameServer> overrideServers)
            : this(options)
        {
            _endpoints = overrideServers?.ToArray() ?? throw new ArgumentNullException(nameof(overrideServers));
        }

        internal IReadOnlyCollection<NameServer> ShuffleNameServers()
        {
            if (NameServers.Count > 1)
            {
                var servers = _endpoints.Where(p => p.Enabled).ToArray();

                // if all servers are disabled, retry all of them
                if (servers.Length == 0)
                {
                    return _endpoints;
                }

                // shuffle servers only if we do not have to preserve the order
                if (UseRandomNameServer)
                {
                    var q = new Queue<NameServer>(_endpoints);
                    var server = q.Dequeue();
                    q.Enqueue(server);
                    _endpoints = q.ToArray();
                }

                return servers;
            }
            else
            {
                return NameServers;
            }
        }

        internal DnsQuerySettings WithServers(
            IReadOnlyCollection<NameServer> nameServers)
        {
            return new DnsQuerySettings(new DnsQueryOptions(nameServers)
            {
                ContinueOnDnsError = ContinueOnDnsError,
                EnableAuditTrail = EnableAuditTrail,
                Recursion = Recursion,
                Retries = Retries,
                ThrowDnsErrors = ThrowDnsErrors,
                Timeout = Timeout,
                UseCache = UseCache,
                UseRandomNameServer = UseRandomNameServer,
                UseTcpFallback = UseTcpFallback,
                UseTcpOnly = UseTcpOnly
            });
        }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            return Equals(obj as DnsQuerySettings);
        }

        /// <inheritdocs />
        public bool Equals(DnsQuerySettings other)
        {
            return other != null &&
                   NameServers.SequenceEqual(other.NameServers) &&
                   EnableAuditTrail == other.EnableAuditTrail &&
                   UseCache == other.UseCache &&
                   Recursion == other.Recursion &&
                   Retries == other.Retries &&
                   ThrowDnsErrors == other.ThrowDnsErrors &&
                   UseRandomNameServer == other.UseRandomNameServer &&
                   ContinueOnDnsError == other.ContinueOnDnsError &&
                   Timeout.Equals(other.Timeout) &&
                   UseTcpFallback == other.UseTcpFallback &&
                   UseTcpOnly == other.UseTcpOnly &&
                   AutoResolvedNameServers == other.AutoResolvedNameServers;
        }

        /// <inheritdocs />
        public override int GetHashCode()
        {
            var hashCode = -1775804580;
            hashCode = hashCode * -1521134295 + EqualityComparer<NameServer[]>.Default.GetHashCode(_endpoints);
            hashCode = hashCode * -1521134295 + EnableAuditTrail.GetHashCode();
            hashCode = hashCode * -1521134295 + UseCache.GetHashCode();
            hashCode = hashCode * -1521134295 + Recursion.GetHashCode();
            hashCode = hashCode * -1521134295 + Retries.GetHashCode();
            hashCode = hashCode * -1521134295 + ThrowDnsErrors.GetHashCode();
            hashCode = hashCode * -1521134295 + UseRandomNameServer.GetHashCode();
            hashCode = hashCode * -1521134295 + ContinueOnDnsError.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan>.Default.GetHashCode(Timeout);
            hashCode = hashCode * -1521134295 + UseTcpFallback.GetHashCode();
            hashCode = hashCode * -1521134295 + UseTcpOnly.GetHashCode();
            hashCode = hashCode * -1521134295 + AutoResolvedNameServers.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// The readonly version of <see cref="LookupClientOptions"/> used as default settings in <see cref="LookupClient"/>.
    /// </summary>
    public class LookupClientSettings : DnsQuerySettings, IEquatable<LookupClientSettings>
    {
        /// <summary>
        /// Creates a new instance of <see cref="LookupClientSettings"/>.
        /// </summary>
        public LookupClientSettings(LookupClientOptions options) : base(options)
        {
            MinimumCacheTimeout = options.MinimumCacheTimeout;
        }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in cases where the server retruns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets igonred in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/>.
        /// </remarks>
        public TimeSpan? MinimumCacheTimeout { get; }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            return Equals(obj as LookupClientSettings);
        }

        /// <inheritdocs />
        public bool Equals(LookupClientSettings other)
        {
            return other != null &&
                   base.Equals(other) &&
                   EqualityComparer<TimeSpan?>.Default.Equals(MinimumCacheTimeout, other.MinimumCacheTimeout);
        }

        /// <inheritdocs />
        public override int GetHashCode()
        {
            var hashCode = 1049610412;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan?>.Default.GetHashCode(MinimumCacheTimeout);
            return hashCode;
        }

        internal LookupClientSettings Copy(
            IReadOnlyCollection<NameServer> nameServers,
            TimeSpan? minimumCacheTimeout,
            bool? continueOnDnsError = null,
            bool? enableAuditTrail = null,
            bool? recursion = null,
            int? retries = null,
            bool? throwDnsErrors = null,
            TimeSpan? timeout = null,
            bool? useCache = null,
            bool? useRandomNameServer = null,
            bool? useTcpFallback = null,
            bool? useTcpOnly = null)
        {
            // auto resolved flag might get lost here. But this stuff gets deleted anyways.
            return new LookupClientSettings(new LookupClientOptions(nameServers)
            {
                MinimumCacheTimeout = minimumCacheTimeout,
                ContinueOnDnsError = continueOnDnsError ?? ContinueOnDnsError,
                EnableAuditTrail = enableAuditTrail ?? EnableAuditTrail,
                Recursion = recursion ?? Recursion,
                Retries = retries ?? Retries,
                ThrowDnsErrors = throwDnsErrors ?? ThrowDnsErrors,
                Timeout = timeout ?? Timeout,
                UseCache = useCache ?? UseCache,
                UseRandomNameServer = useRandomNameServer ?? UseRandomNameServer,
                UseTcpFallback = useTcpFallback ?? UseTcpFallback,
                UseTcpOnly = useTcpOnly ?? UseTcpOnly
            });
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithContinueOnDnsError(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, continueOnDnsError: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithEnableAuditTrail(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, enableAuditTrail: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithMinimumCacheTimeout(TimeSpan? value)
        {
            return Copy(NameServers, minimumCacheTimeout: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithRecursion(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, recursion: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithRetries(int value)
        {
            return Copy(NameServers, MinimumCacheTimeout, retries: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithThrowDnsErrors(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, throwDnsErrors: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithTimeout(TimeSpan value)
        {
            return Copy(NameServers, MinimumCacheTimeout, timeout: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithUseCache(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, useCache: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithUseTcpFallback(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, useTcpFallback: value);
        }

        // TODO: remove if LookupClient settings can be made readonly
        internal LookupClientSettings WithUseTcpOnly(bool value)
        {
            return Copy(NameServers, MinimumCacheTimeout, useTcpOnly: value);
        }
    }
}