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
