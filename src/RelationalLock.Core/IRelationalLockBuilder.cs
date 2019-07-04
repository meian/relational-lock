using System;
using System.Collections.Generic;

namespace RelationalLock {

    public interface IRelationalLockBuilder {

        IRelationalLockManager Build(RelationalLockConfigurator configurator);
    }
}
