using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RelationalLock {
    public class RelationalInfo {
        internal RelationalInfo(string key, IEnumerable<string> relatedKeys, IEnumerable<string> lockKeys) {
            Key = key;
            RelatedKeys = relatedKeys.ToImmutableHashSet();
            LockKeys = lockKeys.ToImmutableArray();
        }

        public string Key { get; }

        public ImmutableHashSet<string> RelatedKeys { get; }

        public ImmutableArray<string> LockKeys { get; }
    }
}
