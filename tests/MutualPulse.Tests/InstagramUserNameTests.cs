using MutualPulse.Desktop.Settings;
using Xunit;

namespace MutualPulse.Tests;

public sealed class InstagramUserNameTests
{
    [Theory]
    [InlineData("@Good_Name.2", "good_name.2")]
    [InlineData("user", "user")]
    public void AcceptsAccountNames(string input, string expected)
        => Assert.Equal(expected, InstagramUserName.Normalize(input));

    [Theory]
    [InlineData("https://www.instagram.com/example/")]
    [InlineData("name with spaces")]
    [InlineData("foo..bar")]
    [InlineData(".hidden")]
    public void RejectsIncorrectAccountName(string input)
        => Assert.Throws<ArgumentException>(() => InstagramUserName.Normalize(input));
}
