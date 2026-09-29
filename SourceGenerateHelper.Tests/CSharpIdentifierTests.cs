namespace SourceGenerateHelper.Tests;

public sealed class CSharpIdentifierTests
{
    // ------------------------------------------------------------
    // Escape
    // ------------------------------------------------------------

    [Theory]
    [InlineData("name", "name")]
    [InlineData("event", "@event")]
    [InlineData("class", "@class")]
    [InlineData("default", "@default")]
    [InlineData("record", "record")]
    [InlineData("value", "value")]
    [InlineData("@event", "@event")]
    public void EscapeWritesKeywordWithAt(string name, string expected)
    {
        Assert.Equal(expected, CSharpIdentifier.Escape(name));
    }

    [Theory]
    [InlineData("Name", "Name")]
    [InlineData("class", "@class")]
    [InlineData("record", "@record")]
    [InlineData("required", "@required")]
    [InlineData("file", "@file")]
    public void EscapeTypeNameWritesContextualKeywordWithAt(string name, string expected)
    {
        Assert.Equal(expected, CSharpIdentifier.EscapeTypeName(name));
    }

    [Theory]
    [InlineData("Company.Product", "Company.Product")]
    [InlineData("Company.event.Model", "Company.@event.Model")]
    [InlineData("event", "@event")]
    [InlineData("global::System", "global::System")]
    public void EscapeQualifiedNameEscapesEachName(string name, string expected)
    {
        Assert.Equal(expected, CSharpIdentifier.EscapeQualifiedName(name));
    }

    // ------------------------------------------------------------
    // Valid
    // ------------------------------------------------------------

    [Theory]
    [InlineData("name", true)]
    [InlineData("_name1", true)]
    [InlineData("class", true)]
    [InlineData("1st", false)]
    [InlineData("a-b", false)]
    [InlineData("a b", false)]
    [InlineData("", false)]
    public void IsValidTakesTheCharactersOfAName(string name, bool expected)
    {
        Assert.Equal(expected, CSharpIdentifier.IsValid(name));
    }

    // ------------------------------------------------------------
    // Compile
    // ------------------------------------------------------------

    [Fact]
    public void EscapedNamesCompile()
    {
        var source =
            $$"""
            namespace {{CSharpIdentifier.EscapeQualifiedName("Test.event")}};

            public class {{CSharpIdentifier.EscapeTypeName("record")}}
            {
                public int {{CSharpIdentifier.Escape("class")}} { get; set; }

                public void Method(int {{CSharpIdentifier.Escape("event")}}) => {{CSharpIdentifier.Escape("class")}} = {{CSharpIdentifier.Escape("event")}};
            }
            """;

        Assert.DoesNotContain(TestCompilation.GetProblems(TestCompilation.Create(source)), static x => x.Id != "CS8981");
    }
}
