using System;
using Xunit.Abstractions;

namespace RelationalLock.Tests {

    public class TestBase {

        protected ITestOutputHelper Helper { get; }

        protected TestBase(ITestOutputHelper helper) {
            Helper = helper;
        }
    }
}
