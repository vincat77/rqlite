using System;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;

public static class TarGzipUtil
{
    public static bool IsTarGzipFile(string path)
    {
        try
        {
            using var f = File.OpenRead(path);
            using var gz = new GZipStream(f, CompressionMode.Decompress);
            // attempt to read first tar entry
            using var reader = new TarReader(gz);
            return reader.GetNextEntry() != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool TarGzipHasSubdirectories(string path)
    {
        using var f = File.OpenRead(path);
        using var gz = new GZipStream(f, CompressionMode.Decompress);
        using var reader = new TarReader(gz);
        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) != null)
        {
            if (entry.EntryType == TarEntryType.Directory ||
                entry.Name.Contains('/') || entry.Name.Contains('\\'))
                return true;
        }
        return false;
    }

    public static void UntarGzipToDir(string path, string dir)
    {
        using var f = File.OpenRead(path);
        using var gz = new GZipStream(f, CompressionMode.Decompress);
        using var reader = new TarReader(gz);
        Directory.CreateDirectory(dir);
        TarEntry? entry;
        var basePath = Path.GetFullPath(dir) + Path.DirectorySeparatorChar;
        while ((entry = reader.GetNextEntry()) != null)
        {
            var dest = Path.Combine(dir, entry.Name);
            dest = Path.GetFullPath(dest);
            if (!dest.StartsWith(basePath, StringComparison.Ordinal))
                throw new InvalidOperationException("invalid file path");
            if (entry.EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(dest);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                using var outFile = File.Create(dest);
                entry.DataStream.CopyTo(outFile);
            }
        }
    }
}
