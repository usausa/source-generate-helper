namespace SourceGenerateHelper.Tests;

public sealed class HintNameBuilderTest
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
        Assert.Equal("Data.g.cs", HintNameBuilder.Build(String.Empty, "Data"));
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

    // An optional suffix can be passed unconditionally; it must not leave a dangling separator.
    [Fact]
    public void EmptyPartsAreSkipped()
    {
        Assert.Equal("Test_Data.g.cs", HintNameBuilder.Build("Test", "Data", String.Empty));
        Assert.Equal("Test_Data.g.cs", HintNameBuilder.Build("Test", String.Empty, "Data"));
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
    // Parity with the hand-rolled MakeFilename implementations it replaces
    //-----------------------------------------------------------------------

    // MemberAccessor / ServiceRegistration / AspNetCore / ByteMapper / Mapper:
    //   MakeFilename(ns, className) -> "{ns}_{class}_{fixed suffix}.g.cs"
    [Fact]
    public void MatchesNamespaceAndClassWithFixedSuffix()
    {
        Assert.Equal("Test_Ns_Data_Accessor.g.cs", HintNameBuilder.Build("Test.Ns", "Data", "Accessor"));
    }

    // Lambda / Functions / MauiComponents / Navigation:
    //   MakeFilename(ns, className, methodName)
    [Fact]
    public void MatchesNamespaceClassAndMethod()
    {
        Assert.Equal("Test_Ns_Handlers_Run.g.cs", HintNameBuilder.Build("Test.Ns", "Handlers", "Run"));
    }

    // CommonCode:
    //   MakeFilename(ns, containingTypes, className, suffix)
    [Fact]
    public void MatchesContainingTypesAndSuffix()
    {
        string[] containingTypes = ["Outer<T>", "Middle"];

        Assert.Equal(
            "Test_Ns_Outer[T]_Middle_Data_CompareTo.g.cs",
            HintNameBuilder.Build("Test.Ns", [.. containingTypes, "Data", "CompareTo"]));
    }

    // ByteMapper.AspNetCore:
    //   MakeFilename(ns, className, nameSuffix) + ".AspNetCore.g.cs", where nameSuffix already
    //   carries its own leading '_' and may be empty.
    [Fact]
    public void MatchesByteMapperAspNetCoreShape()
    {
        Assert.Equal(
            "Test_Ns_SampleMappers.AspNetCore.g.cs",
            HintNameBuilder.BuildWithExtension("Test.Ns", ".AspNetCore.g.cs", "SampleMappers", String.Empty));

        Assert.Equal(
            "Test_Ns_SampleMappers_EntityA_MyProfile.AspNetCore.g.cs",
            HintNameBuilder.BuildWithExtension("Test.Ns", ".AspNetCore.g.cs", "SampleMappers", "EntityA", "MyProfile"));
    }
}
