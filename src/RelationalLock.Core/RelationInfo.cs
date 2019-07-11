using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RelationalLock {

    /// <summary>
    /// Information indicating key relationships.
    /// </summary>
    public class RelationalInfo {

        internal RelationalInfo(string key, IEnumerable<string> relatedKeys, IEnumerable<string> lockKeys) {
            Key = key;
            RelatedKeys = relatedKeys.ToImmutableHashSet();
            LockKeys = lockKeys.ToImmutableArray();
        }

        /// <summary>
        /// Key of relationship origin.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Key list related to <see cref="Key"/>.
        /// </summary>
        public ImmutableHashSet<string> RelatedKeys { get; }

        /// <summary>
        /// Key list used for actual lock acquisition.
        /// </summary>
        public ImmutableArray<string> LockKeys { get; }
    }
}
