using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Santander.HackerNewsBestStories.Domain.Entities;
using Santander.HackerNewsBestStories.Infrastructure.Clients;
using Santander.HackerNewsBestStories.Infrastructure.Models;
using Santander.HackerNewsBestStories.Infrastructure.Services;
using Santander.HackerNewsBestStories.Infrastructure.Settings;
using Santander.HackerNewsBestStories.UnitTests.Clients;

namespace Santander.HackerNewsBestStories.UnitTests.Services;

public sealed class HackerNewsServiceTests : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private HackerNewsService CreateService(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com/")
        };

        var apiOptions = Substitute.For<IOptions<HackerNewsApiSettings>>();
        apiOptions.Value.Returns(new HackerNewsApiSettings
        {
            BaseUrl = "https://hacker-news.firebaseio.com/",
            BestStoriesEndpoint = "v0/beststories.json",
            StoryEndpointTemplate = "v0/item/{0}.json"
        });

        var client = new HackerNewsClient(httpClient, apiOptions);

        var cacheOptions = Substitute.For<IOptions<CacheSettings>>();
        cacheOptions.Value.Returns(new CacheSettings
        {
            StoryListCacheDurationSeconds = 60,
            StoryItemCacheDurationSeconds = 300
        });

        var resilienceOptions = Substitute.For<IOptions<ResiliencePolicySettings>>();
        resilienceOptions.Value.Returns(new ResiliencePolicySettings { MaxConcurrency = 10 });

        return new HackerNewsService(client, _cache, cacheOptions, resilienceOptions);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnTopNOrderedByScoreDescending()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1, 2, 3, 4, 5 });
        handler.AddResponse("v0/item/1.json", Dto(1, "Story A", "https://a.com", 50));
        handler.AddResponse("v0/item/2.json", Dto(2, "Story B", "https://b.com", 100));
        handler.AddResponse("v0/item/3.json", Dto(3, "Story C", "https://c.com", 75));
        handler.AddResponse("v0/item/4.json", Dto(4, "Story D", "https://d.com", 200));
        handler.AddResponse("v0/item/5.json", Dto(5, "Story E", "https://e.com", 10));

        var service = CreateService(handler);
        var result = await service.GetBestStoriesAsync(3, CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Score.Should().Be(200);
        result[1].Score.Should().Be(100);
        result[2].Score.Should().Be(75);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnCachedResultOnSecondCall()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1 });
        handler.AddResponse("v0/item/1.json", Dto(1, "Story", "https://a.com", 100));

        var service = CreateService(handler);

        var first = await service.GetBestStoriesAsync(1, CancellationToken.None);
        var firstRequestCount = handler.RequestCount;

        var second = await service.GetBestStoriesAsync(1, CancellationToken.None);

        second.Should().BeEquivalentTo(first);
        handler.RequestCount.Should().Be(firstRequestCount);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldMapAllFieldsCorrectly()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1 });
        handler.AddResponse("v0/item/1.json", Dto(1, "Test Title", "https://example.com/1", 150, "testuser", 1234567890, 25));

        var service = CreateService(handler);
        var result = await service.GetBestStoriesAsync(1, CancellationToken.None);

        var story = result.Single();
        story.Id.Should().Be(1);
        story.Title.Should().Be("Test Title");
        story.Uri.Should().Be("https://example.com/1");
        story.PostedBy.Should().Be("testuser");
        story.Score.Should().Be(150);
        story.CommentCount.Should().Be(25);
        story.Time.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1234567890).UtcDateTime);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldUseHnFallbackUri_WhenUrlIsEmpty()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1 });
        handler.AddResponse("v0/item/1.json", Dto(1, "Ask HN", "", 100));

        var service = CreateService(handler);
        var result = await service.GetBestStoriesAsync(1, CancellationToken.None);

        result.Single().Uri.Should().Be("https://news.ycombinator.com/item?id=1");
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldCacheStoryItemsAcrossCalls()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1, 2 });
        handler.AddResponse("v0/item/1.json", Dto(1, "Story A", "https://a.com", 100));
        handler.AddResponse("v0/item/2.json", Dto(2, "Story B", "https://b.com", 50));

        var service = CreateService(handler);

        await service.GetBestStoriesAsync(2, CancellationToken.None);
        var requestCountAfterFirstCall = handler.RequestCount;

        await service.GetBestStoriesAsync(2, CancellationToken.None);

        handler.RequestCount.Should().Be(requestCountAfterFirstCall);
    }

    private static HackerNewsItemDto Dto(int id, string title, string url, int score,
        string by = "author", long time = 1000000, int descendants = 0)
    {
        return new HackerNewsItemDto(id, title, url, by, time, score, descendants);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
