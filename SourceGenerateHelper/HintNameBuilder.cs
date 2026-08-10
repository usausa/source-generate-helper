namespace SourceGenerateHelper;

using System.Text;

public static class HintNameBuilder
{
    public const string DefaultExtension = ".g.cs";

    public static string Build(string? ns, params string[] parts) =>
        BuildWithExtension(ns, DefaultExtension, parts);

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
