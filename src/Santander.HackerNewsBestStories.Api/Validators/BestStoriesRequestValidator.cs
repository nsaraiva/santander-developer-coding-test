using FluentValidation;
using Santander.HackerNewsBestStories.Api.Models;

namespace Santander.HackerNewsBestStories.Api.Validators;

public sealed class BestStoriesRequestValidator : AbstractValidator<BestStoriesRequest>
{
    public BestStoriesRequestValidator()
    {
        RuleFor(x => x.Count)
            .InclusiveBetween(1, 200)
            .WithMessage("count must be between 1 and 200");
    }
}
