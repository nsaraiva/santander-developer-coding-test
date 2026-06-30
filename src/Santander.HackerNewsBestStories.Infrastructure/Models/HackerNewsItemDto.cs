using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Santander.HackerNewsBestStories.Infrastructure.Models;

[ExcludeFromCodeCoverage]
public sealed record HackerNewsItemDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("by")] string By,
    [property: JsonPropertyName("time")] long Time,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("descendants")] int Descendants
);
