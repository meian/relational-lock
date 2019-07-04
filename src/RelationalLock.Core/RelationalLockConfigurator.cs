using System;
using System.Collections.Generic;
using System.Linq;

namespace RelationalLock {

    public class RelationalLockConfigurator {
        private readonly HashSet<HashSet<string>> keySets = new HashSet<HashSet<string>>();
        private TimeSpan defaultExpireIn = LockTimeConstants.MaxTimeSpan;
        private TimeSpan defaultTimeout = LockTimeConstants.MaxTimeSpan;

        public List<RelationalInfo> GetInfos() {
            if (keySets.Any() == false) {
                throw new InvalidOperationException("no key set are configurated.");
            }
            var keys = keySets.SelectMany(_ => _).Distinct().OrderBy(_ => _).ToList();
            return keys
                .Select(key => new { key, targets = keySets.Where(set => set.Contains(key)).ToList() })
                .Select(e => new RelationalInfo(
                    key: e.key,
                    relatedKeys: e.targets.SelectMany(_ => _).Distinct().Where(k => k != e.key),
                    lockKeys: e.targets.Select(GenerateLockKey)
                    ))
                .ToList();
        }

        public RelationalLockConfigurator RegisterRelation(IEnumerable<string> keys) {
            if (keys == null) {
                throw new ArgumentNullException(nameof(keys));
            }
            return RegisterRelation((keys as string[]) ?? keys.ToArray());
        }

        public RelationalLockConfigurator RegisterRelation(params string[] keys) {
            if (keys == null) {
                throw new ArgumentNullException(nameof(keys));
            }
            if (keys.Length < 2) {
                throw new ArgumentException($"more than 2 keys: {keys.ToViewString()}", nameof(keys));
            }
            if (keys.Any(string.IsNullOrEmpty)) {
                throw new ArgumentException($"null or empty key is contained: {keys.ToViewString()}", nameof(keys));
            }
            if (keys.Length != keys.Distinct().Count()) {
                throw new ArgumentException($"duplicate key is exists: {keys.ToViewString()}", nameof(keys));
            }
            var superset = keySets.FirstOrDefault(keySet => keySet.IsSupersetOf(keys));
            if (superset != null) {
                // same or superset exists.
                return this;
            }
            // for merge subsets
            keySets.RemoveWhere(keySet => keySet.IsProperSubsetOf(keys));
            // new keyset
            keySets.Add(new HashSet<string>(keys));
            return this;
        }

        private static string GenerateLockKey(IEnumerable<string> keys) => $"{string.Join("#", keys.OrderBy(_ => _))}$$lockkey";

        public TimeSpan DefaultExpireIn {
            get => defaultExpireIn;
            set => defaultExpireIn = value.ValidDefaultTimeSpan(nameof(DefaultExpireIn));
        }

        public TimeSpan DefaultTimeout {
            get => defaultTimeout;
            set => defaultTimeout = value.ValidDefaultTimeSpan(nameof(DefaultTimeout));
        }

        public int KeyCount => keySets.SelectMany(_ => _).Distinct().Count();

        public int RelationCount => keySets.Count;
    }
}
