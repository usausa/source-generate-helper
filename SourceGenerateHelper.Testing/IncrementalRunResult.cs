namespace SourceGenerateHelper.Testing;

using System.Collections.Generic;

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
}
