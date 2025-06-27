using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface ICounter
{
    long Count();
}

public delegate void LoggerFunc(long n);

public class CountingReader : ICounter
{
    private readonly Stream _reader;
    private long _count;

    public CountingReader(Stream reader)
    {
        _reader = reader;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int n = _reader.Read(buffer, offset, count);
        Interlocked.Add(ref _count, n);
        return n;
    }

    public long Count() => Interlocked.Read(ref _count);
}

public class CountingWriter : ICounter
{
    private readonly Stream _writer;
    private long _count;

    public CountingWriter(Stream writer)
    {
        _writer = writer;
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        _writer.Write(buffer, offset, count);
        Interlocked.Add(ref _count, count);
    }

    public long Count() => Interlocked.Read(ref _count);
}

public class CountingMonitor
{
    private const int CountingMonitorIntervalMs = 10_000;

    private readonly LoggerFunc _loggerFn;
    private readonly ICounter _ctr;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runTask;
    private bool _stopped;
    private readonly object _lock = new();

    private CountingMonitor(LoggerFunc loggerFn, ICounter ctr)
    {
        _loggerFn = loggerFn;
        _ctr = ctr;
        _runTask = Task.Run(Run);
    }

    public static CountingMonitor Start(LoggerFunc loggerFn, ICounter ctr)
    {
        return new CountingMonitor(loggerFn, ctr);
    }

    private async Task Run()
    {
        var token = _cts.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(CountingMonitorIntervalMs, token);
                if (!token.IsCancellationRequested)
                {
                    _loggerFn(_ctr.Count());
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void StopAndWait()
    {
        lock (_lock)
        {
            if (_stopped)
                return;
            _stopped = true;
            _cts.Cancel();
        }
        _runTask.Wait();
    }
}
