namespace SourceGenerateHelper.Testing;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

public sealed class GeneratorTestResult
{
    public const string UnformattedMessageId = "SGHT0001";

    private static readonly string[] GeneratorFailureIds = ["CS8784", "CS8785"];

    internal GeneratorTestResult(
        GeneratorDriverRunResult driverResult,
        GeneratorDriver driver,
        Compilation outputCompilation,
        IReadOnlyDictionary<string, string> generatedSources,
        string allGeneratedText,
        IReadOnlyList<Diagnostic> compilationErrors)
    {
        DriverResult = driverResult;
        Driver = driver;
        OutputCompilation = outputCompilation;
        GeneratedSources = generatedSources;
        AllGeneratedText = allGeneratedText;
        CompilationErrors = compilationErrors;
    }

    public GeneratorDriverRunResult DriverResult { get; }

    public GeneratorDriver Driver { get; }

    public Compilation OutputCompilation { get; }

    public IReadOnlyDictionary<string, string> GeneratedSources { get; }

    public string AllGeneratedText { get; }

    public IReadOnlyList<Diagnostic> CompilationErrors { get; }

    public IReadOnlyList<Diagnostic> GeneratorDiagnostics => DriverResult.Diagnostics;

    public IReadOnlyList<Exception> GeneratorExceptions =>
        DriverResult.Results.Where(static x => x.Exception is not null).Select(static x => x.Exception!).ToArray();

    public IReadOnlyList<Diagnostic> Problems => field ??= CollectProblems();

    public string FirstGeneratedSource =>
        GeneratedSources.Count > 0 ? GeneratedSources.Values.First() : string.Empty;

    public string GeneratedSource(string hintName) =>
        GeneratedSources.TryGetValue(hintName, out var text) ? text : string.Empty;

    public string? FindGeneratedSource(string hintName) =>
        GeneratedSources.GetValueOrDefault(hintName);

    public IReadOnlyList<Diagnostic> Diagnostics(IReadOnlyList<string> prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);

        if (prefixes.Count == 0)
        {
            return GeneratorDiagnostics.ToArray();
        }

        return GeneratorDiagnostics.Where(x => IsGeneratorFailure(x) || prefixes.Any(prefix => x.Id.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
    }

    private static bool IsGeneratorFailure(Diagnostic diagnostic) =>
        GeneratorFailureIds.Contains(diagnostic.Id, StringComparer.Ordinal);

    private static bool HasPlaceholder(string format)
    {
        for (var i = 0; i < format.Length - 1; i++)
        {
            if (format[i] != '{')
            {
                continue;
            }

            if (format[i + 1] == '{')
            {
                i++;
            }
            else if (Char.IsAsciiDigit(format[i + 1]))
            {
                return true;
            }
        }

        return false;
    }

    private List<Diagnostic> CollectProblems()
    {
        var list = new List<Diagnostic>();
        foreach (var diagnostic in GeneratorDiagnostics)
        {
            if (diagnostic.IsSuppressed || (diagnostic.Severity == DiagnosticSeverity.Hidden))
            {
                continue;
            }

            list.Add(diagnostic);

            var format = diagnostic.Descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture);
            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            if (HasPlaceholder(format) && String.Equals(format, message, StringComparison.Ordinal))
            {
                list.Add(Diagnostic.Create(
                    UnformattedMessageId,
                    "Testing",
                    $"The message of {diagnostic.Id} is not formatted, as the arguments do not match its format: {message}",
                    DiagnosticSeverity.Error,
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    location: diagnostic.Location));
            }
        }

        var generatedPaths = new HashSet<string>(DriverResult.GeneratedTrees.Select(static x => x.FilePath), StringComparer.Ordinal);
        foreach (var diagnostic in OutputCompilation.GetDiagnostics())
        {
            if ((diagnostic.Severity == DiagnosticSeverity.Error) ||
                ((diagnostic.Severity == DiagnosticSeverity.Warning) && (diagnostic.Location.SourceTree is { } tree) && generatedPaths.Contains(tree.FilePath)))
            {
                list.Add(diagnostic);
            }
        }

        return list;
    }
}
