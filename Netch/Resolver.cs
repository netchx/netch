using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Netch
{
    public class Resolver : DNS.Client.RequestResolver.IRequestResolver
    {
        public Task<DNS.Protocol.IResponse> Resolve(DNS.Protocol.IRequest request, CancellationToken cancellationToken = default)
        {
            DNS.Protocol.IResponse response = DNS.Protocol.Response.FromRequest(request);

            foreach (var question in response.Questions)
            {
                if (question.Type == DNS.Protocol.RecordType.A)
                {
                    var client = new DnsClient.LookupClient(DnsClient.NameServer.GooglePublicDns);
                    client.UseTcpOnly = true;
                    client.UseCache = true;

                    try
                    {
                        var result = client.Query(question.Name.ToString(), DnsClient.QueryType.A);
                        foreach (var item in result.Answers.ARecords())
                        {
                            response.AnswerRecords.Add(new DNS.Protocol.ResourceRecords.IPAddressResourceRecord(question.Name, item.Address));
                        }
                    }
                    catch (Exception)
                    {
                        // 跳过
                    }
                }
                else if (question.Type == DNS.Protocol.RecordType.AAAA)
                {
                    var client = new DnsClient.LookupClient(DnsClient.NameServer.GooglePublicDns);
                    client.UseTcpOnly = true;
                    client.UseCache = true;

                    try
                    {
                        var result = client.Query(question.Name.ToString(), DnsClient.QueryType.AAAA);
                        foreach (var item in result.Answers.AaaaRecords())
                        {
                            response.AnswerRecords.Add(new DNS.Protocol.ResourceRecords.IPAddressResourceRecord(question.Name, item.Address));
                        }
                    }
                    catch (Exception)
                    {
                        // 跳过
                    }
                }
                else
                {
                    // 跳过
                }
            }

            return Task.FromResult(response);
        }
    }
}
