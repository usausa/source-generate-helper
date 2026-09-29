namespace SourceGenerateHelper;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticInfo info)
    {
        var messageArgs = info.MessageArgs.Count > 0 ? info.MessageArgs.Cast<object>().ToArray() : null;
        var diagnostic = Diagnostic.Create(info.Descriptor, info.Location?.ToLocation(), info.Properties, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }

    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticInfo info, ImmutableArray<SyntaxTree> trees) =>
        context.ReportDiagnostic(info.ToDiagnostic(trees));

    public static void ReportDiagnostics(this SourceProductionContext context, IEnumerable<DiagnosticInfo> infos)
    {
        foreach (var info in infos)
        {
            context.ReportDiagnostic(info);
        }
    }

    public static void ReportDiagnostics(this SourceProductionContext context, IEnumerable<DiagnosticInfo> infos, ImmutableArray<SyntaxTree> trees)
    {
        foreach (var info in infos)
        {
            context.ReportDiagnostic(info.ToDiagnostic(trees));
        }
    }
}
