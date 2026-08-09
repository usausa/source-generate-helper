namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;

public sealed class ResultTest
{
    private static DiagnosticInfo CreateDiagnostic(string id, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        var descriptor = new DiagnosticDescriptor(id, "Title", "Message", "Test", severity, isEnabledByDefault: true);
        return new DiagnosticInfo(descriptor, (Location?)null);
    }

    // ------------------------------------------------------------------
    // Success / Error state
    // ------------------------------------------------------------------

    [Fact]
    public void SuccessHasValueAndNoDiagnostics()
    {
        // Act
        var result = Results.Success(5);

        // Assert
        Assert.False(result.HasDiagnostics);
        Assert.False(result.HasErrors);
        Assert.True(result.HasValue);
        Assert.Equal(5, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ErrorHasDiagnostic()
    {
        // Arrange
        var diagnostic = CreateDiagnostic("TST0001");

        // Act
        var result = Results.Error<int>(diagnostic);

        // Assert
        Assert.True(result.HasDiagnostics);
        Assert.False(result.HasValue);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void ErrorsHasMultipleDiagnostics()
    {
        // Arrange
        var d1 = CreateDiagnostic("TST0001");
        var d2 = CreateDiagnostic("TST0002");

        // Act
        var result = Results.Errors<int>(d1, d2);

        // Assert
        Assert.True(result.HasDiagnostics);
        Assert.False(result.HasValue);
        Assert.Equal(2, result.Diagnostics.Count);
    }

    // ------------------------------------------------------------------
    // SelectValue / SelectError
    // ------------------------------------------------------------------

    [Fact]
    public void SelectValueExcludesValueTypeErrors()
    {
        // Arrange
        Result<int>[] results =
        [
            Results.Success(5),
            Results.Error<int>(CreateDiagnostic("TST0001"))
        ];

        // Act
        var values = results.SelectValue().ToList();

        // Assert
        Assert.Single(values);
        Assert.Equal(5, values[0]);
    }

    [Fact]
    public void SelectValueExcludesReferenceTypeErrors()
    {
        // Arrange
        Result<string>[] results =
        [
            Results.Success("a"),
            Results.Error<string>(CreateDiagnostic("TST0001"))
        ];

        // Act
        var values = results.SelectValue().ToList();

        // Assert
        Assert.Single(values);
        Assert.Equal("a", values[0]);
    }

    [Fact]
    public void SelectErrorReturnsAllDiagnostics()
    {
        // Arrange
        Result<int>[] results =
        [
            Results.Success(5),
            Results.Error<int>(CreateDiagnostic("TST0001")),
            Results.Errors<int>(CreateDiagnostic("TST0002"), CreateDiagnostic("TST0003"))
        ];

        // Act
        var errors = results.SelectError().ToList();

        // Assert
        Assert.Equal(3, errors.Count);
    }

    // ------------------------------------------------------------------
    // Severity
    // ------------------------------------------------------------------

    [Fact]
    public void HasErrorsOnlyForErrorSeverity()
    {
        // Act
        var warning = new Result<int>(5, new EquatableArray<DiagnosticInfo>([CreateDiagnostic("TST0001")]));
        var error = new Result<int>(5, new EquatableArray<DiagnosticInfo>([CreateDiagnostic("TST0002", DiagnosticSeverity.Error)]));

        // Assert
        Assert.True(warning.HasDiagnostics);
        Assert.False(warning.HasErrors);
        Assert.True(error.HasDiagnostics);
        Assert.True(error.HasErrors);
    }

    [Fact]
    public void SelectValueKeepsValueWithDiagnostics()
    {
        // Arrange
        Result<string>[] results =
        [
            Results.Success("a"),
            new("b", new EquatableArray<DiagnosticInfo>([CreateDiagnostic("TST0001")])),
            new("c", new EquatableArray<DiagnosticInfo>([CreateDiagnostic("TST0002", DiagnosticSeverity.Error)])),
            Results.Error<string>(CreateDiagnostic("TST0003"))
        ];

        // Act
        var values = results.SelectValue().ToList();

        // Assert
        Assert.Equal(["a", "b", "c"], values);
    }

    // ------------------------------------------------------------------
    // HasValue
    // ------------------------------------------------------------------

    [Fact]
    public void HasValueTrueForSuccess()
    {
        // Act
        var result = Results.Success("hello");

        // Assert
        Assert.True(result.HasValue);
    }

    [Fact]
    public void HasValueFalseForError()
    {
        // Act
        var result = Results.Error<string>(CreateDiagnostic("TST0001"));

        // Assert
        Assert.False(result.HasValue);
    }

    [Fact]
    public void HasValueGuaranteesNonNullValue()
    {
        // Act
        var result = Results.Success("hello");

        // Assert
        if (result.HasValue)
        {
            // Value is statically known to be non-null after HasValue check
            Assert.Equal(5, result.Value.Length);
        }
    }
}
