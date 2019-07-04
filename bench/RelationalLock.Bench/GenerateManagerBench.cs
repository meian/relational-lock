using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelationalLock.Bench {

    public class GenerateManagerBench {
        private RelationalLockConfigurator baseCfg;
        private IRelationalLockBuilder builder;

        [Benchmark]
        public RelationalLockConfigurator Get3Key2RelConfigurator() {
            var cfg = new RelationalLockConfigurator();
            cfg.RegisterRelation("key1", "key2");
            cfg.RegisterRelation("key2", "key3");
            return cfg;
        }

        [Benchmark]
        public IRelationalLockManager GetManager() => builder.Build(baseCfg);

        [GlobalSetup]
        public void GlobalSetup() {
            builder = new RelationalLockBuilder();
            baseCfg = new RelationalLockConfigurator();
            baseCfg.RegisterRelation("key1", "key2", "key3");
            baseCfg.RegisterRelation("key2", "key4");
        }
    }
}
