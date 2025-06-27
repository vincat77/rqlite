using System;
using System.IO;
using System.Text.Json;

public class S3Config
{
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKeyID { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; }
}

public class BackupConfig
{
    public int Version { get; set; }
    public StorageType Type { get; set; }
    public bool NoCompress { get; set; }
    public bool Timestamp { get; set; }
    public bool Vacuum { get; set; }
    public Duration Interval { get; set; }
    public JsonElement Sub { get; set; }

    public static (BackupConfig cfg, S3Config s3) Unmarshal(ReadOnlySpan<byte> data)
    {
        var cfg = JsonSerializer.Deserialize<BackupConfig>(data)!;
        if (cfg.Version > 1) throw new InvalidOperationException("invalid version");
        var s3 = cfg.Sub.Deserialize<S3Config>();
        return (cfg, s3 ?? new S3Config());
    }

    public static byte[] ReadConfigFile(string filename)
    {
        var data = File.ReadAllBytes(filename);
        var expanded = Environment.ExpandEnvironmentVariables(System.Text.Encoding.UTF8.GetString(data));
        return System.Text.Encoding.UTF8.GetBytes(expanded);
    }
}

public class RestoreConfig
{
    public int Version { get; set; }
    public StorageType Type { get; set; }
    public Duration Timeout { get; set; }
    public bool ContinueOnFailure { get; set; }
    public JsonElement Sub { get; set; }

    public static (RestoreConfig cfg, S3Config s3) Unmarshal(ReadOnlySpan<byte> data)
    {
        var cfg = JsonSerializer.Deserialize<RestoreConfig>(data)!;
        if (cfg.Version > 1) throw new InvalidOperationException("invalid version");
        if (cfg.Timeout.Value == TimeSpan.Zero)
            cfg.Timeout = TimeSpan.FromSeconds(30);
        var s3 = cfg.Sub.Deserialize<S3Config>();
        return (cfg, s3 ?? new S3Config());
    }

    public static byte[] ReadConfigFile(string filename)
    {
        var data = File.ReadAllBytes(filename);
        var expanded = Environment.ExpandEnvironmentVariables(System.Text.Encoding.UTF8.GetString(data));
        return System.Text.Encoding.UTF8.GetBytes(expanded);
    }
}
