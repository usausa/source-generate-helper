namespace SourceGenerateHelper.Tests;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper.Testing;

public sealed class HintNameBuilderTests
{
    //-----------------------------------------------------------------------
    // Shape
    //-----------------------------------------------------------------------

    [Fact]
    public void NamespaceDotsBecomeUnderscores()
    {
        Assert.Equal("Test_Ns_Data.g.cs", HintNameBuilder.Build("Test.Ns", "Data"));
    }

    [Fact]
    public void EmptyNamespaceIsOmitted()
    {
        Assert.Equal("Data.g.cs", HintNameBuilder.Build(string.Empty, "Data"));
        Assert.Equal("Data.g.cs", HintNameBuilder.Build(null, "Data"));
    }

    [Fact]
    public void GenericBracketsAreReplaced()
    {
        Assert.Equal("Test_Data[T].g.cs", HintNameBuilder.Build("Test", "Data<T>"));
    }

    [Fact]
    public void AtOfKeywordIsRemoved()
    {
        Assert.Equal("Test_event_class.g.cs", HintNameBuilder.Build("Test.@event", "@class"));
    }

    [Fact]
    public void UnderscoresInNamesBecomeHyphens()
    {
        Assert.Equal("Test_Outer-Inner.g.cs", HintNameBuilder.Build("Test", "Outer_Inner"));
        Assert.Equal("Test-Ns_Data.g.cs", HintNameBuilder.Build("Test_Ns", "Data"));
    }

    [Fact]
    public void PartsAreJoinedWithUnderscore()
    {
        Assert.Equal("Test_Outer_Inner_Data_Suffix.g.cs", HintNameBuilder.Build("Test", "Outer", "Inner", "Data", "Suffix"));
    }

    [Fact]
    public void EmptyPartsAreSkipped()
    {
        Assert.Equal("Test_Data.g.cs", HintNameBuilder.Build("Test", "Data", string.Empty));
        Assert.Equal("Test_Data.g.cs", HintNameBuilder.Build("Test", string.Empty, "Data"));
    }

    [Fact]
    public void NoPartsYieldsNamespaceOnly()
    {
        Assert.Equal("Test_Ns.g.cs", HintNameBuilder.Build("Test.Ns"));
    }

    [Fact]
    public void ExtensionCanBeOverridden()
    {
        Assert.Equal(
            "Test_Ns_Data.AspNetCore.g.cs",
            HintNameBuilder.BuildWithExtension("Test.Ns", ".AspNetCore.g.cs", "Data"));
    }

    //-----------------------------------------------------------------------
    // Collision
    //-----------------------------------------------------------------------

    [Fact]
    public void NestedTypeAndUnderscoreNameDoNotCollide()
    {
        Assert.NotEqual(HintNameBuilder.Build("Test", "Outer", "Inner"), HintNameBuilder.Build("Test", "Outer_Inner"));
    }

    [Fact]
    public void NamespaceDotAndUnderscoreDoNotCollide()
    {
        Assert.NotEqual(HintNameBuilder.Build("Test.Ns", "Data"), HintNameBuilder.Build("Test_Ns", "Data"));
    }

    [Fact]
    public void PartBoundaryAndUnderscoreDoNotCollide()
    {
        Assert.NotEqual(HintNameBuilder.Build("Test", "Handlers_Run", "Async"), HintNameBuilder.Build("Test", "Handlers", "Run_Async"));
    }

    //-----------------------------------------------------------------------
    // Implementations
    //-----------------------------------------------------------------------

    [Fact]
    public void MatchesNamespaceAndClassWithFixedSuffix()
    {
        Assert.Equal("Test_Ns_Data_Accessor.g.cs", HintNameBuilder.Build("Test.Ns", "Data", "Accessor"));
    }

    [Fact]
    public void MatchesNamespaceClassAndMethod()
    {
        Assert.Equal("Test_Ns_Handlers_Run.g.cs", HintNameBuilder.Build("Test.Ns", "Handlers", "Run"));
    }

    [Fact]
    public void MatchesNamespaceClassAndSharedSuffix()
    {
        Assert.Equal("Test_Ns_Handlers_--shared--.g.cs", HintNameBuilder.Build("Test.Ns", "Handlers", "__shared__"));
    }

    [Fact]
    public void MatchesContainingTypesAndSuffix()
    {
        string[] containingTypes = ["Outer<T>", "Middle"];

        Assert.Equal(
            "Test_Ns_Outer[T]_Middle_Data_CompareTo.g.cs",
            HintNameBuilder.Build("Test.Ns", [.. containingTypes, "Data", "CompareTo"]));
    }

    [Fact]
    public void MatchesByteMapperAspNetCoreShape()
    {
        Assert.Equal(
            "Test_Ns_SampleMappers.AspNetCore.g.cs",
            HintNameBuilder.BuildWithExtension("Test.Ns", ".AspNetCore.g.cs", "SampleMappers", string.Empty));

        Assert.Equal(
            "Test_Ns_SampleMappers_EntityA_MyProfile.AspNetCore.g.cs",
            HintNameBuilder.BuildWithExtension("Test.Ns", ".AspNetCore.g.cs", "SampleMappers", "EntityA", "MyProfile"));
    }

    //-----------------------------------------------------------------------
    // Type
    //-----------------------------------------------------------------------

    private const string TypeSource =
        """
        namespace Test.Ns
        {
            public class Outer
            {
                public class Inner { }
                public class Generic<T> { }
            }

            public class Item { }
            public class Item<T> { }
            public class Under_Score { }
        }

        namespace Test.Other
        {
            public class Item { }
        }

        namespace Test.@event
        {
            public class @class { }
        }

        public class Global { }
        """;

    private static INamedTypeSymbol GetType(string metadataName) =>
        TestCompilation.Create(TypeSource).GetTypeByMetadataName(metadataName)!;

    [Theory]
    [InlineData("Test.Ns.Outer", "Test_Ns_Outer.g.cs")]
    [InlineData("Test.Ns.Outer+Inner", "Test_Ns_Outer+Inner.g.cs")]
    [InlineData("Test.Ns.Outer+Generic`1", "Test_Ns_Outer+Generic`1.g.cs")]
    [InlineData("Test.Ns.Item`1", "Test_Ns_Item`1.g.cs")]
    [InlineData("Test.Ns.Under_Score", "Test_Ns_Under-Score.g.cs")]
    [InlineData("Test.event.class", "Test_event_class.g.cs")]
    [InlineData("Global", "Global.g.cs")]
    public void TypeNameHasNamespaceNestingAndArity(string metadataName, string expected)
    {
        Assert.Equal(expected, HintNameBuilder.BuildFromType(GetType(metadataName)));
    }

    [Fact]
    public void TypeNameTakesPartsAndExtension()
    {
        Assert.Equal("Test_Ns_Outer+Inner_Accessor.g.cs", HintNameBuilder.BuildFromType(GetType("Test.Ns.Outer+Inner"), "Accessor"));
        Assert.Equal("Test_Ns_Item.AspNetCore.g.cs", HintNameBuilder.BuildFromTypeWithExtension(GetType("Test.Ns.Item"), ".AspNetCore.g.cs"));
    }

    private static readonly string[] SameNameTypes = ["Test.Ns.Item", "Test.Ns.Item`1", "Test.Other.Item", "Test.Ns.Outer+Inner"];

    [Fact]
    public void TypesOfOneNameGetTheirOwnNames()
    {
        var names = SameNameTypes.Select(static x => HintNameBuilder.BuildFromType(GetType(x))).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void TypeNamesAreAcceptedAsHintNames()
    {
        var result = new GeneratorTestRunner(new TypeFileGenerator()).Run(TypeSource);

        Assert.Empty(result.Problems);
        Assert.Contains("Test_Ns_Outer+Generic`1.g.cs", result.GeneratedSources.Keys);
    }

    internal sealed class TypeFileGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var names = context.CompilationProvider.Select(static (compilation, _) =>
                compilation.Assembly.GlobalNamespace.GetTypeMembersRecursive()
                    .SelectMany(static x => x.GetTypeMembers().Prepend(x))
                    .Select(static x => HintNameBuilder.BuildFromType(x))
                    .ToImmutableArray());

            context.RegisterSourceOutput(names, static (production, items) =>
            {
                foreach (var name in items)
                {
                    production.AddSource(name, SourceText.From("// generated", Encoding.UTF8));
                }
            });
        }
    }
}
