using ClearLend.Domain.Common;

namespace ClearLend.Domain.Tests;

internal static class TestResult
{
    public static T Get<T>(DomainResult<T> result)
    {
        if (result.TryGetValue(out var value))
        {
            return value;
        }

        Assert.Fail($"Expected success but received {result.Error?.Code}: {result.Error?.Message}");
        return default!;
    }
}
