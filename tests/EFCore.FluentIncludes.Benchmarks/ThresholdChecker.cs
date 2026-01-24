using System.Text.Json;

namespace EFCore.FluentIncludes.Benchmarks;

/// <summary>
/// Checks benchmark results against performance thresholds.
/// </summary>
public static class ThresholdChecker
{
    /// <summary>
    /// Maximum allowed overhead ratio (e.g., 1.5 = 50% slower than standard EF Core).
    /// </summary>
    public const double MaxOverheadRatio = 1.5;

    public record BenchmarkComparison(
        string Scenario,
        double StandardMeanNs,
        double FluentIncludesMeanNs,
        double Ratio,
        bool Passed);

    public record ThresholdResult(
        List<BenchmarkComparison> Comparisons,
        bool AllPassed,
        string MarkdownTable);

    public static ThresholdResult CheckResults(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var benchmarks = doc.RootElement.GetProperty("Benchmarks");

        var results = new Dictionary<string, double>();
        foreach (var benchmark in benchmarks.EnumerateArray())
        {
            var method = benchmark.GetProperty("Method").GetString()!;
            var stats = benchmark.GetProperty("Statistics");
            var meanNs = stats.GetProperty("Mean").GetDouble();
            results[method] = meanNs;
        }

        var comparisons = new List<BenchmarkComparison>();
        var scenarios = new[] { "SingleNavigation", "TwoLevelNavigation", "DeepNavigation", "MultiplePaths", "ComplexScenario" };

        foreach (var scenario in scenarios)
        {
            var standardKey = $"Standard_{scenario}";
            var fluentKey = $"FluentIncludes_{scenario}";

            if (results.TryGetValue(standardKey, out var standardMean) &&
                results.TryGetValue(fluentKey, out var fluentMean))
            {
                var ratio = fluentMean / standardMean;
                var passed = ratio <= MaxOverheadRatio;
                comparisons.Add(new BenchmarkComparison(scenario, standardMean, fluentMean, ratio, passed));
            }
        }

        var allPassed = comparisons.All(c => c.Passed);
        var markdown = GenerateMarkdownTable(comparisons);

        return new ThresholdResult(comparisons, allPassed, markdown);
    }

    private static string GenerateMarkdownTable(List<BenchmarkComparison> comparisons)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| Scenario | Standard EF | FluentIncludes | Overhead | Status |");
        sb.AppendLine("|----------|-------------|----------------|----------|--------|");

        foreach (var c in comparisons)
        {
            var standardUs = c.StandardMeanNs / 1000;
            var fluentUs = c.FluentIncludesMeanNs / 1000;
            var overheadPct = (c.Ratio - 1) * 100;
            var status = c.Passed ? "pass" : "FAIL";

            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"| {FormatScenario(c.Scenario)} | {standardUs:F2} us | {fluentUs:F2} us | +{overheadPct:F0}% | {status} |");
        }

        sb.AppendLine();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"_Threshold: {(MaxOverheadRatio - 1) * 100:F0}% maximum overhead vs standard EF Core_");

        return sb.ToString();
    }

    private static string FormatScenario(string scenario)
    {
        return scenario switch
        {
            "SingleNavigation" => "Single navigation",
            "TwoLevelNavigation" => "Two-level navigation",
            "DeepNavigation" => "Deep navigation (4 levels)",
            "MultiplePaths" => "Multiple paths",
            "ComplexScenario" => "Complex scenario",
            _ => scenario
        };
    }

    public static void PrintResults(ThresholdResult result)
    {
        Console.WriteLine();
        Console.WriteLine("## Benchmark Results");
        Console.WriteLine();
        Console.WriteLine(result.MarkdownTable);
        Console.WriteLine();

        if (result.AllPassed)
        {
            Console.WriteLine("✅ All benchmarks passed threshold check!");
        }
        else
        {
            Console.WriteLine("❌ Some benchmarks exceeded the threshold!");
            foreach (var failed in result.Comparisons.Where(c => !c.Passed))
            {
                Console.WriteLine($"   - {failed.Scenario}: {failed.Ratio:F2}x (max allowed: {MaxOverheadRatio}x)");
            }
        }
    }
}
