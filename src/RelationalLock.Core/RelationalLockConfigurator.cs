using System;
using System.Collections.Generic;
using System.Linq;

namespace RelationalLock {

    /// <summary>
    /// Configure lock relationships.
    /// </summary>
    public class RelationalLockConfigurator {
        private readonly HashSet<HashSet<string>> keySets = new HashSet<HashSet<string>>();
        private TimeSpan defaultExpireIn = LockTimeConstants.MaxTimeSpan;
        private TimeSpan defaultTimeout = LockTimeConstants.MaxTimeSpan;

        /// <summary>
        /// Get registered relationship information list.
        /// </summary>
        /// <returns>list of relationship information.</returns>
        public List<RelationalInfo> GetInfos() {
            if (keySets.Any() == false) {
                throw new InvalidOperationException("no key set are configurated.");
            }
            // collect more than 1 existance.
            var keys = keySets.SelectMany(_ => _)
                .Distinct()
                .OrderBy(_ => _)
                .ToList();
            return keys
                .Select(key => new { key, targetSets = keySets.Where(set => set.Contains(key)).ToList() })
                .Select(e => new RelationalInfo(
                    key: e.key,
                    relatedKeys: e.targetSets.SelectMany(_ => _).Distinct().Where(k => k != e.key),
                    lockKeys: e.targetSets.Select(GenerateLockKey)
                    ))
                .ToList();
        }

        /// <summary>
        /// Register keys relationship by keys collection.
        /// </summary>
        /// <param name="keys">keys collection for relationship</param>
        /// <returns>Configurator instance</returns>
        /// <exception cref="ArgumentNullException">
        /// When argument is null.
        /// </exception>
        public RelationalLockConfigurator RegisterRelation(IEnumerable<string> keys) {
            if (keys == null) {
                throw new ArgumentNullException(nameof(keys));
            }
            return RegisterRelation((keys as string[]) ?? keys.ToArray());
        }

        /// <summary>
        /// Register keys relationship by more two keys.
        /// </summary>
        /// <param name="keys">keys array for relationship</param>
        /// <returns>configurator instance</returns>
        /// <exception cref="ArgumentNullException">
        /// When argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When keys count less than 2(one or zero).
        /// When keys contains null or empty string.
        /// When exist duplicate keys.
        /// </exception>
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

        /// <summary>
        /// Default expire period for call <see cref="IRelationalLockManager.AcquireLock(string, TimeSpan?, TimeSpan?)"/> without expireIn argument.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When specified for out of range TimeSpan.
        /// See <see cref="LockTimeConstants.MinTimeSpan"/> and <see cref="LockTimeConstants.MaxTimeSpan"/>
        /// </exception>
        public TimeSpan DefaultExpireIn {
            get => defaultExpireIn;
            set => defaultExpireIn = value.ValidDefaultTimeSpan(nameof(DefaultExpireIn));
        }

        /// <summary>
        /// Default timeout for call <see cref="IRelationalLockManager.AcquireLock(string, TimeSpan?, TimeSpan?)"/> without timeout argument.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When specified for out of range TimeSpan.
        /// See <see cref="LockTimeConstants.MinTimeSpan"/> and <see cref="LockTimeConstants.MaxTimeSpan"/>
        /// </exception>
        public TimeSpan DefaultTimeout {
            get => defaultTimeout;
            set => defaultTimeout = value.ValidDefaultTimeSpan(nameof(DefaultTimeout));
        }

        /// <summary>
        /// Count for keys registered with some relationships.
        /// </summary>
        public int KeyCount => keySets.SelectMany(_ => _).Distinct().Count();

        /// <summary>
        /// Count for relationships.
        /// </summary>
        public int RelationCount => keySets.Count;
    }
}
