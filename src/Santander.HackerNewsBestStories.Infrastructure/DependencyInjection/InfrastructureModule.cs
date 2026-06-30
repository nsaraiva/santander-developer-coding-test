using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;
using Santander.HackerNewsBestStories.Infrastructure.Clients;
using Santander.HackerNewsBestStories.Infrastructure.Services;
using Santander.HackerNewsBestStories.Infrastructure.Settings;

namespace Santander.HackerNewsBestStories.Infrastructure.DependencyInjection;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.Configure<HackerNewsApiSettings>(
            configuration.GetSection(HackerNewsApiSettings.SectionName));

        services.Configure<CacheSettings>(
            configuration.GetSection(CacheSettings.SectionName));

        services.Configure<ResiliencePolicySettings>(
            configuration.GetSection(ResiliencePolicySettings.SectionName));

        var resilienceSettings = configuration
            .GetSection(ResiliencePolicySettings.SectionName)
            .Get<ResiliencePolicySettings>() ?? new ResiliencePolicySettings();

        services.AddHttpClient<HackerNewsClient>((sp, client) =>
        {
            var apiSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HackerNewsApiSettings>>();
            client.BaseAddress = new Uri(apiSettings.Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(resilienceSettings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(resilienceSettings))
        .AddPolicyHandler(GetCircuitBreakerPolicy(resilienceSettings));

        services.AddSingleton<IHackerNewsService, HackerNewsService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResiliencePolicySettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                settings.RetryCount,
                attempt => TimeSpan.FromMilliseconds(
                    settings.RetryBaseDelayMs * Math.Pow(2, attempt - 1)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResiliencePolicySettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                settings.CircuitBreakerAllowedFailures,
                TimeSpan.FromSeconds(settings.CircuitBreakerDurationSeconds));
    }
}
