namespace SourceGeneratorHelper;

using Microsoft.CodeAnalysis;

public static class RoslynExtensions
{
    // ------------------------------------------------------------
    // Accessibility
    // ------------------------------------------------------------

    public static string ToAccessibilityText(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Protected => "protected",
        Accessibility.Private => "private",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => throw new NotSupportedException()
    };
}
