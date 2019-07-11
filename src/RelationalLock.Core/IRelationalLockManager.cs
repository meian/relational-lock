using System;
using System.Collections.Generic;

namespace RelationalLock {

    /// <summary>
    /// Represents lock control instance.
    /// </summary>
    public interface IRelationalLockManager : IDisposable {

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeout">timeout for requesting acquirement</param>
        /// <param name="expireIn">expire period for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        bool AcquireLock(string key, TimeSpan? timeout = default, TimeSpan? expireIn = default);

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeout">timeout for requesting acquirement</param>
        /// <param name="expireAt">expiration for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        bool AcquireLock(string key, TimeSpan timeout, DateTime? expireAt = default);

        /// <summary>
        /// Acquire a lock on key and related keys.
        /// </summary>
        /// <param name="key">key for locking</param>
        /// <param name="timeoutMilliseconds">timeout milliseconds for requesting acquirement</param>
        /// <param name="expireInMilliseconds">expire period milliseconds for acquired lock</param>
        /// <returns>true if acquired lock, false if timeout</returns>
        bool AcquireLock(string key, int timeoutMilliseconds, int? expireInMilliseconds = default);

        /// <summary>
        /// Get all locking information on managed keys.
        /// </summary>
        /// <returns>locking information list</returns>
        Dictionary<string, LockStateInfo> GetAllStates();

        /// <summary>
        /// Get locking information.
        /// </summary>
        /// <param name="key">key for locking information</param>
        /// <returns>locking information</returns>
        LockStateInfo GetState(string key);

        /// <summary>
        /// Release the lock by key.
        /// </summary>
        /// <param name="key">key for releasing lock</param>
        void Release(string key);

        /// <summary>
        /// List of keys available for locking.
        /// </summary>
        IEnumerable<string> AvailableKeys { get; }
    }
}
