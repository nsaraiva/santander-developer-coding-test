using FluentAssertions;
using FluentValidation.TestHelper;
using Santander.HackerNewsBestStories.Api.Models;
using Santander.HackerNewsBestStories.Api.Validators;

namespace Santander.HackerNewsBestStories.UnitTests.Validators;

public sealed class BestStoriesRequestValidatorTests
{
    private readonly BestStoriesRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenCountIsZero()
    {
        var request = new BestStoriesRequest(0);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void Should_HaveError_WhenCountIsNegative()
    {
        var request = new BestStoriesRequest(-1);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void Should_HaveError_WhenCountIsGreaterThan200()
    {
        var request = new BestStoriesRequest(201);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void Should_NotHaveError_WhenCountIs1()
    {
        var request = new BestStoriesRequest(1);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void Should_NotHaveError_WhenCountIs200()
    {
        var request = new BestStoriesRequest(200);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void Should_NotHaveError_WhenCountIsReasonable()
    {
        var request = new BestStoriesRequest(10);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }
}
