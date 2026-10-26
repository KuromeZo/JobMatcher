# Job Matcher

AI-powered job matching for IT positions in Poland. Fetches listings from JustJoinIT, scores each one against your CV using Claude, and explains what matches and what you'd need to learn.

**Live demo:** [jobmatcher-zanko.vercel.app](https://jobmatcher-zanko.vercel.app)

---

## What it does

Job boards let you filter by tag. That tells you a listing mentions C#, not whether you're actually a fit for it.

Job Matcher reads the full job description and compares it against your real skill set:

1. **Upload your CV** (`.docx`) — Claude parses it into a structured profile (skills, level, summary)
2. **Pick filters** — category (.NET, Java, JavaScript, Data, Python, DevOps) and experience level
3. **Get scored results** — each listing gets a 1–10 score, a list of skills you already match, a list of gaps you'd need to close, and a one-sentence verdict

Results stream in as they're scored, so you see matches appear one by one instead of waiting for the whole batch.

---

## Screenshots

**Dashboard** — filters, AI-scored jobs, and skill gaps

![Dashboard](docs/dashboard.png)

**Profile** — CV upload and parsed skill list

![Profile](docs/profile.png)

---

## Tech stack

**Backend** — ASP.NET Core (.NET 10)
- EF Core + PostgreSQL
- JWT authentication with BCrypt password hashing
- Claude Haiku for scoring and CV parsing
- `DocumentFormat.OpenXml` for `.docx` text extraction
- NDJSON streaming responses

**Frontend** — Angular 21
- Standalone components, signals, new control flow (`@if` / `@for`)
- PrimeNG (Aura dark theme)
- Reactive forms, JWT interceptor, route guards

**Infrastructure**
- Backend + PostgreSQL on Railway
- Frontend on Vercel
- Auto-deploy on push to `master`

---

## Architecture

```
JobMatcher/
├── JobMatcher.API/              ASP.NET Core backend
│   ├── Configuration/           Strongly typed options (validated at startup)
│   ├── Controllers/             HTTP layer only
│   ├── Data/                    EF Core DbContext and migrations
│   ├── Extensions/              Service registration (AI, job board, JWT)
│   ├── Infrastructure/          Exception handler, rate limit policies
│   ├── Models/
│   │   ├── Domain/              Business objects
│   │   ├── External/            DTOs for JustJoinIT, Claude and Gemini
│   │   └── Persistence/         EF entities
│   ├── Repositories/            Data access
│   └── Services/
│       ├── Ai/                  Scoring + CV parsing, Claude and Gemini clients
│       ├── JustJoinIt/          Job fetching
│       └── Interfaces/          Abstractions for DI
└── job-matcher-ui/              Angular frontend
    └── src/app/
        ├── dashboard/           Filters + results grid
        ├── job-card/            Single scored job
        ├── login/               Auth screen
        ├── profile-editor/      CV upload + profile editing
        └── services/            API client, auth, guards
```

### Design decisions

**AI provider is swappable.** `IAiScoringService` and `IAiCvParserService` abstract the AI backend. Scoring and CV parsing only depend on `ILlmClient`; Claude and Gemini clients both exist and the provider is picked by the `Ai:Provider` setting (`Claude` or `Gemini`). Claude Haiku is the default; it's roughly 20× cheaper than Sonnet and handles structured JSON output fine.

**CV text is extracted before it reaches the AI.** Rather than sending the file directly, `DocxTextExtractorService` pulls out plain text first. This keeps CV parsing provider-agnostic — different AI APIs handle file uploads differently, but they all take text.

**Scoring uses the full job body, not just skill tags.** The listing endpoint only returns tags, so each new offer triggers a second fetch by slug to get the full description. Scoring tags alone would be no better than the site's own filters.

**Job offers are shared, scores are per-user.** `JobOffers` is a common cache — fetching the same listing once serves everyone. `ScoredJobs` is keyed by `UserId`, since the same job scores differently against different profiles.

**Pagination stops early on repeats.** The fetcher sorts by newest and stops after 5 consecutive already-known offers, which cuts HTTP calls sharply on repeat runs. Offers missed by that early stop are recovered from the database by category and level.

**Scoring is parallel and cancellable.** Cached scores are streamed first. Offers that need scoring go through up to 4 parallel AI calls; results come back through a `Channel` and are saved to the database one at a time, since `DbContext` is not thread-safe. The request's `CancellationToken` flows down to `HttpClient` and EF Core, so closing the tab stops the AI calls.

**External calls are resilient.** HTTP clients use the standard resilience handler: retries with backoff on 429/5xx (honoring `Retry-After`), timeouts and a circuit breaker.

**The demo is protected.** Rate limits: 10 auth requests per minute per IP, 20 AI requests per hour per user. Errors are returned as `ProblemDetails`, and requests are validated before anything is fetched or scored.

**Offers live as long as on the job board.** Each offer stores JustJoinIT's `expiredAt`, which is refreshed whenever the offer shows up in a listing again (employers can extend it). Expired offers and their scores are deleted at the start of every run.

**Streaming is line-by-line.** Returning `IAsyncEnumerable` directly gets buffered by ASP.NET Core. The controller writes each NDJSON line and calls `Response.Body.FlushAsync()` explicitly so results actually arrive incrementally.

---

## API

```
POST /api/auth/register        Public — returns JWT
POST /api/auth/login           Public — returns JWT

POST /api/offers/score         Score with supplied profile and filters, sorted by score
POST /api/offers/score/stream  Same, streamed as NDJSON

POST /api/cv/upload            .docx → parsed CandidateProfile
```

All `/api/offers` and `/api/cv` endpoints require `Authorization: Bearer <token>`.

### Request body for scoring

```json
{
  "profile": {
    "skills": ["C#", "ASP.NET Core", "SQL"],
    "level": "junior",
    "description": "CS student, backend focus"
  },
  "filters": {
    "categories": ["net", "java"],
    "experienceLevels": ["junior", "mid"]
  },
  "minScore": 6,
  "forceRescore": false
}
```

---

## Running locally

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL (local instance or a hosted one)
- An [Anthropic API key](https://console.anthropic.com)

### Backend

Create `JobMatcher.API/appsettings.Development.json` — this file is gitignored:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=jobmatcher;Username=postgres;Password=yourpassword"
  },
  "ClaudeApi": {
    "ApiKey": "sk-ant-..."
  },
  "Jwt": {
    "Key": "at-least-32-characters-long-random-string",
    "Issuer": "JobMatcher",
    "Audience": "JobMatcherUsers"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

Then:

```bash
cd JobMatcher.API
dotnet run
```

Runs on `http://localhost:5212`. The database schema is created and kept up to date automatically on startup by EF Core migrations.

### Frontend

```bash
cd job-matcher-ui
npm install
npm start
```

Runs on `http://localhost:4200`. `proxy.conf.json` forwards `/api` to the backend, so no CORS setup is needed in development.

---

## Database migrations

Schema changes go through EF Core migrations in `JobMatcher.API/Data/Migrations`. `dotnet-ef` is pinned as a local tool:

```bash
dotnet tool restore
cd JobMatcher.API
dotnet dotnet-ef migrations add <Name> -o Data/Migrations
```

Pending migrations are applied when the API starts. Databases created by the old `EnsureCreated()` code are detected and marked as already having `InitialCreate`, so only the newer migrations run on them and the data is kept.

---

## Deployment

Both services auto-deploy on push to `master`.

**Backend (Railway)** — root directory `JobMatcher.API`, with these environment variables:

```
ConnectionStrings__DefaultConnection
ClaudeApi__ApiKey
Ai__Provider                   (optional, Claude by default)
Jwt__Key
Jwt__Issuer
Jwt__Audience
Cors__AllowedOrigins__0
```

ASP.NET Core maps `__` to `:`, so `Jwt__Key` fills `Jwt:Key`. Use Railway's internal hostname (`postgres.railway.internal`) in the connection string so traffic stays on the private network.

**Frontend (Vercel)** — root directory `job-matcher-ui`, build command `npm run build`, output `dist/job-matcher-ui/browser`. The production API URL lives in `src/environments/environment.prod.ts` and is swapped in at build time via `fileReplacements` in `angular.json`.

---

## Known limitations

- Changing your profile doesn't invalidate cached scores automatically — use the **Rescore** button to force a fresh pass.
- An offer closed early by the employer stays until its `expiredAt` date. Offers stored before expiry dates were tracked are deleted 30 days after they were fetched.
- The candidate profile lives in `localStorage`, keyed by user id. It doesn't sync across devices.

---

## License

MIT
