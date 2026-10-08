using System.Reflection;

namespace ClearLend.Application.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = Assembly.Load("ClearLend.Application")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ClearLend.Infrastructure", references);
        Assert.DoesNotContain("ClearLend.Api", references);
    }
}
