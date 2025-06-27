using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

public interface IDownloadClient
{
    Task DownloadAsync(Stream writer);
    string ToString();
}

public class RestoreDownloader
{
    private readonly IDownloadClient _client;
    private readonly Action<string> _logger;

    public RestoreDownloader(IDownloadClient client, Action<string>? logger = null)
    {
        _client = client;
        _logger = logger ?? Console.WriteLine;
    }

    public async Task DoAsync(Stream dest, TimeSpan timeout, CancellationToken token)
    {
        var tmp = Path.GetTempFileName();
        await using var f = File.Create(tmp);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);
            await _client.DownloadAsync(f);
            f.Seek(0, SeekOrigin.Begin);
            var cw = new CountingWriterAt(dest);
            if (await IsGzipAsync(f))
            {
                using var gzr = new GZipStream(f, CompressionMode.Decompress, true);
                await gzr.CopyToAsync(cw);
            }
            else
            {
                await f.CopyToAsync(cw);
            }
        }
        finally
        {
            f.Close();
            File.Delete(tmp);
        }
    }

    private static async Task<bool> IsGzipAsync(FileStream f)
    {
        f.Seek(0, SeekOrigin.Begin);
        var buf = new byte[3];
        var n = await f.ReadAsync(buf, 0, buf.Length);
        f.Seek(0, SeekOrigin.Begin);
        return n == 3 && buf[0] == 0x1f && buf[1] == 0x8b && buf[2] == 0x08;
    }

    private class CountingWriterAt : Stream
    {
        private readonly Stream _inner;
        public long Count { get; private set; }
        public CountingWriterAt(Stream inner) { _inner = inner; }
        public override bool CanRead => false;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            Count += count;
        }
    }
}
