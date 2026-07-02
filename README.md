# HackerNews Best Stories API

REST API that returns the top stories from Hacker News, ordered by score.

```
GET /api/beststories/{count}
```

Response:

```json
[
  {
    "title": "Story Title",
    "uri": "https://example.com",
    "postedBy": "author",
    "time": "2024-01-01T00:00:00Z",
    "score": 100,
    "commentCount": 25
  }
]
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or
- [Docker](https://www.docker.com/)

## How to run

### With .NET SDK

```bash
dotnet run --project src/Santander.HackerNewsBestStories.Api
```

Open `http://localhost:5286/swagger` in your browser.

### With Docker

```bash
docker build -t hackernews-api .
docker run -p 8080:8080 hackernews-api
```

```bash
curl http://localhost:8080/api/beststories/5
```

### Tests

```bash
dotnet test
```

## Architecture

Clean Architecture with 4 layers:

- **Api** — Controllers, middleware, request validation
- **Application** — Service abstractions
- **Domain** — Entities (zero dependencies)
- **Infrastructure** — HTTP client, caching, resilience (Polly), settings

## Performance & reliability: servicing large requests without overloading Hacker News

The requirement: *"your API should be able to efficiently service large numbers of requests without risking overloading of the Hacker News API"* — addressed with four strategies:

### 1. Two-level in-memory caching

| Cache key | What it stores | TTL |
|---|---|---|
| `best_{n}` | Pre-computed result (top `n` stories, already sorted by score) | 60s |
| `item_{id}` | Individual story fetched from Hacker News | 300s |

**Why:** If 100 users request `/api/beststories/5` simultaneously, only the first one calls the Hacker News API. The other 99 read from memory — zero external requests. Individual items are cached separately so overlapping requests across different `n` values (e.g., `best_5` and `best_10`) reuse the same story data without duplicate API calls.

### 2. Bounded concurrency

```csharp
MaxDegreeOfParallelism = 10 // configurable via appsettings
```

**Why:** Fetching 200 stories one at a time is too slow. Fetching all 200 at once would overload the Hacker News API. By limiting to 10 concurrent requests, we balance speed against upstream load regardless of how many stories are requested.

### 3. Resilience policies (Polly)

| Policy | Behaviour | Why |
|---|---|---|
| **Retry** (2 attempts, exponential backoff: 100ms, 200ms) | Retries on transient HTTP errors or timeouts | Avoids flooding Hacker News on temporary failures |
| **Circuit breaker** (2 failures, 30s open) | Stops sending requests after 2 consecutive failures, waits 30s before retrying | Gives Hacker News time to recover if it's degraded |
| **Timeout** (10s per request) | Cancels any request that takes longer than 10s | Prevents resource exhaustion from slow upstream responses |

### 4. Validation before processing

`count` is validated (1–200) by FluentValidation before any API call or cache lookup — invalid requests are rejected immediately with `400 Bad Request`, consuming negligible resources.

## How it works

1. `GET /api/beststories/{count}` hits `BestStoriesController`
2. `count` is validated (1–200) via FluentValidation
3. `HackerNewsService` checks the cache (`best_{n}`)
4. On cache miss: fetches story IDs from `https://hacker-news.firebaseio.com/v0/beststories.json`
5. Fetches individual stories in parallel (max 10 concurrent), each cached individually (`item_{id}`)
6. Orders by score descending, takes top `count`, caches the result, and returns

## Assumptions

- Story order from Hacker News API is considered the "best stories" set; the API re-orders them by score
- If a story has no URL (`url` is null or empty), the Hacker News discussion link (`https://news.ycombinator.com/item?id={id}`) is used as fallback
- A count of up to 200 is reasonable; the Hacker News best stories endpoint typically returns ~200 IDs
- The API and Hacker News API communication may fail; resilience policies handle transient errors gracefully
- In-memory caching is sufficient for a single-instance deployment

## Enhancements given more time

- **Redis cache** — replace `IMemoryCache` for multi-instance deployments
- **Health checks** — `/health` endpoint for Kubernetes liveness/readiness probes
- **Rate limiting** — protect the API from abuse using `System.Threading.RateLimiting`
- **OpenTelemetry** — structured logging, metrics, and distributed tracing
- **API versioning** — `Accept` header or URL-based versioning for backward compatibility
- **Integration tests** — test the full pipeline against a real or containerized HN API mock
- **Background cache refresh** — proactively refresh the cache before it expires to reduce latency
- **ETag / conditional GET** — return `304 Not Modified` when the cached result hasn't changed
- **Response compression** — reduce bandwidth with `Accept-Encoding: gzip`
