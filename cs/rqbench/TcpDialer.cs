using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

public class TcpDialer
{
    private readonly byte _header;
    private readonly SslClientAuthenticationOptions? _tlsOptions;

    public TcpDialer(byte header, SslClientAuthenticationOptions? tlsOptions)
    {
        _header = header;
        _tlsOptions = tlsOptions;
    }

    public async Task<Stream> DialAsync(string addr, TimeSpan timeout)
    {
        var parts = addr.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("addr must be host:port");
        string host = parts[0];
        int port = int.Parse(parts[1]);

        using var cts = new CancellationTokenSource(timeout);
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cts.Token);
            Stream stream = client.GetStream();
            if (_tlsOptions != null)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: false,
                    _tlsOptions.RemoteCertificateValidationCallback);
                await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    ClientCertificates = _tlsOptions.ClientCertificates,
                    EnabledSslProtocols = _tlsOptions.EnabledSslProtocols,
                    CertificateRevocationCheckMode = _tlsOptions.CertificateRevocationCheckMode
                }, cts.Token);
                stream = ssl;
            }
            await stream.WriteAsync(new[] { _header }, 0, 1, cts.Token);
            await stream.FlushAsync(cts.Token);
            return stream;
        }
        catch
        {
            client.Close();
            throw;
        }
    }
}
