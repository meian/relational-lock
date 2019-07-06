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

|           Job | Runtime |       Method | LoopCount |        Mean |      Error |     StdDev | Ratio | RatioSD | Rank |
|-------------- |-------- |------------- |---------- |------------:|-----------:|-----------:|------:|--------:|-----:|
|    .net 4.7.2 |     Clr | RunSequence2 |         1 |    19.84 us |  0.3348 us |  0.2795 us |  1.26 |    0.03 |    2 |
| .net core 2.2 |    Core | RunSequence2 |         1 |    15.78 us |  0.3022 us |  0.2827 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |       |         |      |
|    .net 4.7.2 |     Clr | RunSequence5 |         1 |    21.03 us |  0.4140 us |  0.5938 us |  1.26 |    0.04 |    2 |
| .net core 2.2 |    Core | RunSequence5 |         1 |    16.74 us |  0.3262 us |  0.4572 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |       |         |      |
|    .net 4.7.2 |     Clr | RunSequence2 |        10 |   175.52 us |  3.5051 us |  5.4571 us |  1.25 |    0.05 |    2 |
| .net core 2.2 |    Core | RunSequence2 |        10 |   140.59 us |  2.6936 us |  3.1020 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |       |         |      |
|    .net 4.7.2 |     Clr | RunSequence5 |        10 |   188.93 us |  3.7730 us |  5.8741 us |  1.28 |    0.05 |    2 |
| .net core 2.2 |    Core | RunSequence5 |        10 |   143.74 us |  2.8643 us |  7.2905 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |       |         |      |
|    .net 4.7.2 |     Clr | RunSequence2 |       100 | 1,721.24 us | 31.8786 us | 29.8192 us |  1.32 |    0.03 |    2 |
| .net core 2.2 |    Core | RunSequence2 |       100 | 1,306.33 us | 25.0781 us | 27.8743 us |  1.00 |    0.00 |    1 |
|               |         |              |           |             |            |            |       |         |      |
|    .net 4.7.2 |     Clr | RunSequence5 |       100 | 1,809.40 us | 30.1135 us | 28.1682 us |  1.30 |    0.03 |    2 |
| .net core 2.2 |    Core | RunSequence5 |       100 | 1,390.87 us | 23.0622 us | 19.2580 us |  1.00 |    0.00 |    1 |
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

|           Job | Runtime |      Method | LoopCount |        Mean |      Error |    StdDev |      Median | Ratio | RatioSD | Rank |
|-------------- |-------- |------------ |---------- |------------:|-----------:|----------:|------------:|------:|--------:|-----:|
|    .net 4.7.2 |     Clr | RunParallel |         1 |    85.02 us |  1.6840 us |  3.696 us |    83.49 us |  1.83 |    0.14 |    2 |
| .net core 2.2 |    Core | RunParallel |         1 |    46.31 us |  0.9568 us |  2.521 us |    46.15 us |  1.00 |    0.00 |    1 |
|               |         |             |           |             |            |           |             |       |         |      |
|    .net 4.7.2 |     Clr | RunParallel |        10 |   364.34 us |  8.2421 us | 20.062 us |   357.49 us |  1.05 |    0.05 |    2 |
| .net core 2.2 |    Core | RunParallel |        10 |   348.62 us |  6.9635 us | 16.818 us |   341.88 us |  1.00 |    0.00 |    1 |
|               |         |             |           |             |            |           |             |       |         |      |
|    .net 4.7.2 |     Clr | RunParallel |       100 | 3,371.09 us | 78.7855 us | 93.789 us | 3,340.96 us |  1.21 |    0.04 |    2 |
| .net core 2.2 |    Core | RunParallel |       100 | 2,805.21 us | 33.5526 us | 29.744 us | 2,797.50 us |  1.00 |    0.00 |    1 |
```
