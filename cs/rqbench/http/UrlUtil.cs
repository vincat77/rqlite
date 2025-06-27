using System;
using System.Net;
using System.Text;

namespace rqbench.Http
{
    public static class UrlUtil
    {
        public static string NormalizeAddr(string addr)
        {
            if (!addr.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !addr.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return $"http://{addr}";
            }
            return addr;
        }

        public static string EnsureHTTPS(string addr)
        {
            if (!addr.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !addr.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://{addr}";
            }
            if (addr.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return "https://" + addr.Substring("http://".Length);
            return addr;
        }

        public static bool CheckHTTPS(string addr)
        {
            return addr.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static string AddBasicAuth(string joinAddr, string username, string password, out Exception? error)
        {
            error = null;
            if (string.IsNullOrEmpty(username))
                return joinAddr;
            if (!Uri.TryCreate(joinAddr, UriKind.Absolute, out var u))
            {
                error = new UriFormatException("invalid address");
                return string.Empty;
            }
            if (!string.IsNullOrEmpty(u.UserInfo))
            {
                error = new InvalidOperationException("userinfo exists");
                return string.Empty;
            }
            var builder = new UriBuilder(u)
            {
                UserName = username,
                Password = password
            };
            return builder.Uri.ToString();
        }

        public static string RemoveBasicAuth(string u)
        {
            if (!Uri.TryCreate(u, UriKind.Absolute, out var uri))
                return u;
            var builder = new UriBuilder(uri)
            {
                UserName = string.Empty,
                Password = string.Empty
            };
            return builder.Uri.ToString();
        }
    }
}
