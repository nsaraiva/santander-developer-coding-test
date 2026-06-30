using Microsoft.AspNetCore.Mvc;
using Santander.HackerNewsBestStories.Application.Abstractions.Services;

namespace Santander.HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BestStoriesController(IHackerNewsService service) : ControllerBase
{
    [HttpGet("{count:int}")]
    public async Task<IActionResult> GetBestStories(int count, CancellationToken ct)
    {
        if (count <= 0)
            return BadRequest("count must be greater than zero");

        var stories = await service.GetBestStoriesAsync(count, ct);
        return Ok(stories);
    }
}
