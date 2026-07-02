using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Santander.HackerNewsBestStories.Infrastructure.Clients;
using Santander.HackerNewsBestStories.Infrastructure.Models;
using Santander.HackerNewsBestStories.Infrastructure.Settings;

namespace Santander.HackerNewsBestStories.UnitTests.Clients;

public sealed class HackerNewsClientTests
{
    private readonly HackerNewsApiSettings _settings = new()
    {
        BestStoriesEndpoint = "v0/beststories.json",
        StoryEndpointTemplate = "v0/item/{0}.json"
    };

    private HackerNewsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com/")
        };

        var options = Substitute.For<IOptions<HackerNewsApiSettings>>();
        options.Value.Returns(_settings);

        return new HackerNewsClient(httpClient, options);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ShouldReturnIds_WhenApiReturnsData()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/beststories.json", new[] { 1, 2, 3 });
        var client = CreateClient(handler);

        var result = await client.GetBestStoryIdsAsync(CancellationToken.None);

        result.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ShouldReturnEmpty_WhenApiReturnsNull()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddNullResponse("v0/beststories.json");
        var client = CreateClient(handler);

        var result = await client.GetBestStoryIdsAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoryAsync_ShouldReturnDto_WhenApiReturnsData()
    {
        var expected = new HackerNewsItemDto(1, "Test Title", "https://example.com", "author", 1234567890, 100, 50);
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse("v0/item/1.json", expected);
        var client = CreateClient(handler);

        var result = await client.GetStoryAsync(1, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetStoryAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddNullResponse("v0/item/999.json");
        var client = CreateClient(handler);

        var result = await client.GetStoryAsync(999, CancellationToken.None);

        result.Should().BeNull();
    }
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public int RequestCount { get; private set; }
    public IReadOnlyList<string> RequestedPaths => _requestedPaths;
    private readonly List<string> _requestedPaths = new();

    public void AddResponse<T>(string endpoint, T content)
    {
        var json = JsonSerializer.Serialize(content, _jsonOptions);
        _responses[endpoint] = json;
    }

    public void AddNullResponse(string endpoint)
    {
        _responses[endpoint] = "null";
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;

        var path = request.RequestUri?.PathAndQuery.TrimStart('/') ?? "";
        _requestedPaths.Add(path);

        if (_responses.TryGetValue(path, out var json))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
