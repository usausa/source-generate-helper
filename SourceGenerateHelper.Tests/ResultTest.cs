namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;

public sealed class ResultTest
{
    private static DiagnosticInfo CreateDiagnostic(string id)
    {
        var descriptor = new DiagnosticDescriptor(id, "Title", "Message", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);
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
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
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
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
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
        Assert.True(result.IsError);
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
}
