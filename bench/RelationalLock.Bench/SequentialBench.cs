using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RelationalLock.Bench {

    public class SequentialBench {

        [Params(10, 100)]
        public int LoopCount;

        private IRelationalLockManager manager2;
        private IRelationalLockManager manager5;

        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new RelationalLockBuilder();
            var cfg = new RelationalLockConfigurator();
            cfg.RegisterRelation("key1", "key2", "key3");
            cfg.RegisterRelation("key2", "key4");
            manager2 = builder.Build(cfg);
            cfg.RegisterRelation("key4", "key5");
            cfg.RegisterRelation("key2", "key5");
            cfg.RegisterRelation("key1", "key5");
            manager5 = builder.Build(cfg);
        }

        [Benchmark]
        public void RunSequence2() {
            for (var i = 0; i < LoopCount; i++) {
                manager2.AcquireLock("key1");
                manager2.Release("key1");
            }
        }

        [Benchmark]
        public void RunSequence5() {
            for (var i = 0; i < LoopCount; i++) {
                manager5.AcquireLock("key5");
                manager5.Release("key5");
            }
        }
    }
}
