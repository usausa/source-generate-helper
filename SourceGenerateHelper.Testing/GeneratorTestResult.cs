namespace SourceGenerateHelper.Testing;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

public sealed class GeneratorTestResult
{
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

        return GeneratorDiagnostics.Where(x => prefixes.Any(prefix => x.Id.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
    }
}
