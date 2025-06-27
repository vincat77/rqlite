using System;
using System.IO;
using System.IO.Compression;

public static class ZipUtil
{
    public static bool IsZipFile(string path)
    {
        if (!File.Exists(path)) return false;
        byte[] magic = new byte[4];
        using var f = File.OpenRead(path);
        if (f.Read(magic, 0, 4) != 4) return false;
        return magic[0] == (byte)'P' && magic[1] == (byte)'K' && magic[2] == 3 && magic[3] == 4;
    }

    public static bool ZipHasSubdirectories(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                entry.FullName != Path.GetFileName(entry.FullName))
            {
                return true;
            }
        }
        return false;
    }

    public static void UnzipToDir(string path, string dir)
    {
        using var archive = ZipFile.OpenRead(path);
        Directory.CreateDirectory(dir);
        foreach (var entry in archive.Entries)
        {
            var destPath = Path.Combine(dir, entry.FullName);
            if (!destPath.StartsWith(Path.GetFullPath(dir) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("invalid file path");
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                entry.ExtractToFile(destPath, overwrite: true);
            }
        }
    }
}
