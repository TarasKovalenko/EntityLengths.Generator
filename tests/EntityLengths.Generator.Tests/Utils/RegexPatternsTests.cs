using EntityLengths.Generator.Utils;
using Xunit;

namespace EntityLengths.Generator.Tests.Utils;

public class RegexPatternsTests
{
    [Theory]
    [InlineData("varchar(100)", "100")]
    [InlineData("VARCHAR(50)", "50")]
    [InlineData("char(10)", "10")]
    [InlineData("CHAR(20)", "20")]
    [InlineData("varCHAR(30)", "30")]
    [InlineData("VarChar(40)", "40")]
    [InlineData("varchar(100) NOT NULL", "100")]
    [InlineData("VARCHAR(50) NULL", "50")]
    [InlineData("char(10) DEFAULT NULL", "10")]
    [InlineData("NVARCHAR(20) COLLATE SQL_Latin1_General_CP1_CI_AS", "20")]
    [InlineData(" varchar(100) ", "100")]
    [InlineData("\tvarchar(50)\t", "50")]
    [InlineData("\nvarchar(30)\r\n", "30")]
    [InlineData("VARCHAR(00100)", "00100")]
    [InlineData("char(0010)", "0010")]
    [InlineData("varchar(01)", "01")]
    public void VarCharLength_Should_Extract_Length(string input, string expectedLength)
    {
        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.True(match.Success);
        Assert.Equal(expectedLength, match.Groups[1].Value);
    }

    [Theory]
    [InlineData("varchar(max)")]
    [InlineData("varchar")]
    [InlineData("char")]
    [InlineData("varchar()")]
    [InlineData("varchar(abc)")]
    [InlineData("varchar(-1)")]
    [InlineData("varcharacter(100)")]
    [InlineData("characters(50)")]
    [InlineData("text")]
    [InlineData("ntext")]
    public void VarCharLength_Should_Not_Match_Invalid_Patterns(string input)
    {
        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.False(match.Success);
    }

    [Theory]
    [InlineData("varchar(1)")]
    [InlineData("varchar(8000)")] // SQL Server maximum
    [InlineData("char(1)")]
    [InlineData("char(8000)")]
    public void VarCharLength_Should_Match_Boundary_Values(string input)
    {
        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.True(match.Success);
    }

    [Fact]
    public void VarCharLength_Should_Extract_First_Match_Only()
    {
        // Arrange
        var input = "varchar(100), varchar(200)";

        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("100", match.Groups[1].Value);
    }

    [Theory]
    [InlineData("varchar(8001)")]
    [InlineData("varchar(99999)")]
    [InlineData("char(10000)")]
    public void VarCharLength_Should_Match_Even_Invalid_SQL_Lengths(string input)
    {
        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.True(
            match.Success,
            "Regex should match any number, validation of actual SQL limits should be handled separately"
        );
    }

    [Theory]
    [InlineData("varchar  (100)", "100")]
    [InlineData("varchar\t(50)", "50")]
    [InlineData("char    (20)", "20")]
    public void VarCharLength_Should_Handle_Spaces_Between_Type_And_Length(
        string input,
        string expectedLength
    )
    {
        // Act
        var match = RegexPatterns.VarCharLength.Match(input);

        // Assert
        Assert.True(match.Success);
        Assert.Equal(expectedLength, match.Groups[1].Value);
    }
}
