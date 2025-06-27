using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public static class CertGenerator
{
    public static (byte[] certPem, byte[] keyPem) GenerateCACert(string subject, TimeSpan validFor, int keySize)
    {
        using var rsa = RSA.Create(keySize);
        var req = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.Add(validFor));
        var certPem = PemEncoding.Write("CERTIFICATE", cert.RawData);
        var keyPem = PemEncoding.Write("RSA PRIVATE KEY", rsa.ExportRSAPrivateKey());
        return (certPem, keyPem);
    }

    public static (byte[] certPem, byte[] keyPem) GenerateSelfSignedCert(string subject, TimeSpan validFor, int keySize)
    {
        using var rsa = RSA.Create(keySize);
        var req = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.Add(validFor));
        var certPem = PemEncoding.Write("CERTIFICATE", cert.RawData);
        var keyPem = PemEncoding.Write("RSA PRIVATE KEY", rsa.ExportRSAPrivateKey());
        return (certPem, keyPem);
    }
}
