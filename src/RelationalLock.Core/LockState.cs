using System;
using System.Collections.Generic;

namespace RelationalLock {

    /// <summary>
    /// Specified lock status.
    /// </summary>
    public enum LockState {

        /// <summary>
        /// Currently locked.
        /// </summary>
        Locked = 1,

        /// <summary>
        /// Currently not locked.
        /// </summary>
        Unlocked = 2
    }
}
