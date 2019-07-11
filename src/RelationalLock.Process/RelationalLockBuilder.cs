using System;
using System.Collections.Generic;

namespace RelationalLock {

    /// <summary>
    /// Lock contol builder for process model.
    /// </summary>
    public class RelationalLockBuilder : IRelationalLockBuilder {

        /// <summary>
        /// Build <see cref="IRelationalLockManager"/> instance.
        /// Especially, <see cref="RelationalLockManager"/> is created.
        /// </summary>
        /// <param name="configurator">configurator with relationships</param>
        /// <returns><see cref="IRelationalLockManager"/> instance</returns>
        public IRelationalLockManager Build(RelationalLockConfigurator configurator) {
            if (configurator == null) {
                throw new ArgumentNullException(nameof(configurator));
            }
            if (configurator.RelationCount < 1) {
                throw new ArgumentException("not registered key relations.", nameof(configurator));
            }
            return new RelationalLockManager(configurator);
        }
    }
}
