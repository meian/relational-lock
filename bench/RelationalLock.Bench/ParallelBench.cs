using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RelationalLock.Bench {

    public class ParallelBench {

        [Params(1, 10, 100)]
        public int LoopCount;

        private IRelationalLockManager manager;

        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new RelationalLockBuilder();
            var cfg = new RelationalLockConfigurator();
            cfg.RegisterRelation("key1", "key2");
            cfg.RegisterRelation("key2", "key3");
            cfg.RegisterRelation("key3", "key4");
            cfg.RegisterRelation("key4", "key5");
            cfg.RegisterRelation("key5", "key1");
            manager = builder.Build(cfg);
        }

        [Benchmark]
        public async Task RunParallel() {
            var keys = new[] { "key1", "key2", "key3", "key4", "key5" };
            await Task.WhenAll(keys.Select(key => Task.Run(() => {
                for (var i = 0; i < LoopCount; i++) {
                    manager.AcquireLock(key);
                    manager.Release(key);
                }
            })));
        }
    }
}
