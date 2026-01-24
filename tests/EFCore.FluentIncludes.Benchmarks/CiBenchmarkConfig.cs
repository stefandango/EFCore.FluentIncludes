using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace EFCore.FluentIncludes.Benchmarks;

/// <summary>
/// Benchmark configuration optimized for CI: faster execution with JSON export.
/// </summary>
public class CiBenchmarkConfig : ManualConfig
{
    public CiBenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithWarmupCount(2)
            .WithIterationCount(5)
            .WithId("CI"));

        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddExporter(JsonExporter.Full);

        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));
    }
}
