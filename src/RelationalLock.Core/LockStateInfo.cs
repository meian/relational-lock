using System;
using System.Collections.Generic;

namespace RelationalLock {

    public class LockStateInfo {

        public LockStateInfo(string key, LockState state, string ownerKey = default, DateTime? expireAt = default) {
            Key = key;
            State = state;
            OwnerKey = ownerKey;
            ExpireAt = expireAt;
        }

        public DateTime? ExpireAt { get; }

        public string Key { get; }

        public string OwnerKey { get; }

        public LockState State { get; }
    }
}
