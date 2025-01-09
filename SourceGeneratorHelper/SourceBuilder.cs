namespace SourceGeneratorHelper;

using System.Text;

public sealed class SourceBuilder
{
    private readonly StringBuilder builder = new();

    public override string ToString()
    {
        return builder.ToString();
    }
}
