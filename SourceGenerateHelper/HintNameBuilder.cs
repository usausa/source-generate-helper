namespace SourceGenerateHelper;

using System.Text;

using Microsoft.CodeAnalysis;

public static class HintNameBuilder
{
    public const string DefaultExtension = ".g.cs";

    public static string Build(string? ns, params string[] parts) =>
        BuildWithExtension(ns, DefaultExtension, parts);

    public static string BuildFromType(INamedTypeSymbol type, params string[] parts) =>
        BuildFromTypeWithExtension(type, DefaultExtension, parts);

    public static string BuildFromTypeWithExtension(INamedTypeSymbol type, string extension, params string[] parts)
    {
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } containing ? containing.ToDisplayString() : null;
        return BuildWithExtension(ns, extension, [GetTypePart(type), .. parts]);
    }

    public static string BuildWithExtension(string? ns, string extension, params string[] parts)
    {
        var buffer = new StringBuilder();
        var first = true;

        if (!String.IsNullOrEmpty(ns))
        {
            AppendEscaped(buffer, ns!);
            first = false;
        }

        foreach (var part in parts)
        {
            if (String.IsNullOrEmpty(part))
            {
                continue;
            }

            if (!first)
            {
                buffer.Append('_');
            }

            AppendEscaped(buffer, part);
            first = false;
        }

        buffer.Append(extension);

        return buffer.ToString();
    }

    private static string GetTypePart(INamedTypeSymbol type) =>
        type.ContainingType is null ? type.MetadataName : GetTypePart(type.ContainingType) + "+" + type.MetadataName;

    private static void AppendEscaped(StringBuilder buffer, string value)
    {
        foreach (var c in value)
        {
            // The @ of a keyword is not taken in a hint name (ArgumentException)
            if (c == '@')
            {
                continue;
            }

            buffer.Append(c switch
            {
                '.' => '_',
                '_' => '-',
                '<' => '[',
                '>' => ']',
                _ => c
            });
        }
    }
}
