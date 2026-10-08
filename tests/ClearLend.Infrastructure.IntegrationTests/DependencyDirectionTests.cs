using System.Reflection;

namespace ClearLend.Infrastructure.IntegrationTests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void InfrastructureDoesNotReferenceApi()
    {
        var references = Assembly.Load("ClearLend.Infrastructure")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ClearLend.Api", references);
    }
}
