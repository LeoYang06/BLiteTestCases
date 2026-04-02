using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace PerformanceBenchmark;

public class BenchmarkConfig
{
    public static IConfig Get()
    {
#if DEBUG
        // Debug mode forces InProcess+ShortRun, only used to quickly verify whether the code can run
        return new DebugInProcessConfig()
            .AddJob(Job.ShortRun.WithRuntime(CoreRuntime.Core10_0).WithPlatform(Platform.X64))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend).WithTimeUnit(TimeUnit.Millisecond));
#else
        // Release mode is controlled entirely by [SimpleJob] on the base class
        return DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default)
           .AddExporter(CsvExporter.Default)
           .AddExporter(HtmlExporter.Default)
           .AddExporter(MarkdownExporter.GitHub)
           .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend).WithTimeUnit(TimeUnit.Millisecond));
#endif
    }
}