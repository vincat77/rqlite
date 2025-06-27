using System;

public class MultiRSW
{
    private bool _writerActive;
    private int _numReaders;
    private readonly object _lock = new();

    public void BeginRead()
    {
        lock (_lock)
        {
            if (_writerActive)
                throw new InvalidOperationException("MRSW conflict");
            _numReaders++;
        }
    }

    public void EndRead()
    {
        lock (_lock)
        {
            _numReaders--;
            if (_numReaders < 0)
                throw new InvalidOperationException("reader count went negative");
        }
    }

    public void BeginWrite()
    {
        lock (_lock)
        {
            if (_writerActive || _numReaders > 0)
                throw new InvalidOperationException("MRSW conflict");
            _writerActive = true;
        }
    }

    public void EndWrite()
    {
        lock (_lock)
        {
            if (!_writerActive)
                throw new InvalidOperationException("write done received but no write is active");
            _writerActive = false;
        }
    }

    public void UpgradeToWriter()
    {
        lock (_lock)
        {
            if (_writerActive || _numReaders > 1)
                throw new InvalidOperationException("MRSW conflict");
            if (_numReaders == 0)
                throw new InvalidOperationException("upgrade attempted with no readers");
            _writerActive = true;
            _numReaders = 0;
        }
    }
}
