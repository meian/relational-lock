using System;
using System.Collections.Generic;
using System.Threading;

namespace RelationalLock {

    internal sealed class NamedSemaphore : IDisposable {
        private readonly SemaphoreSlim semaphore;

        private int disposed;

        public NamedSemaphore(string name) {
            Name = name;
            semaphore = new SemaphoreSlim(1, 1);
        }

        ~NamedSemaphore() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
        }

        public void Release() {
            if (disposed != 0) {
                return;
            }
            semaphore.Release();
        }

        public override string ToString() => $"semaphore: {Name}";

        public bool Wait(TimeSpan timeout) {
            CheckIsDisposed();
            return semaphore.Wait(timeout);
        }

        private void CheckIsDisposed() {
            if (disposed != 0) {
                throw new ObjectDisposedException(nameof(semaphore));
            }
        }

        private void Dispose(bool disposing) {
            if (Interlocked.Exchange(ref disposed, 1) != 0) {
                return;
            }
            semaphore.Dispose();
        }

        public bool Locked => semaphore.CurrentCount == 0;

        public string Name { get; }
    }
}
