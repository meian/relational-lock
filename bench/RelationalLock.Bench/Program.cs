using BenchmarkDotNet.Running;
using System;

namespace RelationalLock.Bench {

    internal class Program {

        private static void Main(string[] args) {
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            var benchArgs = new string[] {
                "--filter",
                nameof(ParallelBench)
            };
            var config = new BenchConfig();
            switcher.Run(benchArgs, config);
        }
    }
}
