using System;
using System.Collections.Generic;

namespace RelationalLock {

    public interface IRelationalLockManager : IDisposable {

        bool AcquireLock(string key, TimeSpan? timeout = default, TimeSpan? expireIn = default);

        bool AcquireLock(string key, TimeSpan timeout, DateTime? expireAt = default);

        Dictionary<string, LockStateInfo> GetAllStates();

        LockStateInfo GetState(string key);

        void Release(string key);

        IEnumerable<string> AvailableKeys { get; }
    }
}
