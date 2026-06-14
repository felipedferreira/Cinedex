namespace Movies.WebService.IntegrationTests;

public sealed class SmokeTests
{
    [Fact]
    public void Addition_ReturnsExpectedSum()
    {
        var result = 2 + 2;

        Assert.Equal(4, result);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 3, 5)]
    [InlineData(10, 5, 15)]
    public void Addition_IsCommutativeAndCorrect(int a, int b, int expected)
    {
        Assert.Equal(expected, a + b);
        Assert.Equal(expected, b + a);
    }
}
