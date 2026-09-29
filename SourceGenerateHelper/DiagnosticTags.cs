namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;

public static class DiagnosticTags
{
    // An error so tagged cannot be suppressed by #pragma, NoWarn, a severity setting or [SuppressMessage]
    public static readonly string[] NotSuppressible = [WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Compiler];
}
