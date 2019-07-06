using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS4014

namespace RelationalLock {

    internal class LockContainer : IDisposable {
        private readonly SemaphoreSlim mainSemaphore;
        private readonly CancellationTokenSource mainSource;
        private readonly object releaseSourceLock = new object();
        private readonly ImmutableList<NamedSemaphore> semaphoreList;
        private readonly Stack<NamedSemaphore> waitedStack;
        private int disposed;
        private bool locked;
        private CancellationTokenSource releaseSource;

        public LockContainer(string key, IEnumerable<NamedSemaphore> targetList) {
            Key = key;
            semaphoreList = targetList.OrderBy(target => target.Name).ToImmutableList();
            mainSemaphore = new SemaphoreSlim(1, 1);
            waitedStack = new Stack<NamedSemaphore>();
            mainSource = new CancellationTokenSource();
            releaseSource = new CancellationTokenSource();
            TargetLockKeys = semaphoreList.Select(semaphore => semaphore.Name).ToImmutableArray();
        }

        ~LockContainer() {
            Dispose(false);
        }

        public bool Acquire(DateTimeOffset timeoutAt, TimeSpan expireIn) {
            CheckIsDisposed();
            return SyncAction<bool?>(() => {
                locked = true;
                foreach (var semaphore in semaphoreList) {
                    if (semaphore.Wait(timeoutAt.FromNow()) == false) {
                        ReleaseCore();
                        return false;
                    }
                    waitedStack.Push(semaphore);
                }
                ReleaseAtExpiration(expireIn);
                return true;
            }, timeoutAt.FromNow()) ?? false;
        }

        public void Dispose() => Dispose(true);

        public LockStateInfo GetState() =>
            new LockStateInfo(
                Key,
                locked || semaphoreList.Any(s => s.Locked) ? LockState.Locked : LockState.Unlocked
            );

        public void Release() {
            if (locked == false) {
                return;
            }
            SyncAction(ReleaseCore);
            lock (releaseSourceLock) {
                releaseSource.Cancel();
                releaseSource = new CancellationTokenSource();
            }
        }

        private void CheckIsDisposed() {
            if (disposed != 0) {
                throw new ObjectDisposedException(nameof(mainSemaphore));
            }
        }

        private void Dispose(bool disposing) {
            if (Interlocked.Exchange(ref disposed, 1) != 0) {
                return;
            }
            mainSource.Cancel();
            Release();
            releaseSource.Dispose();
            mainSemaphore.Dispose();
            mainSource.Dispose();
            semaphoreList.ForEach(semahore => semahore.Dispose());
            if (disposing) {
                semaphoreList.Clear();
            }
        }

        private async Task ReleaseAtExpiration(TimeSpan expireIn) {
            try {
                await Task.Delay(expireIn, releaseSource.Token);
                Release();
            }
            catch (TaskCanceledException) {
                // canceld
            }
        }

        private void ReleaseCore() {
            if (locked == false) {
                return;
            }
            foreach (var waited in waitedStack) {
                waited.Release();
            }
            waitedStack.Clear();
            locked = false;
        }

        private void SyncAction(Action action) {
            try {
                mainSemaphore.Wait(mainSource.Token);
            }
            catch (OperationCanceledException) {
                // for dispose
                return;
            }
            try {
                action();
            }
            finally {
                mainSemaphore.Release();
            }
        }

        private T SyncAction<T>(Func<T> func, TimeSpan timeout) {
            try {
                if (mainSemaphore.Wait(timeout, mainSource.Token) == false) {
                    return default;
                }
            }
            catch (OperationCanceledException) {
                // for dispose
                return default;
            }
            try {
                return func();
            }
            finally {
                mainSemaphore.Release();
            }
        }

        public string Key { get; }

        public IEnumerable<string> TargetLockKeys { get; }
    }
}
