using System;
using System.IO;
using System.Security.Cryptography;

public static class MD5Util
{
    public static string MD5(string path)
    {
        using var stream = File.OpenRead(path);
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
