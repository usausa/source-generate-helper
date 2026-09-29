namespace SourceGenerateHelper.Testing;

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

public sealed class IncrementalRunResult
{
    public GeneratorDriverRunResult FirstResult { get; }

    public GeneratorDriverRunResult SecondResult { get; }

    public string FirstGeneratedText { get; }

    public string SecondGeneratedText { get; }

    public IReadOnlyList<IncrementalStepRunReason> OutputReasons { get; }

    internal IncrementalRunResult(
        GeneratorDriverRunResult firstResult,
        GeneratorDriverRunResult secondResult,
        string firstGeneratedText,
        string secondGeneratedText,
        IReadOnlyList<IncrementalStepRunReason> outputReasons)
    {
        FirstResult = firstResult;
        SecondResult = secondResult;
        FirstGeneratedText = firstGeneratedText;
        SecondGeneratedText = secondGeneratedText;
        OutputReasons = outputReasons;
    }

    public IReadOnlyList<IncrementalStepRunReason> StepReasons(string stepName) =>
        SecondResult.Results
            .SelectMany(x => x.TrackedSteps.TryGetValue(stepName, out var steps) ? steps : [])
            .SelectMany(static x => x.Outputs)
            .Select(static x => x.Reason)
            .ToList();
}
