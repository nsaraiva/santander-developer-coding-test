using Microsoft.AspNetCore.Mvc;
using Santander.HackerNewsBestStories.Api.Models;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;

namespace Santander.HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BestStoriesController(IHackerNewsService service) : ControllerBase
{
    [HttpGet("{count:int}")]
    public async Task<IActionResult> GetBestStories(
        [FromRoute] BestStoriesRequest request, CancellationToken ct)
    {
        var stories = await service.GetBestStoriesAsync(request.Count, ct);
        return Ok(stories);
    }
}
