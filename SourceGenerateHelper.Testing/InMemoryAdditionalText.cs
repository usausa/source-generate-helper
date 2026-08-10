namespace SourceGenerateHelper.Testing;

using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

// An AdditionalText backed by a string, for generators that read AdditionalTextsProvider.
internal sealed class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText text;

    public InMemoryAdditionalText(string path, string content)
    {
        Path = path;
        text = SourceText.From(content, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => text;
}
