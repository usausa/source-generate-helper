namespace SourceGenerateHelper;

using System;

public readonly ref struct IndentScope : IDisposable
{
    private readonly SourceBuilder builder;

    public IndentScope(SourceBuilder builder)
    {
        this.builder = builder;
        builder.BeginScope();
    }

    public void Dispose()
    {
        builder.EndScope();
    }
}
