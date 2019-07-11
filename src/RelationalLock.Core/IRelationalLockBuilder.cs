using System;
using System.Collections.Generic;

namespace RelationalLock {

    /// <summary>
    /// Represents builder to create <see cref="IRelationalLockManager"/> with <see cref="RelationalLockConfigurator"/>.
    /// </summary>
    public interface IRelationalLockBuilder {

        /// <summary>
        /// Build <see cref="IRelationalLockManager"/> instance.
        /// </summary>
        /// <param name="configurator">configurator with relationships</param>
        /// <returns><see cref="IRelationalLockManager"/> instance</returns>
        IRelationalLockManager Build(RelationalLockConfigurator configurator);
    }
}
