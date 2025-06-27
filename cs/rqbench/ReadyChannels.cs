using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ReadyChannels
{
    private readonly object _lock = new();
    private readonly List<Task> _tasks = new();
    private long _closed;

    public void Register(Task task)
    {
        lock (_lock)
        {
            _tasks.Add(task);
        }
        task.ContinueWith(_ => Interlocked.Increment(ref _closed));
    }

    public bool Ready()
    {
        lock (_lock)
        {
            return Interlocked.Read(ref _closed) == _tasks.Count;
        }
    }
}
