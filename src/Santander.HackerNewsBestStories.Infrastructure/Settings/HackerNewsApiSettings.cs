using System.Diagnostics.CodeAnalysis;

namespace Santander.HackerNewsBestStories.Infrastructure.Settings;

[ExcludeFromCodeCoverage]
public sealed class HackerNewsApiSettings
{
    public const string SectionName = "HackerNewsApi";

    public string BaseUrl { get; init; } = "https://hacker-news.firebaseio.com/";
    public string BestStoriesEndpoint { get; init; } = "v0/beststories.json";
    public string StoryEndpointTemplate { get; init; } = "v0/item/{0}.json";
    public int TimeoutSeconds { get; init; } = 10;
    public int MaxConcurrency { get; init; } = 10;
}
