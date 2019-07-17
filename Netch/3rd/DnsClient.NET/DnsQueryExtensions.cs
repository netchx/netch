using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsClient
{
    /// <summary>
    /// Extension methods for the <see cref="IDnsQuery"/> contract.
    /// <para>
    /// The methods implement common queries which are more complex and have some business logic.
    /// </para>
    /// </summary>
    public static class DnsQueryExtensions
    {
        /// <summary>
        /// The <c>GetHostEntry</c> method queries a DNS server for the IP addresses and aliases associated with the <paramref name="hostNameOrAddress"/>.
        /// In case <paramref name="hostNameOrAddress"/> is an <see cref="IPAddress"/>, <c>GetHostEntry</c> does a reverse lookup on that first to determine the hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <see cref="ResourceRecordType.CNAME"/> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname or <paramref name="hostNameOrAddress"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following code example uses the <see cref="GetHostEntry(IDnsQuery, string)"/> method to resolve an IP address or hostname to an <see cref="IPHostEntry"/> instance.
        /// <code>
        /// <![CDATA[
        /// public static void PrintHostEntry(string hostOrIp)
        /// {
        ///     var lookup = new LookupClient();
        ///     IPHostEntry hostEntry = lookup.GetHostEntry(hostOrIp);
        ///     Console.WriteLine(hostEntry.HostName);
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// The method has some logic to populate the <see cref="IPHostEntry.Aliases"/> list:
        /// <list type="bullet">
        /// <item>
        /// <term>
        /// In case of sub-domain queries or similar, there might be multiple <see cref="ResourceRecordType.CNAME"/> records for one <see cref="IPAddress"/>,
        /// </term>
        /// </item><item>
        /// <term>
        /// If only one <see cref="IPAddress"/> is in the result set, all the aliases found will be returned.
        /// </term>
        /// </item><item>
        /// <term>
        /// If more than one <see cref="IPAddress"/> is in the result set, aliases are returned only if at least one doesn't match the queried hostname.
        /// </term>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="hostNameOrAddress">The <see cref="IPAddress"/> or host name to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="hostNameOrAddress"/>.
        /// In case the <paramref name="hostNameOrAddress"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="hostNameOrAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static IPHostEntry GetHostEntry(this IDnsQuery query, string hostNameOrAddress)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
            {
                return query.GetHostEntry(address);
            }

            return GetHostEntryFromName(query, hostNameOrAddress);
        }

        /// <summary>
        /// The <c>GetHostEntryAsync</c> method queries a DNS server for the IP addresses and aliases associated with the <paramref name="hostNameOrAddress"/>.
        /// In case <paramref name="hostNameOrAddress"/> is an <see cref="IPAddress"/>, <c>GetHostEntry</c> does a reverse lookup on that first to determine the hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <see cref="ResourceRecordType.CNAME"/> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname or <paramref name="hostNameOrAddress"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following code example uses the <see cref="GetHostEntryAsync(IDnsQuery, string)"/> method to resolve an IP address or hostname to an <see cref="IPHostEntry"/> instance.
        /// <code>
        /// <![CDATA[
        /// public static async Task PrintHostEntry(string hostOrIp)
        /// {
        ///     var lookup = new LookupClient();
        ///     IPHostEntry hostEntry = await lookup.GetHostEntryAsync(hostOrIp);
        ///     Console.WriteLine(hostEntry.HostName);
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// The method has some logic to populate the <see cref="IPHostEntry.Aliases"/> list:
        /// <list type="bullet">
        /// <item>
        /// <term>
        /// In case of sub-domain queries or similar, there might be multiple <see cref="ResourceRecordType.CNAME"/> records for one <see cref="IPAddress"/>,
        /// </term>
        /// </item><item>
        /// <term>
        /// If only one <see cref="IPAddress"/> is in the result set, all the aliases found will be returned.
        /// </term>
        /// </item><item>
        /// <term>
        /// If more than one <see cref="IPAddress"/> is in the result set, aliases are returned only if at least one doesn't match the queried hostname.
        /// </term>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="hostNameOrAddress">The <see cref="IPAddress"/> or host name to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="hostNameOrAddress"/>.
        /// In case the <paramref name="hostNameOrAddress"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="hostNameOrAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static Task<IPHostEntry> GetHostEntryAsync(this IDnsQuery query, string hostNameOrAddress)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
            {
                return query.GetHostEntryAsync(address);
            }

            return GetHostEntryFromNameAsync(query, hostNameOrAddress);
        }

        /// <summary>
        /// The <c>GetHostEntry</c> method does a reverse lookup on the IP <paramref name="address"/>,
        /// and queries a DNS server for the IP addresses and aliases associated with the resolved hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <see cref="ResourceRecordType.CNAME"/> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname of the <paramref name="address"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following code example uses the <see cref="GetHostEntry(IDnsQuery, IPAddress)"/> method to resolve an IP address to an <see cref="IPHostEntry"/> instance.
        /// <code>
        /// <![CDATA[
        /// public static void PrintHostEntry(IPAddress address)
        /// {
        ///     var lookup = new LookupClient();
        ///     IPHostEntry hostEntry = lookup.GetHostEntry(address);
        ///     Console.WriteLine(hostEntry.HostName);
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// The method has some logic to populate the <see cref="IPHostEntry.Aliases"/> list:
        /// <list type="bullet">
        /// <item>
        /// <term>
        /// In case of sub-domain queries or similar, there might be multiple <see cref="ResourceRecordType.CNAME"/> records for one <see cref="IPAddress"/>,
        /// </term>
        /// </item><item>
        /// <term>
        /// If only one <see cref="IPAddress"/> is in the result set, all the aliases found will be returned.
        /// </term>
        /// </item><item>
        /// <term>
        /// If more than one <see cref="IPAddress"/> is in the result set, aliases are returned only if at least one doesn't match the queried hostname.
        /// </term>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="address"/>.
        /// In case the <paramref name="address"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/> is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static IPHostEntry GetHostEntry(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var hostName = query.GetHostName(address);
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            return GetHostEntryFromName(query, hostName);
        }

        /// <summary>
        /// The <c>GetHostEntryAsync</c> method does a reverse lookup on the IP <paramref name="address"/>,
        /// and queries a DNS server for the IP addresses and aliases associated with the resolved hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <see cref="ResourceRecordType.CNAME"/> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname of the <paramref name="address"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following code example uses the <see cref="GetHostEntryAsync(IDnsQuery, IPAddress)"/> method to resolve an IP address to an <see cref="IPHostEntry"/> instance.
        /// <code>
        /// <![CDATA[
        /// public static async Task PrintHostEntry(IPAddress address)
        /// {
        ///     var lookup = new LookupClient();
        ///     IPHostEntry hostEntry = await lookup.GetHostEntryAsync(address);
        ///     Console.WriteLine(hostEntry.HostName);
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// The method has some logic to populate the <see cref="IPHostEntry.Aliases"/> list:
        /// <list type="bullet">
        /// <item>
        /// <term>
        /// In case of sub-domain queries or similar, there might be multiple <see cref="ResourceRecordType.CNAME"/> records for one <see cref="IPAddress"/>,
        /// </term>
        /// </item><item>
        /// <term>
        /// If only one <see cref="IPAddress"/> is in the result set, all the aliases found will be returned.
        /// </term>
        /// </item><item>
        /// <term>
        /// If more than one <see cref="IPAddress"/> is in the result set, aliases are returned only if at least one doesn't match the queried hostname.
        /// </term>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="address"/>.
        /// In case the <paramref name="address"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/> is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static async Task<IPHostEntry> GetHostEntryAsync(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var hostName = await query.GetHostNameAsync(address).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            return await GetHostEntryFromNameAsync(query, hostName).ConfigureAwait(false);
        }

        private static IPHostEntry GetHostEntryFromName(IDnsQuery query, string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            var hostString = DnsString.FromResponseQueryString(hostName);
            var ipv4Result = query.Query(hostString, QueryType.A);
            var ipv6Result = query.Query(hostString, QueryType.AAAA);

            var allRecords = ipv4Result
                .Answers.Concat(ipv6Result.Answers)
                .ToArray();

            return GetHostEntryProcessResult(hostString, allRecords);
        }

        private static async Task<IPHostEntry> GetHostEntryFromNameAsync(IDnsQuery query, string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            var hostString = DnsString.FromResponseQueryString(hostName);
            var ipv4Result = query.QueryAsync(hostString, QueryType.A);
            var ipv6Result = query.QueryAsync(hostString, QueryType.AAAA);

            await Task.WhenAll(ipv4Result, ipv6Result).ConfigureAwait(false);

            var allRecords = ipv4Result.Result
                .Answers.Concat(ipv6Result.Result.Answers)
                .ToArray();

            return GetHostEntryProcessResult(hostString, allRecords);
        }

        private static IPHostEntry GetHostEntryProcessResult(DnsString hostString, DnsResourceRecord[] allRecords)
        {
            var addressRecords = allRecords
                            .OfType<AddressRecord>()
                            .Select(p => new
                            {
                                Address = p.Address,
                                Alias = DnsString.FromResponseQueryString(p.DomainName)
                            })
                            .ToArray();

            var hostEntry = new IPHostEntry()
            {
                Aliases = new string[0],
                AddressList = addressRecords
                    .Select(p => p.Address)
                    .ToArray()
            };

            if (addressRecords.Length > 1)
            {
                if (addressRecords.Any(p => !p.Alias.Equals(hostString)))
                {
                    hostEntry.Aliases = addressRecords
                        .Select(p => p.Alias.ToString())
                        .Select(p => p.Substring(0, p.Length - 1))
                        .Distinct()
                        .ToArray();
                }
            }
            else if (addressRecords.Length == 1)
            {
                if (allRecords.Any(p => !DnsString.FromResponseQueryString(p.DomainName).Equals(hostString)))
                {
                    hostEntry.Aliases = allRecords
                        .Select(p => p.DomainName.ToString())
                        .Select(p => p.Substring(0, p.Length - 1))
                        .Distinct()
                        .ToArray();
                }
            }

            hostEntry.HostName = hostString.Value.Substring(0, hostString.Value.Length - 1);

            return hostEntry;
        }

        /// <summary>
        /// The <c>GetHostName</c> method queries a DNS server to resolve the hostname of the <paramref name="address"/> via reverse lookup.
        /// </summary>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to resolve.</param>
        /// <returns>
        /// The hostname if the reverse lookup was successful or <c>null</c>, in case the host was not found.
        /// If <see cref="ILookupClient.ThrowDnsErrors"/> is set to <c>true</c>, this method will throw an <see cref="DnsResponseException"/> instead of returning <c>null</c>!
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>is null.</exception>
        /// <exception cref="DnsResponseException">If no host has been found and <see cref="ILookupClient.ThrowDnsErrors"/> is <c>true</c>.</exception>
        public static string GetHostName(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var result = query.QueryReverse(address);
            return GetHostNameAsyncProcessResult(result);
        }

        /// <summary>
        /// The <c>GetHostNameAsync</c> method queries a DNS server to resolve the hostname of the <paramref name="address"/> via reverse lookup.
        /// </summary>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to resolve.</param>
        /// <returns>
        /// The hostname if the reverse lookup was successful or <c>null</c>, in case the host was not found.
        /// If <see cref="ILookupClient.ThrowDnsErrors"/> is set to <c>true</c>, this method will throw an <see cref="DnsResponseException"/> instead of returning <c>null</c>!
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>is null.</exception>
        /// <exception cref="DnsResponseException">If no host has been found and <see cref="ILookupClient.ThrowDnsErrors"/> is <c>true</c>.</exception>
        public static async Task<string> GetHostNameAsync(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var result = await query.QueryReverseAsync(address).ConfigureAwait(false);
            return GetHostNameAsyncProcessResult(result);
        }

        private static string GetHostNameAsyncProcessResult(IDnsQueryResponse result)
        {
            if (result.HasError)
            {
                return null;
            }

            var hostName = result.Answers.PtrRecords().FirstOrDefault()?.PtrDomainName;
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            // removing the . at the end
            return hostName.Value.Substring(0, hostName.Value.Length - 1);
        }

        /// <summary>
        /// The <c>ResolveService</c> method does a <see cref="QueryType.SRV"/> lookup for <c>_{<paramref name="serviceName"/>}[._{<paramref name="protocol"/>}].{<paramref name="baseDomain"/>}</c>
        /// and aggregates the result (hostname, port and list of <see cref="IPAddress"/>s) to a <see cref="ServiceHostEntry"/>.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <see cref="ResourceRecordType.CNAME"/> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The returned list of <see cref="IPAddress"/>s and/or the hostname can be empty if no matching additional records are found.
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="baseDomain">The base domain, which will be appended to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for. Must not have any <c>_</c> prefix.</param>
        /// <param name="protocol">
        /// The protocol of the service to query for.
        /// Set it to <see cref="ProtocolType.Unknown"/> or <see cref="ProtocolType.Unspecified"/> to not pass any protocol.
        /// </param>
        /// <returns>A collection of <see cref="ServiceHostEntry"/>s.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="baseDomain"/> or <paramref name="serviceName"/> are null.</exception>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        public static ServiceHostEntry[] ResolveService(this IDnsQuery query, string baseDomain, string serviceName, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Unspecified || protocol == ProtocolType.Unknown)
            {
                return ResolveService(query, baseDomain, serviceName, null);
            }

            return ResolveService(query, baseDomain, serviceName, protocol.ToString());
        }

        /// <summary>
        /// The <c>ResolveServiceAsync</c> method does  a <see cref="QueryType.SRV"/> lookup for <c>_{<paramref name="serviceName"/>}[._{<paramref name="protocol"/>}].{<paramref name="baseDomain"/>}</c>
        /// and aggregates the result (hostname, port and list of <see cref="IPAddress"/>s) to a <see cref="ServiceHostEntry"/>.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <see cref="ResourceRecordType.CNAME"/> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The returned list of <see cref="IPAddress"/>s and/or the hostname can be empty if no matching additional records are found.
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="baseDomain">The base domain, which will be appended to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for. Must not have any <c>_</c> prefix.</param>
        /// <param name="protocol">
        /// The protocol of the service to query for.
        /// Set it to <see cref="ProtocolType.Unknown"/> or <see cref="ProtocolType.Unspecified"/> to not pass any protocol.
        /// </param>
        /// <returns>A collection of <see cref="ServiceHostEntry"/>s.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="baseDomain"/> or <paramref name="serviceName"/> are null.</exception>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        public static Task<ServiceHostEntry[]> ResolveServiceAsync(this IDnsQuery query, string baseDomain, string serviceName, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Unspecified || protocol == ProtocolType.Unknown)
            {
                return ResolveServiceAsync(query, baseDomain, serviceName, null);
            }

            return ResolveServiceAsync(query, baseDomain, serviceName, protocol.ToString());
        }

        /// <summary>
        /// The <c>ResolveService</c> method does a <see cref="QueryType.SRV"/> lookup for <c>_{<paramref name="serviceName"/>}[._{<paramref name="tag"/>}].{<paramref name="baseDomain"/>}</c>
        /// and aggregates the result (hostname, port and list of <see cref="IPAddress"/>s) to a <see cref="ServiceHostEntry"/>.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <see cref="ResourceRecordType.CNAME"/> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The returned list of <see cref="IPAddress"/>s and/or the hostname can be empty if no matching additional records are found.
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="baseDomain">The base domain, which will be appended to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for. Must not have any <c>_</c> prefix.</param>
        /// <param name="tag">An optional tag. Must not have any <c>_</c> prefix.</param>
        /// <returns>A collection of <see cref="ServiceHostEntry"/>s.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="baseDomain"/> or <paramref name="serviceName"/> are null.</exception>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        public static ServiceHostEntry[] ResolveService(this IDnsQuery query, string baseDomain, string serviceName, string tag = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (baseDomain == null)
            {
                throw new ArgumentNullException(nameof(baseDomain));
            }
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            var queryString = ConcatResolveServiceName(baseDomain, serviceName, tag);

            var result = query.Query(queryString, QueryType.SRV);

            return ResolveServiceProcessResult(result);
        }

        /// <summary>
        /// The <c>ResolveServiceAsync</c> method does a <see cref="QueryType.SRV"/> lookup for <c>_{<paramref name="serviceName"/>}[._{<paramref name="tag"/>}].{<paramref name="baseDomain"/>}</c>
        /// and aggregates the result (hostname, port and list of <see cref="IPAddress"/>s) to a <see cref="ServiceHostEntry"/>.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <see cref="ResourceRecordType.CNAME"/> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The returned list of <see cref="IPAddress"/>s and/or the hostname can be empty if no matching additional records are found.
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="baseDomain">The base domain, which will be appended to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for. Must not have any <c>_</c> prefix.</param>
        /// <param name="tag">An optional tag. Must not have any <c>_</c> prefix.</param>
        /// <returns>A collection of <see cref="ServiceHostEntry"/>s.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="baseDomain"/> or <paramref name="serviceName"/> are null.</exception>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        public static async Task<ServiceHostEntry[]> ResolveServiceAsync(this IDnsQuery query, string baseDomain, string serviceName, string tag = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (baseDomain == null)
            {
                throw new ArgumentNullException(nameof(baseDomain));
            }
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            var queryString = ConcatResolveServiceName(baseDomain, serviceName, tag);

            var result = await query.QueryAsync(queryString, QueryType.SRV).ConfigureAwait(false);

            return ResolveServiceProcessResult(result);
        }

        private static string ConcatResolveServiceName(string baseDomain, string serviceName, string tag)
        {
            return string.IsNullOrWhiteSpace(tag) ?
                $"{serviceName}.{baseDomain}." :
                $"_{serviceName}._{tag}.{baseDomain}.";
        }

        private static ServiceHostEntry[] ResolveServiceProcessResult(IDnsQueryResponse result)
        {
            var hosts = new List<ServiceHostEntry>();
            if (result.HasError)
            {
                return hosts.ToArray();
            }

            foreach (var entry in result.Answers.SrvRecords())
            {
                var addresses = result.Additionals
                    .OfType<AddressRecord>()
                    .Where(p => p.DomainName.Equals(entry.Target))
                    .Select(p => p.Address);

                var hostName = result.Additionals
                    .OfType<CNameRecord>()
                    .Where(p => p.DomainName.Equals(entry.Target))
                    .Select(p => p.CanonicalName).FirstOrDefault();

                hosts.Add(new ServiceHostEntry()
                {
                    AddressList = addresses.ToArray(),
                    HostName = hostName,
                    Port = entry.Port
                });
            }

            return hosts.ToArray();
        }
    }

    /// <summary>
    /// Extends <see cref="IPHostEntry"/> by the <see cref="ServiceHostEntry.Port"/> property.
    /// </summary>
    /// <seealso cref="System.Net.IPHostEntry" />
    public class ServiceHostEntry : IPHostEntry
    {
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port { get; set; }
    }
}