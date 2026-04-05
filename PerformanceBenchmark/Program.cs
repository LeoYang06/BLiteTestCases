using System;
using BenchmarkDotNet.Running;

namespace PerformanceBenchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"Test time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("Basic Performance Comparison: LiteDB VS BLite");
        Console.WriteLine("==================================================");


        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        // Console.WriteLine("\n[1] Initiating Read Query Performance Benchmark (ReadBenchmark)...");
        // var readSummary = BenchmarkRunner.Run<ReadBenchmark>(BenchmarkConfig.Get());
        //
        // Console.WriteLine($"| Number of successfully executed use cases: {readSummary.BenchmarksCases.Length}");
        // Console.WriteLine($"| Total running time of the test engine: {readSummary.TotalTime.TotalMinutes:F2} minutes");

        //---------------------------------------------------------

        Console.WriteLine("\n[2] Running Pagination Benchmark (PaginationBenchmark)...");
        var paginationSummary = BenchmarkRunner.Run<PaginationBenchmark>(BenchmarkConfig.Get());

        Console.WriteLine($"| Benchmarks executed: {paginationSummary.BenchmarksCases.Length}");
        Console.WriteLine($"| Total engine time: {paginationSummary.TotalTime.TotalMinutes:F2} minutes");

        //---------------------------------------------------------

        // Console.WriteLine("\n[3] Initiating Read Query Performance Benchmark (ReadBenchmark_Cold)...");
        // var readSummary_Cold = BenchmarkRunner.Run<ReadBenchmark_Cold>(BenchmarkConfig.Get());

        //---------------------------------------------------------

        // Console.WriteLine("\n[4] Starting Bulk Write Performance Test (WriteBenchmark)...");
        // var writeSummary = BenchmarkRunner.Run<WriteBenchmark>(BenchmarkConfig.Get());

        Console.WriteLine("\nBenchmarks Are Complete! Please See Markdown Statistics Above.");
        Console.ReadLine();
    }
}