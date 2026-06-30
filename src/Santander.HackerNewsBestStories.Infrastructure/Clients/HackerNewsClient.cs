using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Santander.HackerNewsBestStories.Infrastructure.Models;
using Santander.HackerNewsBestStories.Infrastructure.Settings;

namespace Santander.HackerNewsBestStories.Infrastructure.Clients;

public sealed class HackerNewsClient
{
    private readonly HttpClient _httpClient;
    private readonly HackerNewsApiSettings _settings;

    public HackerNewsClient(HttpClient httpClient, IOptions<HackerNewsApiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken ct)
    {
        var ids = await _httpClient
            .GetFromJsonAsync<int[]>(_settings.BestStoriesEndpoint, ct);

        return ids ?? [];
    }

    public async Task<HackerNewsItemDto?> GetStoryAsync(int id, CancellationToken ct)
    {
        var endpoint = string.Format(_settings.StoryEndpointTemplate, id);
        return await _httpClient.GetFromJsonAsync<HackerNewsItemDto>(endpoint, ct);
    }
}
