using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public sealed class FlushChannel
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    public Task Task => _tcs.Task;
    public void Close() => _tcs.TrySetResult(true);
}

public class Request<T>
{
    public long SequenceNumber { get; set; }
    public List<T> Objects { get; } = new();
    internal List<FlushChannel> FlushChans { get; } = new();

    public void Close()
    {
        foreach (var c in FlushChans)
            c.Close();
    }
}

internal class QueuedObjects<T>
{
    public long SequenceNumber;
    public T[] Objects = Array.Empty<T>();
    public FlushChannel? FlushChan;
}

public class Queue<T>
{
    private readonly int _batchSize;
    private readonly TimeSpan _timeout;
    private readonly Channel<QueuedObjects<T>> _batchCh;
    private readonly Channel<bool> _flush;
    private readonly Channel<Request<T>> _sendCh = Channel.CreateUnbounded<Request<T>>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runTask;
    private long _seqNum;
    private int _depth;

    public ChannelReader<Request<T>> C => _sendCh.Reader;

    public Queue(int maxSize, int batchSize, TimeSpan timeout)
    {
        _batchSize = batchSize;
        _timeout = timeout;
        _batchCh = Channel.CreateBounded<QueuedObjects<T>>(maxSize);
        _flush = Channel.CreateUnbounded<bool>();
        _runTask = Task.Run(RunAsync);
        _seqNum = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public async Task<long> WriteAsync(T[] objects, FlushChannel? c = null)
    {
        if (_cts.IsCancellationRequested)
            throw new InvalidOperationException("queue is closed");

        var q = new QueuedObjects<T> { Objects = objects, FlushChan = c };
        q.SequenceNumber = Interlocked.Increment(ref _seqNum);
        Interlocked.Increment(ref _depth);
        await _batchCh.Writer.WriteAsync(q, _cts.Token);
        return q.SequenceNumber;
    }

    public void Flush() => _flush.Writer.TryWrite(true);

    public int Depth => _depth;

    public Dictionary<string, object> Stats() => new()
    {
        ["batch_size"] = _batchSize,
        ["timeout"] = _timeout.ToString()
    };

    public async Task CloseAsync()
    {
        _cts.Cancel();
        _batchCh.Writer.TryComplete();
        _flush.Writer.TryComplete();
        await _runTask;
    }

    private async Task RunAsync()
    {
        var queued = new List<QueuedObjects<T>>();
        var token = _cts.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var readTask = _batchCh.Reader.ReadAsync(token).AsTask();
                var flushTask = _flush.Reader.ReadAsync(token).AsTask();
                var delayTask = Task.Delay(_timeout, token);
                var completed = await Task.WhenAny(readTask, flushTask, delayTask);

                if (completed == readTask)
                {
                    var obj = await readTask;
                    queued.Add(obj);
                    Interlocked.Decrement(ref _depth);
                }
                else if (completed == flushTask)
                {
                    _ = await flushTask;
                }
                // timeout handled by else

                if (queued.Count >= _batchSize || completed == flushTask || completed == delayTask)
                {
                    if (queued.Count > 0)
                    {
                        var req = MergeQueued(queued);
                        queued.Clear();
                        await _sendCh.Writer.WriteAsync(req, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // flush remaining
        if (queued.Count > 0)
        {
            var req = MergeQueued(queued);
            await _sendCh.Writer.WriteAsync(req, CancellationToken.None);
        }
        _sendCh.Writer.TryComplete();
    }

    private static Request<T> MergeQueued(List<QueuedObjects<T>> qs)
    {
        var req = new Request<T>();
        if (qs.Count > 0)
            req.SequenceNumber = qs[0].SequenceNumber;

        foreach (var q in qs)
        {
            if (q.SequenceNumber > req.SequenceNumber)
                req.SequenceNumber = q.SequenceNumber;
            req.Objects.AddRange(q.Objects);
            if (q.FlushChan != null)
                req.FlushChans.Add(q.FlushChan);
        }
        return req;
    }
}
