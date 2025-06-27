using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace rqbench.Http
{
    public static class Preflight
    {
        private const string TestPath = "/status";
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public static (string? addr, bool ok) AnyServingHTTP(string[] addrs)
        {
            foreach (var a in addrs)
            {
                if (IsServingHTTP(a))
                    return (a, true);
            }
            return (null, false);
        }

        public static bool IsServingHTTP(string addr)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            using var client = new HttpClient(handler) { Timeout = Timeout };

            foreach (var u in new[] { $"http://{addr}{TestPath}", $"https://{addr}{TestPath}" })
            {
                try
                {
                    using var resp = client.GetAsync(u).Result;
                    resp.Dispose();
                    return true;
                }
                catch
                {
                    // ignore
                }
            }
            return false;
        }
    }
}
