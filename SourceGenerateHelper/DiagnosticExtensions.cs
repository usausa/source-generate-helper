namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticInfo info)
    {
        var messageArgs = info.MessageArg is { } arg ? new object[] { arg } : null;
        var diagnostic = Diagnostic.Create(info.Descriptor, info.Location?.ToLocation(), info.Properties, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }
}
