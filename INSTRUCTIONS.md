# HackerNews Best Stories API - Project Instructions

## Test Requirements
- RESTful API with ASP.NET Core
- Endpoint: `GET /api/best-stories?n={n}` returns top `n` stories from Hacker News
- Source IDs: `https://hacker-news.firebaseio.com/v0/beststories.json`
- Source details: `https://hacker-news.firebaseio.com/v0/item/{id}.json`
- Response: JSON array sorted by score (descending) with fields: title, uri, postedBy, time, score, commentCount
- Must handle high load without overloading Hacker News API

## Architecture
Clean Architecture with 4 layers:
- **Api**: controllers, configuration, startup
- **Application**: use cases (CQRS-light), abstractions
- **Domain**: entities, zero external dependencies
- **Infrastructure**: HttpClient, caching, resilience (Polly)

## Technical Decisions
- **Cache**: IMemoryCache with two levels
  - Computed result (`best_{n}`): 60s TTL
  - Individual item (`item_{id}`): 300s TTL
- **Concurrency**: Parallel.ForEachAsync with MaxDegreeOfParallelism=10
- **Resilience**: Polly retry (2x, exponential backoff), timeout (5s), circuit breaker
- **Validation**: FluentValidation for `n` (1..200)
- **Testing**: xUnit + FluentAssertions + NSubstitute (mocks)
- **Coverage**: Classes without logic marked with `[ExcludeFromCodeCoverage]`

## Language
All code, variables, methods, classes, branches, commits, and PRs in **English**.

## Git Workflow
- `main` branch: stable releases
- `develop` branch: integration
- Feature branches: `feature/<feature-name>` created from `develop`
- After each feature: commit, push, create PR, merge to `develop`
- Commit messages in English

## Feature Plan (Order)

| # | Feature | Description |
|---|---------|-------------|
| 1 | Domain + Infrastructure Client | Story entity, HackerNewsItemDto, HackerNewsClient, project references, NuGet packages |
| 2 | HackerNews Service | Caching, concurrency control, Polly resilience, DI registration |
| 3 | Application Layer | IQueryHandler, GetBestStories use case (query, handler, response, validator, DI) |
| 4 | API Layer | BestStoriesController, Program.cs wiring, integration |
| 5 | Tests | Unit tests (handler, validator, service) + integration tests (controller) |
| 6 | Docker + README | Dockerfile, README.md with run instructions |

## Folder Structure
```
src/
├── Santander.HackerNewsBestStories.Api/
│   ├── Controllers/
│   │   └── BestStoriesController.cs
│   ├── Program.cs
│   └── appsettings.json
├── Santander.HackerNewsBestStories.Application/
│   ├── Abstractions/
│   │   ├── IQueryHandler.cs
│   │   └── Services/
│   │       └── IHackerNewsService.cs
│   ├── UseCases/GetBestStories/
│   │   ├── GetBestStoriesQuery.cs
│   │   ├── GetBestStoriesHandler.cs
│   │   ├── GetBestStoriesResponse.cs
│   │   └── GetBestStoriesValidator.cs
│   └── DependencyInjection/
│       └── ApplicationModule.cs
├── Santander.HackerNewsBestStories.Domain/
│   └── Entities/
│       └── Story.cs
└── Santander.HackerNewsBestStories.Infrastructure/
    ├── Clients/
    │   └── HackerNewsClient.cs
    ├── Services/
    │   └── HackerNewsService.cs
    ├── Models/
    │   └── HackerNewsItemDto.cs
    └── DependencyInjection/
        └── InfrastructureModule.cs

tests/
├── Santander.HackerNewsBestStories.UnitTests/
└── Santander.HackerNewsBestStories.IntegrationTests/
```

## Application Flow
1. Controller receives `GET /api/best-stories?n=10`
2. Validates `n` (1..200)
3. Handler checks cache (`best_{n}`)
4. Cache miss → fetches IDs via HackerNewsClient
5. Fetches details in parallel (max 10 concurrent), with individual cache (`item_{id}`)
6. Sorts by score descending
7. Takes top `n`
8. Caches result and returns

## Future Improvements (README)
- Redis for distributed cache
- Background cache refresh
- Metrics (OpenTelemetry/Prometheus)
- Health checks
- Rate limiting
- Load tests
