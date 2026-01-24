using BenchmarkDotNet.Running;
using EFCore.FluentIncludes.Benchmarks;

if (args.Contains("--ci"))
{
    // CI mode: run benchmarks with CI config and check thresholds
    var summary = BenchmarkRunner.Run<IncludeBenchmarks>(new CiBenchmarkConfig());

    // Find the JSON results file
    var jsonFiles = Directory.GetFiles(summary.ResultsDirectoryPath, "*-report-full.json");
    if (jsonFiles.Length == 0)
    {
        Console.WriteLine("Error: No benchmark results JSON file found.");
        return 1;
    }

    var result = ThresholdChecker.CheckResults(jsonFiles[0]);
    ThresholdChecker.PrintResults(result);

    // Write markdown to file for GitHub Actions
    var markdownPath = Path.Combine(summary.ResultsDirectoryPath, "benchmark-results.md");
    File.WriteAllText(markdownPath, result.MarkdownTable);
    Console.WriteLine($"\nMarkdown results written to: {markdownPath}");

    // Set output for GitHub Actions
    var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    if (!string.IsNullOrEmpty(githubOutput))
    {
        File.AppendAllText(githubOutput, $"passed={result.AllPassed.ToString().ToLowerInvariant()}\n");
        File.AppendAllText(githubOutput, $"results-path={markdownPath}\n");
    }

    return result.AllPassed ? 0 : 1;
}
else
{
    // Interactive mode: use BenchmarkSwitcher for flexibility
    BenchmarkSwitcher.FromAssembly(typeof(IncludeBenchmarks).Assembly).Run(args);
    return 0;
}
