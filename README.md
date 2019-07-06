# RelationalLock

RelationalLock is a library that locks multiple keys registered with relationships.

## Motivation

An approach to control synchronization between systems with different architectures.

## How to use

1. Create a lock controller instance implemented `IRelationalLockManager`.
   1. Register string key relations with `RelationalLockConfigurator`.
   2. Build control instance by builder implemented `IRelationalLockBuilder`.
2. Control lock and release by `AcquireLock` and `Release`.

## Quick Sample

This sample is:

- Emurate 3 services, and functions in services.
- Some functions are required exclusive execution.

```csharp
using RelationalLock;
using System;
using System.Threading.Tasks;

namespace RelationalLockSample {

  class Program {
    static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(100);

    static async Task Main(string[] args) {
      // generate contol instance with 2 exclusive relations defined.
      var configurator = new RelationalLockConfigurator()
        .RegisterRelation("ServiceA.FuncA1", "ServiceB.FuncB1")                     // 2 keys relations
        .RegisterRelation("ServiceA.FuncA1", "ServiceC.FuncC1", "ServiceB.FuncB2"); // other 3 keys relations
      var builder = new RelationalLockBuilder();
      var manager = builder.Build(configurator);

      // wait for all services.
      await Task.WhenAll(
        ServiceA(manager),
        ServiceB(manager),
        ServiceC(manager));
    }

    static async Task ServiceA(IRelationalLockManager manager) {
      if (manager.AcquireLock("ServiceA.FuncA1", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))) {
        Console.WriteLine("start ServiceA.FuncA1");
        // assume processing time for FuncA1.
        await Task.Delay(3000);
        Console.WriteLine("end ServiceA.FuncA1");
        manager.Release("ServiceA.FuncA1");
      }
      else {
        Console.WriteLine("failed start ServiceA.FuncA1");
      }
    }

    static async Task ServiceB(IRelationalLockManager manager) {
      await Task.Delay(300);

      if (manager.AcquireLock("ServiceB.FuncB1", TimeSpan.FromSeconds(1))) {
        Console.WriteLine("start ServiceB.FuncB1");
        // assume processing time for FuncB1.
        await Task.Delay(3000);
        Console.WriteLine("end ServiceA.FuncA1");
        manager.Release("ServiceA.FuncA1");
      }
      else {
        Console.WriteLine("failed start ServiceB.FuncB1");
      }

      if (manager.AcquireLock("ServiceB.FuncB2", TimeSpan.FromSeconds(3))) {
        Console.WriteLine("start ServiceB.FuncB2");
        await Task.Delay(500);
        Console.WriteLine("end ServiceB.FuncB2");
        manager.Release("ServiceB.FuncB2");
      }
      else {
        Console.WriteLine("failed start ServiceB.FuncB1");
      }
    }

    static async Task ServiceC(IRelationalLockManager manager) {
      await Task.Delay(600);

      if (manager.AcquireLock("ServiceC.FuncC1", TimeSpan.FromSeconds(3))) {
        Console.WriteLine("start ServiceC.FuncC1");
        await Task.Delay(500);
        Console.WriteLine("end ServiceC.FuncC1");
        manager.Release("ServiceC.FuncC1");
      }
      else {
        Console.WriteLine("failed start ServiceC.FuncC1");
      }
    }
  }
}
```

Result are below.

```
start ServiceA.FuncA1
failed start ServiceB.FuncB1
end ServiceA.FuncA1
start ServiceC.FuncC1
end ServiceC.FuncC1
start ServiceB.FuncB2
end ServiceB.FuncB2
```

## Target Architecture

Target framework of core and process library is .NET Standard 2.0.
(.NET Framework 4.7.1 or .NET Core 2.2 etc are aompatible)

## Benchmark

Below is benchmark results in `RelationalLock.Bench` project.

```:SequentialBench
// result of SequentialBench
// this benchmark is 2 case(2 relations and 5 relations) with sequential AcquireLock and Release execution.

// * Summary *

BenchmarkDotNet=v0.11.5, OS=Windows 7 SP1 (6.1.7601.0)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=3328359 Hz, Resolution=300.4484 ns, Timer=TSC
.NET Core SDK=2.2.300
  [Host]        : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  .net 4.7.2    : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3062.0
  .net core 2.2 : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

InvocationCount=10  LaunchCount=1  UnrollFactor=2
WarmupCount=3

| Job           | Runtime | Method       | LoopCount |        Mean |      Error |     StdDev |      Median | Ratio | RatioSD | Rank |
| ------------- | ------- | ------------ | --------- | ----------: | ---------: | ---------: | ----------: | ----: | ------: | ---: |
| .net 4.7.2    | Clr     | RunSequence2 | 1         |    16.18 us |  0.3134 us |  0.2778 us |    16.12 us |  1.14 |    0.02 |    2 |
| .net core 2.2 | Core    | RunSequence2 | 1         |    14.19 us |  0.2805 us |  0.2623 us |    14.18 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunSequence5 | 1         |    16.98 us |  0.3321 us |  0.5363 us |    16.89 us |  1.14 |    0.05 |    2 |
| .net core 2.2 | Core    | RunSequence5 | 1         |    14.90 us |  0.2920 us |  0.5037 us |    14.68 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunSequence2 | 10        |   140.45 us |  2.7819 us |  5.8069 us |   138.84 us |  1.20 |    0.06 |    2 |
| .net core 2.2 | Core    | RunSequence2 | 10        |   119.07 us |  2.3706 us |  2.6349 us |   118.53 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunSequence5 | 10        |   146.21 us |  3.0861 us |  3.3020 us |   145.88 us |  1.16 |    0.05 |    2 |
| .net core 2.2 | Core    | RunSequence5 | 10        |   126.23 us |  3.2100 us |  4.1739 us |   125.92 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunSequence2 | 100       | 1,339.02 us | 24.0970 us | 21.3614 us | 1,340.84 us |  1.16 |    0.02 |    2 |
| .net core 2.2 | Core    | RunSequence2 | 100       | 1,149.39 us | 21.9526 us | 20.5345 us | 1,149.21 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunSequence5 | 100       | 1,394.84 us | 23.7107 us | 22.1790 us | 1,390.60 us |  1.16 |    0.03 |    2 |
| .net core 2.2 | Core    | RunSequence5 | 100       | 1,198.68 us | 19.3855 us | 18.1332 us | 1,199.09 us |  1.00 |    0.00 |    1 |
```

```:ParallelBench
// result of ParallelBench
// this benchmark is 5 parallel AcquireLock and Release executions.

// * Summary *

BenchmarkDotNet=v0.11.5, OS=Windows 7 SP1 (6.1.7601.0)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=3328359 Hz, Resolution=300.4484 ns, Timer=TSC
.NET Core SDK=2.2.300
  [Host]        : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  .net 4.7.2    : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3062.0
  .net core 2.2 : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

InvocationCount=10  LaunchCount=1  UnrollFactor=2
WarmupCount=3

| Job           | Runtime | Method      | LoopCount |        Mean |      Error |     StdDev |      Median | Ratio | RatioSD | Rank |
| ------------- | ------- | ----------- | --------- | ----------: | ---------: | ---------: | ----------: | ----: | ------: | ---: |
| .net 4.7.2    | Clr     | RunParallel | 1         |   107.67 us |   5.230 us |  15.256 us |   102.66 us |  1.74 |    0.25 |    2 |
| .net core 2.2 | Core    | RunParallel | 1         |    61.01 us |   1.212 us |   2.091 us |    61.25 us |  1.00 |    0.00 |    1 |
|               |         |             |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunParallel | 10        |   679.38 us |  16.015 us |  47.221 us |   676.56 us |  1.40 |    0.10 |    2 |
| .net core 2.2 | Core    | RunParallel | 10        |   484.16 us |  10.871 us |  22.691 us |   477.14 us |  1.00 |    0.00 |    1 |
|               |         |             |           |             |            |            |             |       |         |      |
| .net 4.7.2    | Clr     | RunParallel | 100       | 6,039.49 us | 136.269 us | 133.834 us | 5,997.19 us |  1.33 |    0.03 |    2 |
| .net core 2.2 | Core    | RunParallel | 100       | 4,528.50 us |  49.150 us |  45.975 us | 4,518.32 us |  1.00 |    0.00 |    1 |
```
