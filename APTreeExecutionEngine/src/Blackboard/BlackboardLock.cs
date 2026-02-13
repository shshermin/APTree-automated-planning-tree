public class BlackboardLock 
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public IDisposable AcquireReadLock()
    {
        _lock.EnterReadLock();
        return new LockReleaser(() => _lock.ExitReadLock());
    }

    public IDisposable AcquireWriteLock()
    {
        _lock.EnterWriteLock();
        return new LockReleaser(() => _lock.ExitWriteLock());
    }

    private class LockReleaser : IDisposable
    {
        private readonly Action _releaseAction;
        
        public LockReleaser(Action releaseAction)
        {
            _releaseAction = releaseAction;
        }

        public void Dispose()
        {
            _releaseAction();
        }
    }

   
}