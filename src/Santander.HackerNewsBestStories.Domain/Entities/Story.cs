using System.Diagnostics.CodeAnalysis;

namespace Santander.HackerNewsBestStories.Domain.Entities;

[ExcludeFromCodeCoverage]
public sealed record Story(
    int Id,
    string Title,
    string Uri,
    string PostedBy,
    DateTime Time,
    int Score,
    int CommentCount
);
