using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

public class CertReloader
{
    private readonly string _certPath;
    private readonly string _keyPath;
    private DateTime _modTime;
    private X509Certificate2 _cert;
    private readonly ReaderWriterLockSlim _lock = new();

    public CertReloader(string certPath, string keyPath)
    {
        _certPath = certPath;
        _keyPath = keyPath;
        _cert = LoadKeyPair(certPath, keyPath);
        _modTime = LatestModTime(certPath, keyPath);
    }

    public X509Certificate2 GetCertificate()
    {
        _lock.EnterReadLock();
        try
        {
            var latest = LatestModTime(_certPath, _keyPath);
            if (latest <= _modTime)
                return _cert;
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _lock.EnterWriteLock();
        try
        {
            var latest = LatestModTime(_certPath, _keyPath);
            if (latest > _modTime)
            {
                _cert = LoadKeyPair(_certPath, _keyPath);
                _modTime = latest;
            }
            return _cert;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static X509Certificate2 LoadKeyPair(string cert, string key)
    {
        return X509Certificate2.CreateFromPemFile(cert, key);
    }

    private static DateTime LatestModTime(params string[] files)
    {
        DateTime latest = DateTime.MinValue;
        foreach (var f in files)
        {
            var t = File.GetLastWriteTimeUtc(f);
            if (t > latest) latest = t;
        }
        return latest;
    }
}
