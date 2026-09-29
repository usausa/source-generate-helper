namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis.CSharp;

public static class CSharpIdentifier
{
    public static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    public static string EscapeTypeName(string name) =>
        SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None ? Escape(name) : "@" + name;

    public static string EscapeQualifiedName(string name) =>
        name.IndexOf('.') < 0 ? Escape(name) : String.Join(".", name.Split('.').Select(Escape));

    public static bool IsValid(string name) =>
        SyntaxFacts.IsValidIdentifier(name);
}
