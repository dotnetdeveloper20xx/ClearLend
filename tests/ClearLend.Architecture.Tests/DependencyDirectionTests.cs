using System.Reflection;

namespace ClearLend.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterClearLendProjects()
    {
        var references = ReferencedProjectNames(typeof(ClearLend.Domain.Common.DomainResult).Assembly);

        Assert.DoesNotContain("ClearLend.Application", references);
        Assert.DoesNotContain("ClearLend.Infrastructure", references);
        Assert.DoesNotContain("ClearLend.Api", references);
        Assert.DoesNotContain("ClearLend.Contracts", references);
    }

    private static HashSet<string> ReferencedProjectNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("ClearLend.", StringComparison.Ordinal) == true)
            .Select(reference => reference.Name!)
            .ToHashSet(StringComparer.Ordinal);
}
