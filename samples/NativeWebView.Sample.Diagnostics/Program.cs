using System.Text.Json;
using System.Text.Json.Serialization;
using NativeWebView.Core;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.Linux;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.iOS;
using NativeWebView.Platform.macOS;

try
{
    var options = ParseArguments(args);

    var factory = new NativeWebViewBackendFactory()
        .UseNativeWebViewWindows()
        .UseNativeWebViewMacOS()
        .UseNativeWebViewLinux()
        .UseNativeWebViewIOS()
        .UseNativeWebViewAndroid()
        .UseNativeWebViewBrowser();

    var report = NativeWebViewDiagnosticsReporter.CreateReport(
        factory,
        options.Platforms,
        options.WarningsAsErrors);

    var outputPath = Path.GetFullPath(options.OutputPath);
    await WriteReportAsync(report, outputPath);

    if (!string.IsNullOrWhiteSpace(options.MarkdownOutputPath))
    {
        var markdownOutputPath = Path.GetFullPath(options.MarkdownOutputPath);
        await WriteTextFileAsync(
            markdownOutputPath,
            NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(report));
        Console.WriteLine($"Diagnostics markdown summary written to: {markdownOutputPath}");
    }

    var currentBlockingIssues = NativeWebViewDiagnosticsRegressionAnalyzer.GetBlockingIssues(
        report,
        warningsAsErrors: options.WarningsAsErrors);

    if (!string.IsNullOrWhiteSpace(options.BlockingBaselineOutputPath))
    {
        var blockingBaselineOutputPath = Path.GetFullPath(options.BlockingBaselineOutputPath);
        var baselineText = NativeWebViewDiagnosticsRegressionAnalyzer.SerializeBaselineLines(currentBlockingIssues);
        await WriteTextFileAsync(blockingBaselineOutputPath, baselineText);
        Console.WriteLine($"Blocking diagnostics baseline written to: {blockingBaselineOutputPath}");
    }

    NativeWebViewDiagnosticsRegressionResult? regression = null;
    if (!string.IsNullOrWhiteSpace(options.BlockingBaselinePath))
    {
        var baselinePath = Path.GetFullPath(options.BlockingBaselinePath);
        var baselineLines = await File.ReadAllLinesAsync(baselinePath);
        var baselineIssues = NativeWebViewDiagnosticsRegressionAnalyzer.ParseBaselineLines(baselineLines);

        regression = NativeWebViewDiagnosticsRegressionAnalyzer.CompareBlockingIssues(
            baselineBlockingIssues: baselineIssues,
            currentBlockingIssues);

        PrintRegressionSummary(regression, baselinePath);

        if (!string.IsNullOrWhiteSpace(options.ComparisonMarkdownOutputPath))
        {
            var comparisonMarkdownOutputPath = Path.GetFullPath(options.ComparisonMarkdownOutputPath);
            var comparisonMarkdown = NativeWebViewDiagnosticsRegressionMarkdownFormatter.Format(regression);
            await WriteTextFileAsync(comparisonMarkdownOutputPath, comparisonMarkdown);
            Console.WriteLine($"Blocking diagnostics regression markdown written to: {comparisonMarkdownOutputPath}");
        }
    }

    PrintSummary(report, outputPath);

    var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
        report,
        regression,
        requireReady: options.RequireReady,
        failOnRegression: options.FailOnRegression,
        requireBaselineSync: options.RequireBaselineSync);

    if (!string.IsNullOrWhiteSpace(options.ComparisonJsonOutputPath))
    {
        var comparisonJsonOutputPath = Path.GetFullPath(options.ComparisonJsonOutputPath);
        await WriteJsonFileAsync(evaluation, comparisonJsonOutputPath);
        Console.WriteLine($"Blocking diagnostics regression evaluation JSON written to: {comparisonJsonOutputPath}");
    }

    if (!string.IsNullOrWhiteSpace(options.ComparisonEvaluationMarkdownOutputPath))
    {
        var comparisonEvaluationMarkdownOutputPath = Path.GetFullPath(options.ComparisonEvaluationMarkdownOutputPath);
        var comparisonEvaluationMarkdown = NativeWebViewDiagnosticsRegressionEvaluationMarkdownFormatter.Format(evaluation);
        await WriteTextFileAsync(comparisonEvaluationMarkdownOutputPath, comparisonEvaluationMarkdown);
        Console.WriteLine($"Blocking diagnostics gate evaluation markdown written to: {comparisonEvaluationMarkdownOutputPath}");
    }

    Console.WriteLine(
        $"Diagnostics evaluation summary: effectiveExitCode={evaluation.EffectiveExitCode}, fingerprintVersion={evaluation.FingerprintVersion}, fingerprint={evaluation.Fingerprint}");

    if (evaluation.FailingGates.Count > 0)
    {
        PrintGateFailures(evaluation);
        Console.Error.WriteLine($"Diagnostics evaluation failed with exit code {evaluation.EffectiveExitCode}.");
        return evaluation.EffectiveExitCode;
    }

    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(ex.Message);
    PrintUsage();
    return 2;
}

static async Task WriteReportAsync(NativeWebViewDiagnosticsReport report, string outputPath)
{
    await WriteJsonFileAsync(report, outputPath);
}

static async Task WriteJsonFileAsync<TValue>(TValue value, string outputPath)
{
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    options.Converters.Add(new JsonStringEnumConverter());

    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    await using var stream = File.Create(outputPath);
    await JsonSerializer.SerializeAsync(stream, value, options);
}

static async Task WriteTextFileAsync(string outputPath, string content)
{
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    await File.WriteAllTextAsync(outputPath, content);
}

static void PrintSummary(NativeWebViewDiagnosticsReport report, string outputPath)
{
    Console.WriteLine($"Diagnostics report written to: {outputPath}");
    Console.WriteLine(
        $"Summary: ready={report.IsReady}, warningsAsErrors={report.WarningsAsErrors}, platforms={report.Platforms.Count}, issues={report.IssueCount}, blocking={report.BlockingIssueCount}");

    foreach (var platform in report.Platforms)
    {
        Console.WriteLine(
            $"- {platform.Platform}: ready={platform.IsReady}, provider={platform.ProviderName}, registered={platform.ProviderRegistered}, warnings={platform.WarningCount}, errors={platform.ErrorCount}, blocking={platform.BlockingIssueCount}");

        foreach (var issue in platform.Issues)
        {
            var recommendation = string.IsNullOrWhiteSpace(issue.Recommendation)
                ? string.Empty
                : $" Recommendation: {issue.Recommendation}";
            Console.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}{recommendation}");
        }
    }
}

static void PrintRegressionSummary(NativeWebViewDiagnosticsRegressionResult regression, string baselinePath)
{
    Console.WriteLine($"Blocking diagnostics baseline: {baselinePath}");
    Console.WriteLine(
        $"Blocking regression summary: baseline={regression.BaselineBlockingIssues.Count}, current={regression.CurrentBlockingIssues.Count}, new={regression.NewBlockingIssues.Count}, resolved={regression.ResolvedBlockingIssues.Count}, hasRegression={regression.HasRegression}, hasStaleBaseline={regression.HasStaleBaseline}, requiresBaselineUpdate={regression.RequiresBaselineUpdate}");

    foreach (var issue in regression.NewBlockingIssues)
    {
        Console.WriteLine($"  [NEW] {issue.Platform}|{issue.Code}");
    }

    foreach (var issue in regression.ResolvedBlockingIssues)
    {
        Console.WriteLine($"  [RESOLVED] {issue.Platform}|{issue.Code}");
    }
}

static void PrintGateFailures(NativeWebViewDiagnosticsRegressionEvaluation evaluation)
{
    foreach (var gateFailure in evaluation.GateFailures)
    {
        Console.Error.WriteLine(gateFailure.Message);
        Console.Error.WriteLine($"Recommendation: {gateFailure.Recommendation}");
    }
}

static RunOptions ParseArguments(string[] args)
{
    var outputPath = "artifacts/diagnostics/platform-diagnostics-report.json";
    string? markdownOutputPath = null;
    string? blockingBaselinePath = null;
    string? blockingBaselineOutputPath = null;
    string? comparisonMarkdownOutputPath = null;
    string? comparisonJsonOutputPath = null;
    string? comparisonEvaluationMarkdownOutputPath = null;
    var platforms = NativeWebViewDiagnosticsReporter.GetDefaultPlatforms();
    var requireReady = false;
    var warningsAsErrors = false;
    var failOnRegression = true;
    var requireBaselineSync = false;

    for (var index = 0; index < args.Length; index++)
    {
        var argument = args[index];
        switch (argument)
        {
            case "--output":
                outputPath = GetRequiredValue(args, ref index, "--output");
                break;
            case "--platform":
                platforms = ParsePlatforms(GetRequiredValue(args, ref index, "--platform"));
                break;
            case "--markdown-output":
                markdownOutputPath = GetRequiredValue(args, ref index, "--markdown-output");
                break;
            case "--blocking-baseline":
                blockingBaselinePath = GetRequiredValue(args, ref index, "--blocking-baseline");
                break;
            case "--blocking-baseline-output":
                blockingBaselineOutputPath = GetRequiredValue(args, ref index, "--blocking-baseline-output");
                break;
            case "--comparison-markdown-output":
                comparisonMarkdownOutputPath = GetRequiredValue(args, ref index, "--comparison-markdown-output");
                break;
            case "--comparison-json-output":
                comparisonJsonOutputPath = GetRequiredValue(args, ref index, "--comparison-json-output");
                break;
            case "--comparison-evaluation-markdown-output":
                comparisonEvaluationMarkdownOutputPath = GetRequiredValue(args, ref index, "--comparison-evaluation-markdown-output");
                break;
            case "--require-ready":
                requireReady = true;
                break;
            case "--warnings-as-errors":
                warningsAsErrors = true;
                break;
            case "--allow-regression":
                failOnRegression = false;
                break;
            case "--require-baseline-sync":
                requireBaselineSync = true;
                break;
            case "--help":
            case "-h":
                PrintUsage();
                Environment.Exit(0);
                break;
            default:
                throw new ArgumentException($"Unknown argument: {argument}");
        }
    }

    if (platforms.Count == 0)
    {
        throw new ArgumentException("At least one platform must be specified.");
    }

    if (!string.IsNullOrWhiteSpace(comparisonMarkdownOutputPath) &&
        string.IsNullOrWhiteSpace(blockingBaselinePath))
    {
        throw new ArgumentException("--comparison-markdown-output requires --blocking-baseline.");
    }

    if (!string.IsNullOrWhiteSpace(comparisonJsonOutputPath) &&
        string.IsNullOrWhiteSpace(blockingBaselinePath))
    {
        throw new ArgumentException("--comparison-json-output requires --blocking-baseline.");
    }

    if (requireBaselineSync && string.IsNullOrWhiteSpace(blockingBaselinePath))
    {
        throw new ArgumentException("--require-baseline-sync requires --blocking-baseline.");
    }

    return new RunOptions(
        outputPath,
        markdownOutputPath,
        blockingBaselinePath,
        blockingBaselineOutputPath,
        comparisonMarkdownOutputPath,
        comparisonJsonOutputPath,
        comparisonEvaluationMarkdownOutputPath,
        platforms,
        requireReady,
        warningsAsErrors,
        failOnRegression,
        requireBaselineSync);
}

static IReadOnlyList<NativeWebViewPlatform> ParsePlatforms(string value)
{
    if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
    {
        return NativeWebViewDiagnosticsReporter.GetDefaultPlatforms();
    }

    var tokens = value.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    var resolved = new List<NativeWebViewPlatform>(tokens.Length);

    foreach (var token in tokens)
    {
        resolved.Add(ParsePlatform(token));
    }

    return resolved;
}

static NativeWebViewPlatform ParsePlatform(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "windows" => NativeWebViewPlatform.Windows,
        "macos" => NativeWebViewPlatform.MacOS,
        "linux" => NativeWebViewPlatform.Linux,
        "ios" => NativeWebViewPlatform.IOS,
        "android" => NativeWebViewPlatform.Android,
        "browser" => NativeWebViewPlatform.Browser,
        _ => throw new ArgumentException(
            $"Unsupported platform '{value}'. Use all, windows, macos, linux, ios, android, browser."),
    };
}

static string GetRequiredValue(string[] args, ref int index, string option)
{
    var nextIndex = index + 1;
    if (nextIndex >= args.Length)
    {
        throw new ArgumentException($"Missing value for {option}.");
    }

    index = nextIndex;
    return args[index];
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run --project samples/NativeWebView.Sample.Diagnostics -- [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  --output <path>                     Report output path (default artifacts/diagnostics/platform-diagnostics-report.json)");
    Console.WriteLine("  --markdown-output <path>            Optional diagnostics markdown summary path.");
    Console.WriteLine("  --blocking-baseline <path>          Optional baseline file for blocking issue regression checks.");
    Console.WriteLine("  --blocking-baseline-output <path>   Optional output path for writing current blocking baseline entries.");
    Console.WriteLine("  --comparison-markdown-output <path> Optional markdown output path for baseline comparison summary.");
    Console.WriteLine("  --comparison-json-output <path>     Optional JSON output path for baseline comparison evaluation summary.");
    Console.WriteLine("  --comparison-evaluation-markdown-output <path> Optional markdown output path for gate evaluation summary.");
    Console.WriteLine("  --platform <value>                  all | windows | macos | linux | ios | android | browser");
    Console.WriteLine("                                      Use comma-separated values for multiple platforms.");
    Console.WriteLine("  --require-ready                     Enable readiness gate (failing gate exit code applies).");
    Console.WriteLine("  --warnings-as-errors                Treat warnings as blocking in readiness calculations.");
    Console.WriteLine("  --allow-regression                  Do not fail when baseline comparison finds new blocking issues.");
    Console.WriteLine("  --require-baseline-sync             Enable baseline-sync gate for resolved/stale baseline entries.");
    Console.WriteLine("  Exit codes: 0=pass, 10=require-ready, 11=regression, 12=baseline-sync, 13=multiple gates.");
    Console.WriteLine("  --help|-h                           Show this help.");
}

internal sealed record RunOptions(
    string OutputPath,
    string? MarkdownOutputPath,
    string? BlockingBaselinePath,
    string? BlockingBaselineOutputPath,
    string? ComparisonMarkdownOutputPath,
    string? ComparisonJsonOutputPath,
    string? ComparisonEvaluationMarkdownOutputPath,
    IReadOnlyList<NativeWebViewPlatform> Platforms,
    bool RequireReady,
    bool WarningsAsErrors,
    bool FailOnRegression,
    bool RequireBaselineSync);
