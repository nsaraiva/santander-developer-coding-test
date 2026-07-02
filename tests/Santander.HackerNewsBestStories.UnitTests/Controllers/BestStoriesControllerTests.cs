using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Santander.HackerNewsBestStories.Api.Controllers;
using Santander.HackerNewsBestStories.Api.Models;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;
using Santander.HackerNewsBestStories.Domain.Entities;

namespace Santander.HackerNewsBestStories.UnitTests.Controllers;

public sealed class BestStoriesControllerTests
{
    [Fact]
    public async Task GetBestStories_ShouldReturnOkWithStories()
    {
        var expectedStories = new List<Story>
        {
            new(1, "A", "https://a.com", "u1", DateTime.UtcNow, 100, 10)
        };

        var service = Substitute.For<IHackerNewsService>();
        service.GetBestStoriesAsync(5, Arg.Any<CancellationToken>()).Returns(expectedStories);

        var controller = new BestStoriesController(service);
        var result = await controller.GetBestStories(
            new BestStoriesRequest(5), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expectedStories);
    }
}
