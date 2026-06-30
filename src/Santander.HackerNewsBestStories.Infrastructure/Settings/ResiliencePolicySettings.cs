using System.Diagnostics.CodeAnalysis;

namespace Santander.HackerNewsBestStories.Infrastructure.Settings;

[ExcludeFromCodeCoverage]
public sealed class ResiliencePolicySettings
{
    public const string SectionName = "ResiliencePolicy";

    public int TimeoutSeconds { get; init; } = 10;
    public int RetryCount { get; init; } = 2;
    public int RetryBaseDelayMs { get; init; } = 100;
    public int CircuitBreakerAllowedFailures { get; init; } = 2;
    public int CircuitBreakerDurationSeconds { get; init; } = 30;
    public int MaxConcurrency { get; init; } = 10;
}
