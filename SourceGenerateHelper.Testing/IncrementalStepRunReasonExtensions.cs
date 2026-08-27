namespace SourceGenerateHelper.Testing;

using Microsoft.CodeAnalysis;

public static class IncrementalStepRunReasonExtensions
{
    public static bool IsChanged(this IncrementalStepRunReason reason) =>
        reason is not (IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);
}
