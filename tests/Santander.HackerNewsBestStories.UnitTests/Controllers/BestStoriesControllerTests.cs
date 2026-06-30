using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Santander.HackerNewsBestStories.Api.Controllers;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;
using Santander.HackerNewsBestStories.Domain.Entities;

namespace Santander.HackerNewsBestStories.UnitTests.Controllers;

public sealed class BestStoriesControllerTests
{
    [Fact]
    public async Task GetBestStories_ShouldReturnOkWithStories_WhenCountIsValid()
    {
        var expectedStories = new List<Story>
        {
            new(1, "A", "https://a.com", "u1", DateTime.UtcNow, 100, 10)
        };

        var service = Substitute.For<IHackerNewsService>();
        service.GetBestStoriesAsync(5, Arg.Any<CancellationToken>()).Returns(expectedStories);

        var controller = new BestStoriesController(service);
        var result = await controller.GetBestStories(5, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expectedStories);
    }

    [Fact]
    public async Task GetBestStories_ShouldReturnBadRequest_WhenCountIsZero()
    {
        var controller = new BestStoriesController(Substitute.For<IHackerNewsService>());
        var result = await controller.GetBestStories(0, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("count must be greater than zero");
    }

    [Fact]
    public async Task GetBestStories_ShouldReturnBadRequest_WhenCountIsNegative()
    {
        var controller = new BestStoriesController(Substitute.For<IHackerNewsService>());
        var result = await controller.GetBestStories(-1, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("count must be greater than zero");
    }

    [Fact]
    public async Task GetBestStories_ShouldPassCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var service = Substitute.For<IHackerNewsService>();
        service.GetBestStoriesAsync(3, ct).Returns([]);

        var controller = new BestStoriesController(service);
        await controller.GetBestStories(3, ct);

        await service.Received(1).GetBestStoriesAsync(3, ct);
    }
}
