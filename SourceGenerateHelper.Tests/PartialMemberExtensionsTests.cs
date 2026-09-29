namespace SourceGenerateHelper.Tests;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed class PartialMemberExtensionsTests
{
    private const string Definitions =
        """
        #nullable enable
        using System;
        using System.Collections.Generic;

        namespace App.@event
        {
            public static partial class Methods
            {
                public static partial void Plain(int x);
                static partial void NoAccessibility(string s);
                public static partial string? Nullable(string? s, List<string?> list);
                public static partial ref int RefReturn(ref int x);
                public static partial ref readonly int RefReadOnlyReturn(in int x);
                public static partial void Modifiers(this string s, scoped ref int a, ref readonly int b, out int c, params int[] d);
                public static partial void ParamsCollection(params List<int> values);
                public static partial T Generic<T, U>(T value, U other) where T : class?, IComparable<T> where U : struct;
                public static partial void Unmanaged<T>() where T : unmanaged;
                public static partial void NotNull<T>() where T : notnull, new();
                public static partial void RefStruct<T>() where T : allows ref struct;
                public static partial void Keyword(int @event, string @class);
                public static partial (int A, string? B) Tuple((int X, int Y) value);
                public static unsafe partial void Unsafe(int* pointer);
                public static partial void Defaults(int x = 5, string? s = null);
                public static partial Dictionary<string, List<int?>>? Nested();
            }

            public class Base
            {
                public virtual void Virtual() { }
                public virtual void Generic<T>() where T : class { }
                public int Hidden() => 0;
            }

            public partial class Instance : Base
            {
                public override partial void Virtual();
                public override partial void Generic<T>() where T : class;
                public new partial int Hidden();
                public virtual partial void NewVirtual();
                public sealed override partial string ToString();
                internal protected partial void Protected();
            }

            public partial struct Struct
            {
                public readonly partial int ReadOnly();
            }

            public partial class Properties
            {
                public required partial string Required { get; set; }
                public partial string? Init { get; init; }
                public partial int PrivateSet { get; private set; }
                public virtual partial int Virtual { get; set; }
                public static partial int Static { get; set; }
                public partial int this[int index] { get; set; }
                public partial string @class { get; set; }
                public partial ref int RefProperty { get; }
            }

            public partial struct StructProperties
            {
                public partial int ReadOnlyGet { readonly get; set; }
            }

            public partial class Outer<T> where T : new()
            {
                public partial class Inner
                {
                    public partial void Method(T value);
                }
            }

            public partial record Record
            {
                public partial void Method();
            }

            public partial record struct RecordStruct
            {
                public partial void Method();
            }

            public partial class @record
            {
                public partial void Method();
            }
        }
        """;

    private static string BuildImplementations(Compilation compilation)
    {
        static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type) =>
            [type, .. type.GetTypeMembers().SelectMany(WithNested)];

        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        foreach (var type in compilation.Assembly.GlobalNamespace.GetTypeMembersRecursive().SelectMany(WithNested))
        {
            var members = type.GetMembers().Where(IsDefinitionToImplement).ToList();
            if (members.Count == 0)
            {
                continue;
            }

            builder.Append("namespace ").Append(CSharpIdentifier.EscapeQualifiedName(type.ContainingNamespace.ToDisplayString())).AppendLine(" {");
            var containingTypes = type.GetContainingTypes();
            foreach (var containingType in containingTypes)
            {
                builder.Append(containingType.GetPartialDeclaration()).AppendLine(" {");
            }

            builder.Append(type.GetPartialDeclaration()).AppendLine(" {");
            foreach (var member in members)
            {
                var syntax = member.DeclaringSyntaxReferences[0].GetSyntax();
                if ((member is IMethodSymbol method) && (syntax is MethodDeclarationSyntax methodSyntax))
                {
                    builder.Append(method.GetImplementationSignature(methodSyntax)).AppendLine(" => throw new global::System.NotImplementedException();");
                }
                else if ((member is IPropertySymbol property) && (syntax is BasePropertyDeclarationSyntax propertySyntax))
                {
                    builder.Append(property.GetImplementationSignature(propertySyntax)).AppendLine(" {");
                    foreach (var accessor in propertySyntax.AccessorList!.Accessors)
                    {
                        builder.Append(accessor.GetImplementationAccessor()).AppendLine(" => throw new global::System.NotImplementedException();");
                    }

                    builder.AppendLine("}");
                }
            }

            builder.Append('}', containingTypes.Count + 2).AppendLine();
        }

        return builder.ToString();
    }

    private static bool IsDefinitionToImplement(ISymbol member) =>
        member switch
        {
            IMethodSymbol method => method.IsPartialDefinition && (method.PartialImplementationPart is null),
            IPropertySymbol property => property.IsPartialDefinition && (property.PartialImplementationPart is null),
            _ => false
        };

    [Fact]
    public void ImplementationsCompileWithDefinitions()
    {
        var compilation = TestCompilation.Create(Definitions);
        var implementations = BuildImplementations(compilation);

        // Warnings of the type named record itself
        var problems = TestCompilation.GetProblems(TestCompilation.AddSource(compilation, implementations, "Implementations.cs"))
            .Where(static x => x.Id is not ("CS8860" or "CS8981"))
            .ToList();

        Assert.True(problems.Count == 0, String.Join(Environment.NewLine, problems) + Environment.NewLine + implementations);
    }

    [Fact]
    public void DefinitionsNeedImplementations()
    {
        var problems = TestCompilation.GetProblems(TestCompilation.Create(Definitions));

        Assert.Contains(problems, static x => x.Id == "CS8795");
    }

    private static string Signature(string typeName, string memberName)
    {
        var type = TestCompilation.Create(Definitions).GetTypeByMetadataName("App.event." + typeName)!;
        var member = type.GetMembers(memberName).First(IsDefinitionToImplement);
        var syntax = member.DeclaringSyntaxReferences[0].GetSyntax();
        return member switch
        {
            IMethodSymbol method => method.GetImplementationSignature((MethodDeclarationSyntax)syntax),
            _ => ((IPropertySymbol)member).GetImplementationSignature((BasePropertyDeclarationSyntax)syntax)
        };
    }

    [Theory]
    [InlineData("Methods", "NoAccessibility", "static partial void NoAccessibility(string s)")]
    [InlineData("Methods", "Nullable", "public static partial string? Nullable(string? s, global::System.Collections.Generic.List<string?> list)")]
    [InlineData("Methods", "RefReadOnlyReturn", "public static partial ref readonly int RefReadOnlyReturn(in int x)")]
    [InlineData("Methods", "Modifiers", "public static partial void Modifiers(this string s, scoped ref int a, ref readonly int b, out int c, params int[] d)")]
    [InlineData("Methods", "Generic", "public static partial T Generic<T, U>(T value, U other) where T : class?, global::System.IComparable<T> where U : struct")]
    [InlineData("Methods", "Keyword", "public static partial void Keyword(int @event, string @class)")]
    [InlineData("Methods", "Tuple", "public static partial (int A, string? B) Tuple((int X, int Y) value)")]
    [InlineData("Methods", "Defaults", "public static partial void Defaults(int x, string? s)")]
    [InlineData("Instance", "Generic", "public override partial void Generic<T>() where T : class")]
    [InlineData("Properties", "Required", "public required partial string Required")]
    [InlineData("Properties", "this[]", "public partial int this[int index]")]
    [InlineData("Properties", "class", "public partial string @class")]
    public void SignatureRepeatsDefinition(string typeName, string memberName, string expected)
    {
        Assert.Equal(expected, Signature(typeName, memberName));
    }

    [Fact]
    public void AccessorRepeatsModifiers()
    {
        var type = TestCompilation.Create(Definitions).GetTypeByMetadataName("App.event.Properties")!;
        string Accessors(string name) =>
            String.Join(" ", ((BasePropertyDeclarationSyntax)type.GetMembers(name).First(IsDefinitionToImplement).DeclaringSyntaxReferences[0].GetSyntax())
                .AccessorList!.Accessors.Select(static x => x.GetImplementationAccessor()));

        Assert.Equal("get init", Accessors("Init"));
        Assert.Equal("get private set", Accessors("PrivateSet"));
    }
}
