using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IStorageClient
{
    Task UploadAsync(Stream reader, string id);
    Task<string> CurrentIDAsync();
    string ToString();
}

public interface IDataProvider
{
    Task<ulong> LastIndexAsync();
    Task ProvideAsync(Stream writer);
}

public class BackupUploader
{
    private readonly IStorageClient _storageClient;
    private readonly IDataProvider _dataProvider;
    private readonly TimeSpan _interval;
    private readonly Action<string> _logger;

    private DateTime _lastUploadTime;
    private TimeSpan _lastUploadDuration;
    private ulong _lastIndex;

    public BackupUploader(IStorageClient storageClient, IDataProvider dataProvider, TimeSpan interval, Action<string>? logger = null)
    {
        _storageClient = storageClient;
        _dataProvider = dataProvider;
        _interval = interval;
        _logger = logger ?? Console.WriteLine;
    }

    public async Task StartAsync(CancellationToken token, Func<bool>? isUploadEnabled = null)
    {
        isUploadEnabled ??= (() => true);
        _logger($"starting upload to {_storageClient} every {_interval}");
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(_interval, token).ContinueWith(_ => { });
            if (token.IsCancellationRequested) break;
            if (!isUploadEnabled()) continue;
            try { await UploadOnce(token); } catch (Exception ex) { _logger($"failed to upload to {_storageClient}: {ex}"); }
        }
    }

    public Dictionary<string, object> Stats()
    {
        return new()
        {
            ["upload_destination"] = _storageClient.ToString(),
            ["upload_interval"] = _interval.ToString(),
            ["last_upload_time"] = _lastUploadTime.ToString("o"),
            ["last_upload_duration"] = _lastUploadDuration.ToString(),
            ["last_index"] = _lastIndex.ToString()
        };
    }

    private async Task UploadOnce(CancellationToken token)
    {
        ulong li = await _dataProvider.LastIndexAsync();
        if (li <= _lastIndex)
            return;

        var path = Path.GetTempFileName();
        await using var fd = File.Create(path);
        try
        {
            await _dataProvider.ProvideAsync(fd);
            if (_lastIndex == 0)
            {
                try
                {
                    var cloudID = await _storageClient.CurrentIDAsync();
                    if (cloudID == li.ToString())
                        return;
                }
                catch (Exception ex)
                {
                    _logger($"failed to get current sum from {_storageClient}: {ex}");
                }
            }

            fd.Seek(0, SeekOrigin.Begin);
            var cr = new CountingReader(fd);
            var sw = Stopwatch.StartNew();
            await _storageClient.UploadAsync(new StreamWrapper(cr), li.ToString());
            _lastIndex = li;
            _lastUploadTime = DateTime.UtcNow;
            _lastUploadDuration = sw.Elapsed;
            _logger($"completed auto upload of {Humanize.Bytes((ulong)cr.Count())} to {_storageClient} in {_lastUploadDuration}");
        }
        finally
        {
            fd.Close();
            File.Delete(path);
        }
    }

    private class StreamWrapper : Stream
    {
        private readonly CountingReader _cr;
        public StreamWrapper(CountingReader cr) { _cr = cr; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _cr.Count();
        public override long Position { get => _cr.Count(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _cr.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
