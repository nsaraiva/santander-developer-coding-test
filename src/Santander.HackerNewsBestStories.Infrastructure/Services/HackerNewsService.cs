using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;
using Santander.HackerNewsBestStories.Domain.Entities;
using Santander.HackerNewsBestStories.Infrastructure.Clients;
using Santander.HackerNewsBestStories.Infrastructure.Models;
using Santander.HackerNewsBestStories.Infrastructure.Settings;

namespace Santander.HackerNewsBestStories.Infrastructure.Services;

public sealed class HackerNewsService(
    HackerNewsClient client,
    IMemoryCache cache,
    IOptions<CacheSettings> cacheOptions,
    IOptions<ResiliencePolicySettings> resilienceOptions) : IHackerNewsService
{
    private readonly CacheSettings _cacheSettings = cacheOptions.Value;
    private readonly ResiliencePolicySettings _resilienceSettings = resilienceOptions.Value;

    public async Task<IReadOnlyList<Story>> GetBestStoriesAsync(int count, CancellationToken ct)
    {
        var cacheKey = $"best_{count}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<Story>? cached))
            return cached!;

        var storyIds = await GetCachedStoryIdsAsync(ct);

        var stories = await FetchStoriesWithConcurrencyLimitAsync(storyIds, ct);

        var ordered = stories
            .OrderByDescending(s => s.Score)
            .Take(count)
            .ToList()
            .AsReadOnly();

        cache.Set(cacheKey, ordered, TimeSpan.FromSeconds(_cacheSettings.StoryListCacheDurationSeconds));

        return ordered;
    }

    private async Task<IReadOnlyList<int>> GetCachedStoryIdsAsync(CancellationToken ct)
    {
        const string idsCacheKey = "best_story_ids";

        if (cache.TryGetValue(idsCacheKey, out IReadOnlyList<int>? cachedIds))
            return cachedIds!;

        var ids = await client.GetBestStoryIdsAsync(ct);
        cache.Set(idsCacheKey, ids, TimeSpan.FromSeconds(_cacheSettings.StoryListCacheDurationSeconds));
        return ids;
    }

    private async Task<List<Story>> FetchStoriesWithConcurrencyLimitAsync(
        IReadOnlyList<int> storyIds, CancellationToken ct)
    {
        var stories = new List<Story>(storyIds.Count);
        var lockObj = new object();

        await Parallel.ForEachAsync(
            storyIds,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _resilienceSettings.MaxConcurrency,
                CancellationToken = ct
            },
            async (id, token) =>
            {
                var story = await GetCachedStoryItemAsync(id, token);
                if (story is not null)
                {
                    lock (lockObj)
                    {
                        stories.Add(story);
                    }
                }
            });

        return stories;
    }

    private async Task<Story?> GetCachedStoryItemAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"item_{id}";

        if (cache.TryGetValue(cacheKey, out Story? cached))
            return cached;

        var dto = await client.GetStoryAsync(id, ct);
        if (dto is null)
            return null;

        var story = MapToStory(dto);
        cache.Set(cacheKey, story, TimeSpan.FromSeconds(_cacheSettings.StoryItemCacheDurationSeconds));
        return story;
    }

    private static Story MapToStory(HackerNewsItemDto dto)
    {
        var uri = string.IsNullOrWhiteSpace(dto.Url)
            ? $"https://news.ycombinator.com/item?id={dto.Id}"
            : dto.Url;

        return new Story(
            dto.Id,
            dto.Title,
            uri,
            dto.By,
            DateTimeOffset.FromUnixTimeSeconds(dto.Time).UtcDateTime,
            dto.Score,
            dto.Descendants
        );
    }
}
