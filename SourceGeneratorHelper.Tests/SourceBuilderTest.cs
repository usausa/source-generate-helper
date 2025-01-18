namespace SourceGeneratorHelper.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class SourceBuilderTest
{
    [Fact]
    public void TestCondition()
    {
        const string source =
            """
            a
            xyz
            1
            1.00
            1, 2, 3

            """;

        var builder = new SourceBuilder();

        builder.AppendIf(false, 'z');
        builder.AppendIf(true, 'a');
        builder.NewLine();

        builder.AppendIf(false, "abc");
        builder.AppendIf(true, "xyz");
        builder.NewLine();

        builder.AppendIf(false, 0);
        builder.AppendIf(true, 1);
        builder.NewLine();

        builder.AppendFormatIf(false, "{0:F2}", 0f);
        builder.AppendFormatIf(true, "{0:F2}", 1f);
        builder.NewLine();

        builder.AppendJoinIf(false, [0, 0, 0], ", ");
        builder.AppendJoinIf(true, [1, 2, 3], ", ");
        builder.NewLine();

        Assert.Equal(source, builder.ToString());
    }

    [Fact]
    public void TestClass()
    {
        const string source =
            """
            // <auto-generated />
            #nullable enable
            #pragma warning disable IDE0000
            namespace Test;

            using System;

            // class
            public class Class
            {
                private int x;
                private int y = 0;

                public string Name { get; set; } = string.Empty;

                public T Method<T>(T a)
                {
                    return a;
                }

                public string Check(int value)
                {
                    if (value == 0)
                    {
                        return "a";
                    }
                    else if (value == 1)
                    {
                        return "b";
                    }
                    else
                    {
                        return "c";
                    }
                }

                public int Count()
                {
                    var ret = 0;
                    for (var i = x; i < 10; i++)
                    {
                        ret += i;
                    }
                    return ret;
                }

                public int Count2()
                {
                    var ret = 0;
                    while (y < 5)
                    {
                        ret++;
                    }
                    return ret;
                }

                public IEnumerable<TResult> Convert<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> converter)
                {
                    foreach (var value in source)
                    {
                        yield return converter(value);
                    }
                }
            }

            """;

        var builder = new SourceBuilder();

        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Disable("IDE0000");
        builder.Namespace("Test");
        builder.NewLine();
        builder.Using("System");
        builder.NewLine();

        builder.Comment("class");
        builder.Append("public class Class").NewLine();
        builder.BeginScope();

        builder.Field("int", "x");
        builder.Field("int", "y", "0");
        builder.NewLine();

        builder.Property("string", "Name", "string.Empty");
        builder.NewLine();

        builder.Indent().Accessibility(Accessibility.Public).Append(" T Method").Generics("T").Append('(').Argument("T", "a").Append(')').NewLine();
        builder.BeginScope();
        builder.Return("a");
        builder.EndScope();

        builder.NewLine();

        builder.Indent().Accessibility(Accessibility.Public).Append(" string Check(int value)").NewLine();
        builder.BeginScope();

        builder.If("value == 0");
        builder.Return("\"a\"");
        builder.ElseIf("value == 1");
        builder.Return("\"b\"");
        builder.Else();
        builder.Return("\"c\"");
        builder.EndIf();
        builder.EndScope();

        builder.NewLine();

        builder.Indent().Accessibility(Accessibility.Public).Append(" int Count()").NewLine();
        builder.BeginScope();
        builder.Var("ret", "0");
        builder.For("i = x", "i < 10", "i++");
        builder.Indent().Append("ret += i;").NewLine();
        builder.EndFor();
        builder.Return("ret");
        builder.EndScope();

        builder.NewLine();

        builder.Indent().Accessibility(Accessibility.Public).Append(" int Count2()").NewLine();
        builder.BeginScope();
        builder.Var("ret", "0");
        builder.While("y < 5");
        builder.Indent().Append("ret++;").NewLine();
        builder.EndWhile();
        builder.Return("ret");
        builder.EndScope();

        builder.NewLine();

        builder
            .Indent()
            .Accessibility(Accessibility.Public)
            .Append(" IEnumerable")
            .Generics("TResult")
            .Append(" Convert")
            .Generics(["TSource", "TResult"])
            .Append('(')
            .Arguments([("IEnumerable<TSource>", "source"), ("Func<TSource, TResult>", "converter")])
            .Append(')')
            .NewLine();
        builder.BeginScope();
        builder.ForEach("value", "source");
        builder.YieldReturn("converter(value)");
        builder.EndForEach();
        builder.EndScope();

        builder.EndScope();

        Assert.Equal(source, builder.ToString());

        // Clear
        builder.Clear();

        Assert.Equal(0, builder.Length);
        Assert.Equal(0, builder.IndentLevel);
    }

    [Fact]
    public void TestBySymbol()
    {
        const string source =
            """
            public class Class
            {
                private int x = 0;

                public string Name { get; set; } = string.Empty;

                public void Method(int value)
                {
                    Converter.Convert<int>(value);
                }

                public void Method(int value1, string value2)
                {
                    Converter.Convert<int, string>(value1, value2);
                }
            }

            """;

        var metadataReferences = new[] { MetadataReference.CreateFromFile(typeof(int).Assembly.Location) };
        var compilation = CSharpCompilation.Create("Dummy", references: metadataReferences);

        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var stringSymbol = compilation.GetTypeByMetadataName("System.String")!;

        var builder = new SourceBuilder();

        builder.Append("public class Class").NewLine();
        builder.BeginScope();

        builder.Field(intSymbol, "x", "0");
        builder.NewLine();

        builder.Property(stringSymbol, "Name", "string.Empty");
        builder.NewLine();

        builder.Indent().Accessibility(Accessibility.Public).Append(" void Method(").Argument(intSymbol, "value").Append(')').NewLine();
        builder.BeginScope();
        builder.Indent().Append("Converter.Convert").Generics(intSymbol).Append("(value);").NewLine();
        builder.EndScope();

        builder.NewLine();

        builder
            .Indent()
            .Accessibility(Accessibility.Public)
            .Append(" void Method(")
            .Arguments([(intSymbol, "value1"), (stringSymbol, "value2")])
            .Append(')')
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("Converter.Convert").Generics(intSymbol, stringSymbol).Append("(value1, value2);").NewLine();
        builder.EndScope();

        builder.EndScope();

        Assert.Equal(source, builder.ToString());
    }
}
