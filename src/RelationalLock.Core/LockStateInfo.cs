using System;
using System.Collections.Generic;

namespace RelationalLock {

    /// <summary>
    /// Information indicating the lock status.
    /// By <see cref="IRelationalLockManager.GetState(string)"/> or <see cref="IRelationalLockManager.GetAllStates()"/>.
    /// </summary>
    public class LockStateInfo {

        /// <summary>
        /// Create information with lock construction values.
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="state">lock status</param>
        /// <param name="ownerKey">lock owner key</param>
        /// <param name="expireAt">lock expiration</param>
        public LockStateInfo(string key, LockState state, string ownerKey = default, DateTime? expireAt = default) {
            Key = key;
            State = state;
            OwnerKey = ownerKey;
            ExpireAt = expireAt;
        }

        /// <summary>
        /// Lock expiration.
        /// </summary>
        public DateTime? ExpireAt { get; }

        /// <summary>
        /// Target key for information.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Lock owner key by <see cref="IRelationalLockManager.AcquireLock(string, TimeSpan?, TimeSpan?)"/>.
        /// </summary>
        public string OwnerKey { get; }

        /// <summary>
        /// Lock status.
        /// </summary>
        public LockState State { get; }
    }
}
