using System;
using System.Threading;

public class AtomicMonotonicUInt64
{
    private ulong _value;
    private readonly object _lock = new();

    public ulong Load()
    {
        lock (_lock)
        {
            return _value;
        }
    }

    public void Store(ulong v)
    {
        lock (_lock)
        {
            if (v > _value)
                _value = v;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _value = 0;
        }
    }
}

public class AtomicTime
{
    private DateTime _time;
    private readonly ReaderWriterLockSlim _lock = new();

    public void Store(DateTime t)
    {
        _lock.EnterWriteLock();
        try
        {
            _time = t;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public DateTime Load()
    {
        _lock.EnterReadLock();
        try
        {
            return _time;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Add(TimeSpan d)
    {
        _lock.EnterWriteLock();
        try
        {
            _time = _time.Add(d);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public TimeSpan Sub(AtomicTime t)
    {
        _lock.EnterReadLock();
        try
        {
            return _time - t._time;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsZero()
    {
        _lock.EnterReadLock();
        try
        {
            return _time == default;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}

public class AtomicBool
{
    private int _state; // 1 true, 0 false

    public void Set() => Interlocked.Exchange(ref _state, 1);
    public void Unset() => Interlocked.Exchange(ref _state, 0);
    public bool Is() => Interlocked.CompareExchange(ref _state, 0, 0) == 1;
}

public class AtomicString
{
    private string _value = string.Empty;
    private readonly ReaderWriterLockSlim _lock = new();

    public void Store(string s)
    {
        _lock.EnterWriteLock();
        try
        {
            _value = s;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string Load()
    {
        _lock.EnterReadLock();
        try
        {
            return _value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
