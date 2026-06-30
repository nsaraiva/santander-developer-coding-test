using Santander.HackerNewsBestStories.Domain.Entities;

namespace Santander.HackerNewsBestStories.Application.Abstractions.Services;

public interface IHackerNewsService
{
    Task<IReadOnlyList<Story>> GetBestStoriesAsync(int count, CancellationToken ct);
}
