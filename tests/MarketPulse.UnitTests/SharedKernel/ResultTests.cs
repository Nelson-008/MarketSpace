using MarketPulse.SharedKernel.Results;
using Shouldly;

namespace MarketPulse.UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_exposes_value()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_value_access_throws()
    {
        Result<int> result = Error.NotFound("thing.not_found", "Not found.");

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_without_error_is_not_allowed()
    {
        Should.Throw<ArgumentException>(() => Result.Failure(Error.None));
    }
}
