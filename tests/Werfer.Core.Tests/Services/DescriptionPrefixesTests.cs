namespace Werfer.Services;

public class DescriptionPrefixesTests
{
    [Theory]
    [InlineData("PROJ-12 - Fix login", "Fix login")]
    [InlineData("  [Feature] PROJ-12 - Fix login", "Fix login")]
    [InlineData("PROJ-12 - [FEATURE] Fix login", "Fix login")]
    [InlineData("Fix login", "Fix login")]
    [InlineData("  Fix login", "  Fix login")]
    [InlineData("PROJ-12 - ", "")]
    public void Strip_RemovesEveryLeadingPrefix_IgnoringCase(string pasted, string expected)
    {
        Assert.Equal(expected, DescriptionPrefixes.Strip(pasted, ["PROJ-12 -", "[Feature]", ""]));
    }

    [Fact]
    public void Strip_KeepsAPrefixInTheMiddle()
    {
        Assert.Equal("Fix [Feature] login", DescriptionPrefixes.Strip("Fix [Feature] login", ["[Feature]"]));
    }
}
