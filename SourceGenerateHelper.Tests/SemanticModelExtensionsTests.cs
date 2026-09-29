namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;

public sealed class SemanticModelExtensionsTests
{
    private const string Source =
        """
        public enum Sample { A, B }

        public class Base
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int M { get; set; }
            public int S { get; set; }
            private int P { get; set; }
            protected int Q { get; set; }
            public int this[int index] => index;
            public virtual int V { get; set; }
        }

        public class Derived : Base
        {
            public new string X { get; set; } = "";
            public new int Z;
            public new void M() { }
            public static new int S { get; set; }
            public override int V { get; set; }
            public int Own { get; set; }
        }

        public interface IA { int A { get; } }
        public interface IB { int A { get; } }
        public interface IC : IA, IB { }
        public interface ID : IA { new int A { get; } }

        public class Holder
        {
            public void Run(Derived d) { /*here*/ }
        }

        public class Constrained<T> where T : Base
        {
            public void Run(T t) { /*constrained*/ }
        }
        """;

    private static (SemanticModel Model, int Position, Compilation Compilation) Create(string marker = "/*here*/")
    {
        var compilation = TestCompilation.Create(Source);
        var tree = compilation.SyntaxTrees[0];
        return (compilation.GetSemanticModel(tree), TestCompilation.PositionOf(tree, marker), compilation);
    }

    // ------------------------------------------------------------
    // Conversion
    // ------------------------------------------------------------

    [Theory]
    [InlineData("5", SpecialType.System_Byte, true)]
    [InlineData("300", SpecialType.System_Byte, false)]
    [InlineData("1.5d", SpecialType.System_Int32, false)]
    [InlineData("1", SpecialType.System_Double, true)]
    [InlineData("null", SpecialType.System_Int32, false)]
    [InlineData("null", SpecialType.System_String, true)]
    [InlineData("\"x\"", SpecialType.System_Object, true)]
    [InlineData("\"x\"", SpecialType.System_Int32, false)]
    public void IsImplicitlyConvertibleToSpecialType(string expression, SpecialType type, bool expected)
    {
        var (model, position, compilation) = Create();

        Assert.Equal(expected, model.IsImplicitlyConvertible(position, expression, compilation.GetSpecialType(type)));
    }

    [Fact]
    public void IsImplicitlyConvertibleToEnum()
    {
        var (model, position, compilation) = Create();
        var sample = compilation.GetTypeByMetadataName("Sample")!;

        Assert.True(model.IsImplicitlyConvertible(position, "global::Sample.B", sample));
        Assert.True(model.IsImplicitlyConvertible(position, "0", sample));
        Assert.False(model.IsImplicitlyConvertible(position, "1", sample));
    }

    // ------------------------------------------------------------
    // Lookup
    // ------------------------------------------------------------

    [Fact]
    public void LookupInstancePropertiesTakesWhatNamesBindTo()
    {
        var (model, position, compilation) = Create();
        var derived = compilation.GetTypeByMetadataName("Derived")!;

        var properties = model.LookupInstanceProperties(position, derived);

        Assert.Equal(["Own", "V", "X", "Y"], properties.Select(static x => x.Name).OrderBy(static x => x, StringComparer.Ordinal));
        Assert.Equal("Derived", properties.Single(static x => x.Name == "X").ContainingType.Name);
        Assert.Equal("Derived", properties.Single(static x => x.Name == "V").ContainingType.Name);
        Assert.Equal("Base", properties.Single(static x => x.Name == "Y").ContainingType.Name);
    }

    [Fact]
    public void LookupInstancePropertiesOfTypeParameterTakesThoseOfConstraint()
    {
        var (model, position, compilation) = Create("/*constrained*/");
        var typeParameter = compilation.GetTypeByMetadataName("Constrained`1")!.TypeParameters[0];

        var properties = model.LookupInstanceProperties(position, typeParameter);

        Assert.Contains(properties, static x => x.Name == "X");
        Assert.DoesNotContain(properties, static x => x.Name == "P");
    }

    [Fact]
    public void LookupMemberTakesDerivedMember()
    {
        var (model, position, compilation) = Create();
        var derived = compilation.GetTypeByMetadataName("Derived")!;

        Assert.IsAssignableFrom<IFieldSymbol>(model.LookupMember(position, derived, "Z"));
        Assert.Equal("Derived", model.LookupMember(position, derived, "X")!.ContainingType.Name);
        Assert.Null(model.LookupMember(position, derived, "P"));
        Assert.Null(model.LookupMember(position, derived, "Missing"));
    }

    [Fact]
    public void LookupMemberOfAmbiguousNameIsNull()
    {
        var (model, position, compilation) = Create();

        Assert.Null(model.LookupMember(position, compilation.GetTypeByMetadataName("IC")!, "A"));
        Assert.Equal("ID", model.LookupMember(position, compilation.GetTypeByMetadataName("ID")!, "A")!.ContainingType.Name);
    }
}
