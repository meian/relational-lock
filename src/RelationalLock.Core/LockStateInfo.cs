using System;
using System.Collections.Generic;

namespace RelationalLock {

    public class LockStateInfo {

        public LockStateInfo(string key, LockState state, string ownerKey = default, DateTimeOffset? expireAt = default) {
            Key = key;
            State = state;
            OwnerKey = ownerKey;
            ExpireAt = expireAt;
        }

        public DateTimeOffset? ExpireAt { get; }

        public string Key { get; }

        public string OwnerKey { get; }

        public LockState State { get; }
    }
}
