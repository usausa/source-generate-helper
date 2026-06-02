namespace SourceGenerateHelper;

using System.Linq;

using Microsoft.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticInfo info)
    {
        var messageArgs = info.MessageArgs.Count > 0 ? info.MessageArgs.AsArray().Cast<object>().ToArray() : null;
        var diagnostic = Diagnostic.Create(info.Descriptor, info.Location?.ToLocation(), info.Properties, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }
}
