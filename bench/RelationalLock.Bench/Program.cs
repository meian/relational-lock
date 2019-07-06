using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace RelationalLock.Bench {

    internal class Program {

        private static void Main(string[] args) {
            var assembly = typeof(Program).Assembly;
            var switcher = new BenchmarkSwitcher(assembly);
            var target = assembly.GetTypes().FirstOrDefault(t => t.Name == args?.FirstOrDefault()) ?? typeof(ParallelBench);
            var benchArgs = new string[] {
                "--filter",
                target.Name
            };
            var config = new BenchConfig();
            switcher.Run(benchArgs, config);
        }
    }
}
