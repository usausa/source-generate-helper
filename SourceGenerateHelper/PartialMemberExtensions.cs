namespace SourceGenerateHelper;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class PartialMemberExtensions
{
    public static string GetImplementationSignature(this IMethodSymbol method, MethodDeclarationSyntax definition)
    {
        var builder = new StringBuilder();
        AppendModifiers(builder, definition.Modifiers);
        AppendRefKind(builder, method.RefKind);
        builder.Append(method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable));
        builder.Append(' ').Append(CSharpIdentifier.Escape(method.Name));

        if (method.TypeParameters.Length > 0)
        {
            builder.Append('<');
            for (var i = 0; i < method.TypeParameters.Length; i++)
            {
                builder.Append(i > 0 ? ", " : string.Empty).Append(CSharpIdentifier.EscapeTypeName(method.TypeParameters[i].Name));
            }

            builder.Append('>');
        }

        builder.Append('(');
        AppendParameters(builder, method.Parameters, definition.ParameterList.Parameters);
        builder.Append(')');

        if (method.IsOverride || (method.ExplicitInterfaceImplementations.Length > 0))
        {
            foreach (var clause in definition.ConstraintClauses)
            {
                builder.Append(' ').Append(clause);
            }
        }
        else
        {
            foreach (var typeParameter in method.TypeParameters)
            {
                AppendConstraintClause(builder, typeParameter);
            }
        }

        return builder.ToString();
    }

    public static string GetImplementationSignature(this IPropertySymbol property, BasePropertyDeclarationSyntax definition)
    {
        var builder = new StringBuilder();
        AppendModifiers(builder, definition.Modifiers);
        AppendRefKind(builder, property.RefKind);
        builder.Append(property.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable)).Append(' ');

        if (definition is IndexerDeclarationSyntax indexer)
        {
            builder.Append("this[");
            AppendParameters(builder, property.Parameters, indexer.ParameterList.Parameters);
            builder.Append(']');
        }
        else
        {
            builder.Append(CSharpIdentifier.Escape(property.Name));
        }

        return builder.ToString();
    }

    public static string GetImplementationAccessor(this AccessorDeclarationSyntax accessor)
    {
        var builder = new StringBuilder();
        AppendModifiers(builder, accessor.Modifiers);
        return builder.Append(accessor.Keyword.ValueText).ToString();
    }

    private static void AppendModifiers(StringBuilder builder, SyntaxTokenList modifiers)
    {
        foreach (var modifier in modifiers)
        {
            builder.Append(modifier.ValueText).Append(' ');
        }
    }

    private static void AppendRefKind(StringBuilder builder, RefKind refKind)
    {
        if (refKind == RefKind.Ref)
        {
            builder.Append("ref ");
        }
        else if (refKind == RefKind.RefReadOnly)
        {
            builder.Append("ref readonly ");
        }
    }

    private static void AppendParameters(StringBuilder builder, ImmutableArray<IParameterSymbol> parameters, SeparatedSyntaxList<ParameterSyntax> definitions)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            if (i < definitions.Count)
            {
                AppendModifiers(builder, definitions[i].Modifiers);
            }

            builder.Append(parameters[i].Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable))
                .Append(' ')
                .Append(CSharpIdentifier.Escape(parameters[i].Name));
        }
    }

    private static void AppendConstraintClause(StringBuilder builder, ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<string>();
        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        }
        else if (typeParameter.HasUnmanagedTypeConstraint)
        {
            constraints.Add("unmanaged");
        }
        else if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add("struct");
        }
        else if (typeParameter.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        for (var i = 0; i < typeParameter.ConstraintTypes.Length; i++)
        {
            constraints.Add(typeParameter.ConstraintTypes[i]
                .WithNullableAnnotation(typeParameter.ConstraintNullableAnnotations[i])
                .ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable));
        }

        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        if (typeParameter.AllowsRefLikeType)
        {
            constraints.Add("allows ref struct");
        }

        if (constraints.Count > 0)
        {
            builder.Append(" where ").Append(CSharpIdentifier.EscapeTypeName(typeParameter.Name)).Append(" : ").Append(String.Join(", ", constraints));
        }
    }
}
