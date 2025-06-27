using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

public enum MTLSState
{
    Disabled,
    Enabled
}

public static class RtlsConfig
{
    public static SslClientAuthenticationOptions CreateClientConfig(string certFile, string keyFile, string caCertFile, string serverName, bool noverify)
    {
        var opts = new SslClientAuthenticationOptions
        {
            TargetHost = serverName,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            RemoteCertificateValidationCallback = noverify ? (_,_,_,_) => true : null
        };
        if (!string.IsNullOrEmpty(certFile) && !string.IsNullOrEmpty(keyFile))
        {
            opts.ClientCertificates = new X509CertificateCollection
            {
                X509Certificate2.CreateFromPemFile(certFile, keyFile)
            };
        }
        if (!string.IsNullOrEmpty(caCertFile))
        {
            var ca = new X509Certificate2(File.ReadAllBytes(caCertFile));
            opts.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(ca);
                return chain.Build((X509Certificate2)cert!);
            };
        }
        return opts;
    }

    public static SslServerAuthenticationOptions CreateServerConfig(string certFile, string keyFile, string caCertFile, MTLSState mtls)
    {
        var cert = X509Certificate2.CreateFromPemFile(certFile, keyFile);
        var opts = new SslServerAuthenticationOptions
        {
            ServerCertificate = cert,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };
        if (!string.IsNullOrEmpty(caCertFile))
        {
            var ca = new X509Certificate2(File.ReadAllBytes(caCertFile));
            opts.ClientCertificateRequired = mtls == MTLSState.Enabled;
            opts.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(ca);
                return chain.Build((X509Certificate2)certificate!);
            };
        }
        else if (mtls == MTLSState.Enabled)
        {
            opts.ClientCertificateRequired = true;
        }
        return opts;
    }
}
