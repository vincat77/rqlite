using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ExtensionStore
{
    private readonly string _dir;

    public ExtensionStore(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        _dir = dir;
    }

    public string Dir => _dir;

    public IEnumerable<string> List()
    {
        return Directory.GetFiles(_dir).Where(f => !Path.GetFileName(f).StartsWith("."));
    }

    public IEnumerable<string> Names() => List().Select(Path.GetFileName).OrderBy(n => n);

    public void LoadFromFile(string file)
    {
        var dst = Path.Combine(_dir, Path.GetFileName(file));
        File.Copy(file, dst, true);
    }

    public void LoadFromDir(string dir)
    {
        foreach (var src in Directory.GetFiles(dir).Where(f => !Path.GetFileName(f).StartsWith(".")))
        {
            var dst = Path.Combine(_dir, Path.GetFileName(src));
            File.Copy(src, dst, true);
        }
    }

    public void LoadFromZip(string zipfile)
    {
        if (ZipUtil.ZipHasSubdirectories(zipfile))
            throw new InvalidOperationException("zip file contains subdirectories");
        ZipUtil.UnzipToDir(zipfile, _dir);
    }

    public void LoadFromTarGzip(string targzfile)
    {
        if (TarGzipUtil.TarGzipHasSubdirectories(targzfile))
            throw new InvalidOperationException("tar.gz file contains subdirectories");
        TarGzipUtil.UntarGzipToDir(targzfile, _dir);
    }

    public Dictionary<string, object> Stats()
    {
        return new Dictionary<string, object>
        {
            ["dir"] = _dir,
            ["names"] = Names().ToArray()
        };
    }
}
