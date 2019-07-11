using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace RelationalLock {

    /// <summary>
    /// Lock control instance by in process model.
    /// </summary>
    public class RelationalLockManager : IRelationalLockManager {
        private readonly ImmutableDictionary<string, LockContainer> lockMap;
        private TimeSpan defaultExpireIn;
        private TimeSpan defaultTimeout;
        private int disposed;

        internal RelationalLockManager(RelationalLockConfigurator configurator) {
            var infos = configurator.GetInfos();
            var semaphoreMap = infos
                .SelectMany(info => info.LockKeys)
                .Distinct()
                .ToDictionary(lockKey => lockKey, lockKey => new NamedSemaphore(lockKey));
            lockMap = infos.ToImmutableDictionary(
                info => info.Key,
                info => new LockContainer(info.Key, info.LockKeys.Select(key => semaphoreMap[key])));
            AvailableKeys = lockMap.Keys.OrderBy(_ => _).ToImmutableArray();
            defaultTimeout = configurator.DefaultTimeout;
            defaultExpireIn = configurator.DefaultExpireIn;
        }

        /// <summary>
        /// destructor
        /// </summary>
        ~RelationalLockManager() {
            Dispose(false);
        }

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeout">timeout for requesting acquirement</param>
        /// <param name="expireIn">expire period for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        public bool AcquireLock(string key, TimeSpan? timeout = null, TimeSpan? expireIn = null) {
            CheckIsDisposed();
            IsValidKey(key);
            var timeoutAt = (timeout ?? defaultTimeout).FromNowAt();
            var expireInCorrected = expireIn != null ? expireIn.Value.Correct() : defaultExpireIn;
            return lockMap[key].Acquire(timeoutAt, expireInCorrected);
        }

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeout">timeout for requesting acquirement</param>
        /// <param name="expireAt">expiration for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        public bool AcquireLock(string key, TimeSpan timeout, DateTime? expireAt = null) =>
            AcquireLock(key, timeout, expireAt != null ? expireAt.Value.FromNow() : defaultExpireIn);

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeoutMilliseconds">timeout milliseconds for requesting acquirement</param>
        /// <param name="expireInMilliseconds">expire period milliseconds for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        public bool AcquireLock(string key, int timeoutMilliseconds, int? expireInMilliseconds = null) {
            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            var expireIn = expireInMilliseconds != null ? TimeSpan.FromMilliseconds(expireInMilliseconds.Value) : defaultExpireIn;
            return AcquireLock(key, timeout, expireIn);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Get all locking information on managed keys.
        /// </summary>
        /// <returns>locking information list</returns>
        public Dictionary<string, LockStateInfo> GetAllStates() => AvailableKeys.ToDictionary(key => key, GetState);

        /// <summary>
        /// Get locking information.
        /// </summary>
        /// <param name="key">key for locking information</param>
        /// <returns>locking information</returns>
        public LockStateInfo GetState(string key) {
            CheckIsDisposed();
            IsValidKey(key);
            return lockMap[key].GetState();
        }

        /// <summary>
        /// Release the lock by key.
        /// </summary>
        /// <param name="key">key for releasing lock</param>
        public void Release(string key) {
            IsValidKey(key);
            if (lockMap.TryGetValue(key, out var lockEntry)) {
                lockEntry.Release();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">if true, call from <see cref="Dispose()"/></param>
        protected virtual void Dispose(bool disposing) {
            if (Interlocked.Exchange(ref disposed, 1) != 0) {
                return;
            }
            foreach (var lockObject in lockMap.Values) {
                lockObject.Dispose();
            }
            if (disposing) {
                lockMap.Clear();
                AvailableKeys = Array.Empty<string>();
            }
        }

        private void CheckIsDisposed() {
            if (disposed != 0) {
                throw new ObjectDisposedException("disposable item");
            }
        }

        private void IsValidKey(string key) {
            if (lockMap.ContainsKey(key) == false) {
                throw new ArgumentOutOfRangeException(nameof(key), $"not registered key: {key}");
            }
        }

        /// <summary>
        /// List of keys available for locking.
        /// </summary>
        public IEnumerable<string> AvailableKeys { get; private set; }
    }
}
