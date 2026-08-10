namespace SourceGenerateHelper;

using System.Text;

// Builds the hint name passed to AddSource.
// Every generator in the ecosystem hand-rolled the same shape:
//     "{Namespace}_{ContainingType}_{Type}_{Suffix}.g.cs"
// A hint name has to be a valid, unique file name, so the namespace separator and the generic
// type brackets are replaced rather than emitted as-is.
public static class HintNameBuilder
{
    public const string DefaultExtension = ".g.cs";

    // Joins the parts with '_' under the namespace and appends ".g.cs".
    // Empty parts are skipped, so an optional suffix can be passed unconditionally.
    public static string Build(string? ns, params string[] parts) =>
        BuildWithExtension(ns, DefaultExtension, parts);

    // Same as Build, for generators that need their own extension (".AspNetCore.g.cs" etc.).
    public static string BuildWithExtension(string? ns, string extension, params string[] parts)
    {
        var buffer = new StringBuilder();
        var first = true;

        // The namespace is just the first segment; treating it as one keeps the separator logic in
        // one place, so an empty part list cannot leave a trailing '_'.
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

    // '.' would read as a file extension and '<' '>' are not valid in a file name on Windows.
    private static void AppendEscaped(StringBuilder buffer, string value)
    {
        foreach (var c in value)
        {
            buffer.Append(c switch
            {
                '.' => '_',
                '<' => '[',
                '>' => ']',
                _ => c
            });
        }
    }
}
