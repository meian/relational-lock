using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace RelationalLock {

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

        ~RelationalLockManager() {
            Dispose(false);
        }

        public bool AcquireLock(string key, TimeSpan? timeout = null, TimeSpan? expireIn = null) {
            CheckIsDisposed();
            IsValidKey(key);
            return lockMap[key].Acquire((timeout ?? defaultTimeout).FromNowAt(), (expireIn ?? defaultExpireIn).Correct());
        }

        public bool AcquireLock(string key, TimeSpan timeout, DateTimeOffset? expireAt = null) =>
            AcquireLock(key, timeout, expireAt != null ? expireAt.Value.FromNow() : defaultExpireIn);

        public void Dispose() => Dispose(true);

        public Dictionary<string, LockStateInfo> GetAllStates() => AvailableKeys.ToDictionary(key => key, GetState);

        public LockStateInfo GetState(string key) {
            CheckIsDisposed();
            IsValidKey(key);
            return lockMap[key].GetState();
        }

        public void Release(string key) {
            IsValidKey(key);
            if (lockMap.TryGetValue(key, out var lockEntry)) {
                lockEntry.Release();
            }
        }

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

        public IEnumerable<string> AvailableKeys { get; private set; }
    }
}
