using System;
using System.IO;
using System.IO.Compression;

public static class GzipUtil
{
    public static string Gzip(string file)
    {
        using var input = File.OpenRead(file);
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var output = File.Create(tmp);
        using var gz = new GZipStream(output, CompressionLevel.Optimal);
        input.CopyTo(gz);
        return tmp;
    }

    public static string Gunzip(string file)
    {
        using var input = File.OpenRead(file);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var output = File.Create(tmp);
        gz.CopyTo(output);
        return tmp;
    }
}
