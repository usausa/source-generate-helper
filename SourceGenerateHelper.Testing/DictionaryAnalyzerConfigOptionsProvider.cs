namespace SourceGenerateHelper.Testing;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

// An AnalyzerConfigOptionsProvider backed by a plain dictionary, so tests can supply
// build_property.* values without an .editorconfig.
internal sealed class DictionaryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private static readonly DictionaryAnalyzerConfigOptions Empty = new([]);

    private readonly DictionaryAnalyzerConfigOptions globalOptions;

    public DictionaryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> options)
    {
        globalOptions = new DictionaryAnalyzerConfigOptions(
            options.ToImmutableDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase));
    }

    public override AnalyzerConfigOptions GlobalOptions => globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;

    private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> options;

        public DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            this.options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (options.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
