using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Santander.HackerNewsBestStories.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed");
            await WriteProblemDetailsAsync(context, 502, "Bad Gateway", "The upstream API request failed.");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timed out");
            await WriteProblemDetailsAsync(context, 504, "Gateway Timeout", "The upstream API request timed out.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON response");
            await WriteProblemDetailsAsync(context, 502, "Bad Gateway", "Invalid response received from upstream API.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetailsAsync(context, 500, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
