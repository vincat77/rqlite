using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ReadyTarget<T> where T : IComparable<T>
{
    private readonly object _lock = new();
    private T _currentTarget = default!;
    private readonly List<Subscriber> _subscribers = new();

    public class Subscriber
    {
        public T Target { get; }
        public TaskCompletionSource<object?> Tcs { get; }
        public Task Task => Tcs.Task;

        public Subscriber(T target)
        {
            Target = target;
            Tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public Task SubscribeAsync(T target)
    {
        lock (_lock)
        {
            if (Comparer<T>.Default.Compare(target, _currentTarget) <= 0)
            {
                return Task.CompletedTask;
            }
            var sub = new Subscriber(target);
            _subscribers.Add(sub);
            return sub.Task;
        }
    }

    public void Unsubscribe(Task task)
    {
        lock (_lock)
        {
            for (int i = 0; i < _subscribers.Count; i++)
            {
                if (_subscribers[i].Task == task)
                {
                    _subscribers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void Signal(T index)
    {
        List<Subscriber>? toClose = null;
        lock (_lock)
        {
            if (Comparer<T>.Default.Compare(index, _currentTarget) <= 0)
                return;
            _currentTarget = index;
            for (int i = _subscribers.Count - 1; i >= 0; i--)
            {
                if (Comparer<T>.Default.Compare(index, _subscribers[i].Target) >= 0)
                {
                    toClose ??= new List<Subscriber>();
                    toClose.Add(_subscribers[i]);
                    _subscribers.RemoveAt(i);
                }
            }
        }
        if (toClose != null)
        {
            foreach (var s in toClose)
            {
                s.Tcs.TrySetResult(null);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _currentTarget = default!;
            _subscribers.Clear();
        }
    }

    public int Len
    {
        get { lock (_lock) { return _subscribers.Count; } }
    }
}
