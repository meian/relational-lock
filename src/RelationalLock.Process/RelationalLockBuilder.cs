using System;
using System.Collections.Generic;

namespace RelationalLock {

    public class RelationalLockBuilder : IRelationalLockBuilder {

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
