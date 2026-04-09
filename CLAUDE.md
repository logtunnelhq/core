# LogTunnel — Project Context for Claude Code

## What This Product Is

LogTunnel is an open-core SaaS tool that translates raw Git commit messages into
audience-specific changelog entries. One set of commits produces four outputs:
tech lead, manager, CEO, and public-facing. The AI translation layer uses
Semantic Kernel with the Claude API.

## Business Model

**Open source core** (AGPL license, GitHub public):
- Commit ingestion and parsing
- 4-audience translation engine
- Audience template configuration
- CLI tool
- Self-hostable minimal API

**Paid hosted platform** (private repo):
- GitHub/GitLab webhook automation
- Team collaboration and roles
- Public changelog page (logtunnel.so/changelog/product-name)
- Custom domains
- Viewer analytics
- Slack/email delivery
- Company context memory (vector store — the key retention feature)
- SSO/enterprise auth

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Language | C# 12 |
| AI orchestration | Microsoft Semantic Kernel |
| LLM | Claude API (claude-sonnet-4-20250514) |
| Database | PostgreSQL 16 |
| Vector store | pgvector extension |
| Cache | Redis |
| API style | Minimal API (no controllers) |
| Auth | ASP.NET Core Identity + JWT |
| Payments | Stripe Billing |
| Frontend | React 18 (lives in the platform repo, not here) |
| Hosting | Azure (developer is Azure-familiar) |
| License | AGPL for core, proprietary for platform |

## Project Structure

```
/src
  /LogTunnel.Core              — domain models, interfaces, business logic
  /LogTunnel.Infrastructure    — EF Core, repositories, external services
  /LogTunnel.Api               — minimal API endpoints
  /LogTunnel.Cli               — dotnet tool CLI
/tests
  /LogTunnel.Core.Tests
  /LogTunnel.Infrastructure.Tests
/docs
  /prompts                   — prompt templates as .md files
```

> **Post-split note.** `LogTunnel.Platform`, the React frontend
> under `web/`, and `tests/LogTunnel.Platform.Tests/` no longer
> live in this repo. They were moved to the private platform repo
> at **github.com/logtunnelhq/platform** in April 2026 and the
> matching paths were scrubbed from this repo's git history with
> `git filter-repo`. The core repo now contains only the open
> source components — translator engine, prompts, CLI, minimal
> API, and the data layer (EF Core entities + repositories) that
> the platform repo consumes via a git submodule.
>
> **Never commit `LogTunnel.Platform`, `web/`, or
> `LogTunnel.Platform.Tests` back into this repo.** Anything that
> turns the open-core primitives into a hosted SaaS — auth,
> dashboards, webhooks, marketing workflow, billing — belongs in
> the platform repo.

## Architecture Decisions — Do Not Change These

1. **Minimal API only** — no MVC controllers, no [ApiController] attributes
2. **Semantic Kernel is used in one place only** — ChangelogTranslatorService
3. **All prompts live in /docs/prompts/** as markdown files, not hardcoded strings
4. **No AI anywhere except the translator** — everything else is plain .NET
5. **Repository pattern** — no direct DbContext outside Infrastructure layer
6. **Result pattern** — return Result<T> not exceptions for expected failures
7. **No static classes** — everything injected via DI
8. **AGPL license header** on every file in the Core project

## The Core Domain Model

```csharp
// The central concept — one translation job
public record TranslationRequest(
    string RawCommits,
    CompanyContext Context,
    IReadOnlyList<AudienceConfig> Audiences
);

public record CompanyContext(
    string ProductDescription,
    string TargetCustomers,
    string Terminology, // e.g. "say members not users"
    string? AdditionalContext
);

public record AudienceConfig(
    AudienceType Type,      // TechLead | Manager | CEO | Public
    string Tone,            // e.g. "technical and direct"
    string Format,          // e.g. "bullet points, max 5 items"
    string? CustomInstructions
);

public record ChangelogOutput(
    Guid Id,
    DateTimeOffset GeneratedAt,
    IReadOnlyDictionary<AudienceType, string> Outputs
);
```

## The Four Audience Prompts — Current Thinking

Each audience gets a distinct system prompt. Prompt files live in /docs/prompts/:

- `tech-lead.md` — technical detail, PR references, breaking changes flagged
- `manager.md` — what shipped, business impact, risks, no jargon
- `ceo.md` — pure business language, 3 bullet points max, outcomes not features
- `public.md` — customer-facing, positive framing, features only not fixes

## Build Order

### Phase 1 — Open Source Core — COMPLETE
1. ✅ Domain models and interfaces in LogTunnel.Core
2. ✅ ChangelogTranslatorService with Semantic Kernel
3. ✅ Prompt templates for all four audiences
4. ✅ Basic minimal API — POST /translate, POST /configure, GET /health
5. ✅ CLI tool — logtunnel translate command
6. ✅ Unit tests for translator
7. ✅ README and documentation

### Phase 2 — Data Layer — COMPLETE in this repo
1. ✅ PostgreSQL schema (entities, EF Core configurations, migrations)
2. ✅ Repository interfaces in LogTunnel.Core/Domain/Interfaces
3. ✅ Repository implementations in LogTunnel.Infrastructure
4. ✅ Testcontainers integration tests in LogTunnel.Infrastructure.Tests

### Phase 2 — Hosted Platform Code — moved to the platform repo
The following items were built but no longer live in this repo. They
ship from **github.com/logtunnelhq/platform**, which consumes this
repo as a git submodule:

- User accounts, JWT auth, refresh-token rotation
- Role-based authorization policies
- Admin endpoints (teams, projects, repositories, users)
- Daily log endpoints under /me/*
- Stand-up views (team / project / org)
- Multi-host webhook handlers (GitHub, GitLab, Azure DevOps)
- Translation worker background service
- Hourly daily-log freeze job
- Marketing public-translation workflow
- Stand-up export endpoint (Slack/Teams/copy/email)
- Public changelog endpoint
- React 18 + TypeScript dashboard (login, 6 role pages, admin panel,
  public changelog page)

### Phase 3 — Stripe Billing — planned, in the platform repo
Per-tenant subscription, webhook handler, quota enforcement,
customer portal. The `subscriptions` table will land here in core
(it's part of the data layer); the webhook handler + quota
middleware land in the platform repo.

## What NOT To Build Yet

- No multi-language support
- No mobile app
- No browser extension
- No GitLab support (GitHub only first)
- No AI features beyond translation (no summaries, no suggestions)
- No social features beyond the public feed
- No white-label until $5k MRR

## Coding Conventions

- Async all the way — no sync over async
- Cancellation tokens on every async method
- XML doc comments on all public interfaces
- No magic strings — use constants or enums
- Serilog for structured logging
- FluentValidation for input validation
- No Automapper — explicit mapping methods
- Guard clauses at top of methods, not nested ifs

## Environment Variables Expected

The LLM connector is swappable at runtime via the `Llm` configuration
section. Set `LLM__PROVIDER` to one of `Anthropic`, `OpenAI`, or
`Ollama`; `LLM__APIKEY` is required for Anthropic and OpenAI but
ignored for Ollama. `LLM__BASEURL` is optional — supply it for
OpenAI-compatible endpoints (Azure OpenAI, vLLM, OpenRouter, etc.) or
to point at a non-default Ollama instance. Unknown providers fail
startup with a clear error.

```
LLM__PROVIDER=Anthropic           # or OpenAI, Ollama
LLM__MODEL=claude-sonnet-4-20250514
LLM__APIKEY=
LLM__BASEURL=                     # optional override
CONNECTIONSTRINGS__POSTGRES=
CONNECTIONSTRINGS__REDIS=
STRIPE__SECRETKEY=
STRIPE__WEBHOOKSECRET=
GITHUB__WEBHOOKSECRET=
JWT__SECRET=
JWT__ISSUER=
JWT__AUDIENCE=
```

## Current Status

- **Phase 1 (translator engine, prompts, CLI, minimal API)** —
  COMPLETE (59 commits in this repo).
- **Phase 2 data layer (entities, repositories, EF Core migrations,
  Testcontainers integration tests)** — COMPLETE in this repo.
- **Phase 2 hosted-platform code + Phase 3 React frontend** —
  COMPLETE in the **platform repo**:
  **github.com/logtunnelhq/platform** (private, consumes this repo
  as a git submodule).
- **Phase 3 (Stripe billing)** — next, planned in the platform repo.
  Schema additions for the `subscriptions` table will land here in
  core when the work begins.
