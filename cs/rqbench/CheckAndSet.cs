using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class CheckAndSet
{
    private bool _state;
    private string _owner = string.Empty;
    private DateTime _startTime;
    private readonly object _lock = new();

    public void Begin(string owner)
    {
        lock (_lock)
        {
            if (_state)
                throw new InvalidOperationException($"CAS conflict: currently held by owner \"{_owner}\" for {DateTime.UtcNow - _startTime}");
            _owner = owner;
            _state = true;
            _startTime = DateTime.UtcNow;
        }
    }

    public async Task BeginWithRetry(string owner, TimeSpan timeout, TimeSpan retryInterval)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                Begin(owner);
                return;
            }
            catch (InvalidOperationException)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("CAS conflict timeout");
                await Task.Delay(retryInterval);
            }
        }
    }

    public void End()
    {
        lock (_lock)
        {
            _owner = string.Empty;
            _state = false;
            _startTime = default;
        }
    }

    public string Owner
    {
        get { lock (_lock) { return _owner; } }
    }

    public Dictionary<string, object?> Stats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, object?> { ["owner"] = null };
            if (_state)
            {
                stats["owner"] = _owner;
                stats["duration"] = DateTime.UtcNow - _startTime;
            }
            return stats;
        }
    }
}
