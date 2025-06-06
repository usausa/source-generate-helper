namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;

public static class SourceBuilderExtensions
{
    // ------------------------------------------------------------
    // Condition
    // ------------------------------------------------------------

    public static SourceBuilder AppendIf(this SourceBuilder builder, bool condition, string value) =>
        condition ? builder.Append(value) : builder;

    public static SourceBuilder AppendIf<T>(this SourceBuilder builder, bool condition, T? value) =>
        (condition && (value is not null)) ? builder.Append(value) : builder;

    public static SourceBuilder AppendFormatIf(this SourceBuilder builder, bool condition, string format, params object[] args) =>
        condition ? builder.AppendFormat(format, args) : builder;

    public static SourceBuilder AppendJoinIf<T>(this SourceBuilder builder, bool condition, IEnumerable<T> source, string separator) =>
        condition ? builder.AppendJoin(source, separator) : builder;

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    public static SourceBuilder BeginScope(this SourceBuilder builder)
    {
        builder.Indent().Append('{').NewLine();
        builder.IndentLevel++;
        return builder;
    }

    public static SourceBuilder EndScope(this SourceBuilder builder)
    {
        builder.IndentLevel--;
        builder.Indent().Append('}').NewLine();
        return builder;
    }

    public static SourceBuilder Accessibility(this SourceBuilder builder, Accessibility accessibility) =>
        builder.Append(accessibility.ToText());

    public static SourceBuilder Type(this SourceBuilder builder, ITypeSymbol type) =>
        builder.Append(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

    public static SourceBuilder Argument(this SourceBuilder builder, ITypeSymbol type, string name) =>
        builder.Type(type).Append(' ').Append(name);

    public static SourceBuilder Argument(this SourceBuilder builder, string type, string name) =>
        builder.Append(type).Append(' ').Append(name);

    public static SourceBuilder Arguments(this SourceBuilder builder, IEnumerable<(ITypeSymbol Type, string Name)> source)
    {
        foreach (var (type, name) in source)
        {
            builder.Type(type).Append(' ').Append(name).Append(", ");
        }
        builder.Length -= 2;

        return builder;
    }

    public static SourceBuilder Arguments(this SourceBuilder builder, IEnumerable<(string Type, string Name)> source)
    {
        foreach (var (type, name) in source)
        {
            builder.Append(type).Append(' ').Append(name).Append(", ");
        }
        builder.Length -= 2;

        return builder;
    }

    public static SourceBuilder Generics(this SourceBuilder builder, ITypeSymbol type) =>
        builder.Append('<').Type(type).Append('>');

    public static SourceBuilder Generics(this SourceBuilder builder, string type) =>
        builder.Append('<').Append(type).Append('>');

    public static SourceBuilder Generics(this SourceBuilder builder, params ITypeSymbol[] types)
    {
        builder.Append('<');
        foreach (var type in types)
        {
            builder.Type(type).Append(", ");
        }
        builder.Length -= 2;
        builder.Append('>');

        return builder;
    }

    public static SourceBuilder Generics(this SourceBuilder builder, params string[] types)
    {
        builder.Append('<');
        foreach (var type in types)
        {
            builder.Append(type).Append(", ");
        }
        builder.Length -= 2;
        builder.Append('>');

        return builder;
    }

    public static SourceBuilder Return(this SourceBuilder builder, string name) =>
        builder.Indent().Append("return ").Append(name).Append(';').NewLine();

    public static SourceBuilder YieldReturn(this SourceBuilder builder, string name) =>
        builder.Indent().Append("yield return ").Append(name).Append(';').NewLine();

    public static SourceBuilder Var(this SourceBuilder builder, string name, string? value = null) =>
        builder.Local("var", name, value);

    public static SourceBuilder Local(this SourceBuilder builder, string type, string name, string? value = null)
    {
        builder.Indent().Append(type).Append(' ').Append(name);
        if (value is not null)
        {
            builder.Append(" = ").Append(value);
        }
        builder.Append(';').NewLine();
        return builder;
    }

    public static SourceBuilder AutoGenerated(this SourceBuilder builder) =>
        builder.Indent().Append("// <auto-generated />").NewLine();

    public static SourceBuilder EnableNullable(this SourceBuilder builder) =>
        builder.Indent().Append("#nullable enable").NewLine();

    public static SourceBuilder Disable(this SourceBuilder builder, string id) =>
        builder.Indent().Append("#pragma warning disable ").Append(id).NewLine();

    public static SourceBuilder Namespace(this SourceBuilder builder, string ns) =>
        builder.Indent().Append("namespace ").Append(ns).Append(';').NewLine();

    public static SourceBuilder Using(this SourceBuilder builder, string ns) =>
        builder.Indent().Append("using ").Append(ns).Append(';').NewLine();

    public static SourceBuilder Comment(this SourceBuilder builder, string comment) =>
        builder.Indent().Append("// ").Append(comment).NewLine();

    public static SourceBuilder If(this SourceBuilder builder, string condition) =>
        builder.Indent().Append("if (").Append(condition).Append(')').NewLine().BeginScope();

    public static SourceBuilder Else(this SourceBuilder builder) =>
        builder.EndScope().Indent().Append("else").NewLine().BeginScope();

    public static SourceBuilder ElseIf(this SourceBuilder builder, string condition) =>
        builder.EndScope().Indent().Append("else if (").Append(condition).Append(')').NewLine().BeginScope();

    public static SourceBuilder EndIf(this SourceBuilder builder) =>
        builder.EndScope();

    public static SourceBuilder For(this SourceBuilder builder, string initializer, string condition, string iterator) =>
        builder.Indent().Append("for (var ").Append(initializer).Append("; ").Append(condition).Append("; ").Append(iterator).Append(')').NewLine().BeginScope();

    public static SourceBuilder EndFor(this SourceBuilder builder) =>
        builder.EndScope();

    public static SourceBuilder ForEach(this SourceBuilder builder, string name, string source) =>
        builder.Indent().Append("foreach (var ").Append(name).Append(" in ").Append(source).Append(')').NewLine().BeginScope();

    public static SourceBuilder EndForEach(this SourceBuilder builder) =>
        builder.EndScope();

    public static SourceBuilder While(this SourceBuilder builder, string condition) =>
        builder.Indent().Append("while (").Append(condition).Append(')').NewLine().BeginScope();

    public static SourceBuilder EndWhile(this SourceBuilder builder) =>
        builder.EndScope();

    public static SourceBuilder Property(this SourceBuilder builder, ITypeSymbol type, string name, string? initializer = null)
    {
        builder.Indent().Append("public ").Type(type).Append(' ').Append(name).Append(" { get; set; }");
        if (initializer is not null)
        {
            builder.Append(" = ").Append(initializer).Append(';');
        }
        builder.NewLine();
        return builder;
    }

    public static SourceBuilder Property(this SourceBuilder builder, string type, string name, string? initializer = null)
    {
        builder.Indent().Append("public ").Append(type).Append(' ').Append(name).Append(" { get; set; }");
        if (initializer is not null)
        {
            builder.Append(" = ").Append(initializer);
        }
        builder.Append(';').NewLine();
        return builder;
    }

    public static SourceBuilder Field(this SourceBuilder builder, ITypeSymbol type, string name, string? initializer = null)
    {
        builder.Indent().Append("private ").Type(type).Append(' ').Append(name);
        if (initializer is not null)
        {
            builder.Append(" = ").Append(initializer);
        }
        builder.Append(';').NewLine();
        return builder;
    }

    public static SourceBuilder Field(this SourceBuilder builder, string type, string name, string? initializer = null)
    {
        builder.Indent().Append("private ").Append(type).Append(' ').Append(name);
        if (initializer is not null)
        {
            builder.Append(" = ").Append(initializer);
        }
        builder.Append(';').NewLine();
        return builder;
    }
}
