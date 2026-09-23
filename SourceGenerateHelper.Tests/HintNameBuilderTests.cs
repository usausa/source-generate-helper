namespace SourceGenerateHelper.Tests;

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
}
