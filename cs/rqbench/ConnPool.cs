using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

public delegate Socket ConnectionFactory();

public interface IPool
{
    Socket Get();
    void Close();
    int Len { get; }
    Dictionary<string, object> Stats();
}

public class PooledConn : IDisposable
{
    public Socket Inner { get; }
    private readonly ChannelPool _pool;
    private bool _unusable;
    private readonly object _lock = new();

    public PooledConn(Socket inner, ChannelPool pool)
    {
        Inner = inner;
        _pool = pool;
    }

    public void MarkUnusable()
    {
        lock (_lock)
        {
            _unusable = true;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_unusable)
            {
                Inner.Dispose();
                _pool.DecrementOpen();
            }
            else
            {
                _pool.Return(Inner);
            }
        }
    }
}

public class ChannelPool : IPool
{
    private readonly ConcurrentQueue<Socket> _conns;
    private readonly ConnectionFactory _factory;
    private readonly int _maxCap;
    private bool _closed;
    private int _openConns;
    private readonly object _lock = new();

    public ChannelPool(int maxCap, ConnectionFactory factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (maxCap <= 0) throw new ArgumentException("invalid capacity", nameof(maxCap));
        _factory = factory;
        _maxCap = maxCap;
        _conns = new ConcurrentQueue<Socket>();
    }

    public Socket Get()
    {
        if (_closed) throw new InvalidOperationException("pool is closed");
        if (_conns.TryDequeue(out var conn))
            return Wrap(conn);
        lock (_lock)
        {
            if (_closed) throw new InvalidOperationException("pool is closed");
            if (_openConns >= _maxCap)
            {
                if (_conns.TryDequeue(out conn))
                    return Wrap(conn);
                throw new InvalidOperationException("pool exhausted");
            }
            conn = _factory();
            _openConns++;
            return Wrap(conn);
        }
    }

    private PooledConn Wrap(Socket conn) => new(conn, this);

    internal void Return(Socket conn)
    {
        if (_closed || _conns.Count >= _maxCap)
        {
            conn.Dispose();
            DecrementOpen();
            return;
        }
        _conns.Enqueue(conn);
    }

    internal void DecrementOpen()
    {
        lock (_lock) { _openConns--; }
    }

    public void Close()
    {
        lock (_lock)
        {
            if (_closed) return;
            _closed = true;
        }
        while (_conns.TryDequeue(out var c))
        {
            c.Dispose();
        }
        _openConns = 0;
    }

    public int Len => _conns.Count;

    public Dictionary<string, object> Stats()
    {
        return new Dictionary<string, object>
        {
            ["idle"] = _conns.Count,
            ["open_connections"] = _openConns,
            ["max_open_connections"] = _maxCap
        };
    }
}
