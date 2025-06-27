using System;
using System.Diagnostics;
using System.Threading;

public class PollTrue
{
    private readonly Func<bool> _fn;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _timeout;

    public PollTrue(Func<bool> fn, TimeSpan pollInterval, TimeSpan timeout)
    {
        _fn = fn;
        _pollInterval = pollInterval;
        _timeout = timeout;
    }

    public void Run(string name)
    {
        var sw = Stopwatch.StartNew();
        if (_fn())
            return;
        while (sw.Elapsed < _timeout)
        {
            Thread.Sleep(_pollInterval);
            if (_fn())
                return;
        }
        throw new TimeoutException($"timeout waiting for {name}");
    }
}
