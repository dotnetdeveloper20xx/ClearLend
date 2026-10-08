namespace ClearLend.Api.IntegrationTests;

public sealed class ApiAssemblyTests
{
    [Fact]
    public void ApiAssemblyIsPresent()
    {
        Assert.Equal("ClearLend.Api", System.Reflection.Assembly.Load("ClearLend.Api").GetName().Name);
    }
}
