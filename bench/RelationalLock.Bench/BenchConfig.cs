using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using System;
using System.Collections.Generic;

namespace RelationalLock.Bench {

    public class BenchConfig : ManualConfig {

        public BenchConfig() {
            Add(MemoryDiagnoser.Default);
            Add(MarkdownExporter.GitHub);
            Add(RankColumn.Arabic);
            Add(DefaultColumnProviders.Job);
            Add(DefaultColumnProviders.Statistics);
            Add(DefaultColumnProviders.Params);
            Add(DefaultColumnProviders.Descriptor);
            Add(ConsoleLogger.Default);
            Add(WithDefaultCount(Job.Clr, ".net 4.7.2"));
            Add(WithDefaultCount(Job.Core, ".net core 2.2").AsBaseline());
        }

        private Job WithDefaultCount(Job job, string id) =>
            job
            .WithLaunchCount(1)
            .WithUnrollFactor(2)
            .WithInvocationCount(10)
            .WithWarmupCount(3)
            .WithId(id);
    }
}
