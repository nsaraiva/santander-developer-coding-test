using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Santander.HackerNewsBestStories.Api.Middleware;

namespace Santander.HackerNewsBestStories.UnitTests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        return new ExceptionHandlingMiddleware(next, logger);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoException()
    {
        var called = false;
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn502_WhenHttpRequestException()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
            throw new HttpRequestException("upstream error"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn504_WhenTaskCanceledException()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
            throw new TaskCanceledException("timeout"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(504);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn502_WhenJsonException()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
            throw new JsonException("invalid json"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_WhenGenericException()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
            throw new InvalidOperationException("unexpected"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnProblemDetailsJson()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        var middleware = CreateMiddleware(_ =>
            throw new InvalidOperationException("test"));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Internal Server Error");
        problem.Status.Should().Be(500);
        problem.Detail.Should().Be("An unexpected error occurred.");
        problem.Instance.Should().Be("/api/test");
    }
}
