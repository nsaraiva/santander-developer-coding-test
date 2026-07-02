using System.Diagnostics.CodeAnalysis;

namespace Santander.HackerNewsBestStories.Infrastructure.Settings;

[ExcludeFromCodeCoverage]
public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public int StoryListCacheDurationSeconds { get; init; } = 60;
    public int StoryItemCacheDurationSeconds { get; init; } = 300;
}
