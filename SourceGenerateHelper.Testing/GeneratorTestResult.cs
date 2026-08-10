namespace SourceGenerateHelper.Testing;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

// The outcome of one generator run.
// The raw Roslyn objects (DriverResult / Driver / OutputCompilation) are exposed on purpose:
// anything the runner does not wrap can still be reached through the standard API.
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

    // Raw driver result. Use DriverResult.Results[i].TrackedSteps for incremental-cache assertions.
    public GeneratorDriverRunResult DriverResult { get; }

    // The driver after the run, ready to be driven again for a second incremental pass.
    public GeneratorDriver Driver { get; }

    public Compilation OutputCompilation { get; }

    // Hint name to generated text.
    public IReadOnlyDictionary<string, string> GeneratedSources { get; }

    // All generated sources concatenated. Convenient for substring assertions.
    public string AllGeneratedText { get; }

    // Errors from compiling the source together with the generated code.
    public IReadOnlyList<Diagnostic> CompilationErrors { get; }

    // Diagnostics reported by the generators themselves.
    public IReadOnlyList<Diagnostic> GeneratorDiagnostics => DriverResult.Diagnostics;

    // The first generated source, or an empty string when nothing was generated.
    public string FirstGeneratedSource =>
        GeneratedSources.Count > 0 ? GeneratedSources.Values.First() : String.Empty;

    public string GeneratedSource(string hintName) =>
        GeneratedSources.TryGetValue(hintName, out var text) ? text : String.Empty;

    // Null when the hint name was not generated. Use this instead of GeneratedSource when
    // "nothing was generated" is a distinct assertion from "generated but empty".
    public string? FindGeneratedSource(string hintName) =>
        GeneratedSources.TryGetValue(hintName, out var text) ? text : null;

    // Generator diagnostics filtered by ID prefix. An empty prefix list returns everything.
    public IReadOnlyList<Diagnostic> Diagnostics(IReadOnlyList<string> prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);

        if (prefixes.Count == 0)
        {
            return [.. GeneratorDiagnostics];
        }

        return [.. GeneratorDiagnostics.Where(x => prefixes.Any(prefix => x.Id.StartsWith(prefix, StringComparison.Ordinal)))];
    }
}
