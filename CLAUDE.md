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
| Frontend | React 18 (minimal, only for hosted platform) |
| Hosting | Azure (developer is Azure-familiar) |
| License | AGPL for core, proprietary for platform |

## Project Structure

```
/src
  /LogTunnel.Core              — domain models, interfaces, business logic
  /LogTunnel.Infrastructure    — EF Core, repositories, external services
  /LogTunnel.Api               — minimal API endpoints
  /LogTunnel.Cli               — dotnet tool CLI
  /LogTunnel.Platform          — hosted platform features (paid tier)
/tests
  /LogTunnel.Core.Tests
  /LogTunnel.Infrastructure.Tests
  /LogTunnel.Api.Tests
/docs
  /prompts                   — prompt templates as .md files
```

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

## Build Order — Follow This Exactly

### Phase 1 — Open Source Core (build this first)
1. Domain models and interfaces in LogTunnel.Core
2. ChangelogTranslatorService with Semantic Kernel
3. Prompt templates for all four audiences
4. Basic minimal API — POST /translate, POST /configure, GET /health
5. CLI tool — logtunnel translate command
6. Unit tests for translator
7. README and documentation

### Phase 2 — Hosted Platform (only after Phase 1 ships)
1. PostgreSQL schema and EF Core setup
2. User accounts and JWT auth
3. Project management (CRUD)
4. GitHub webhook handler
5. Company context memory with pgvector
6. Public changelog page (React)
7. Stripe integration
8. Slack/email delivery

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

Phase 1 not yet started. Starting from scratch.
First task: scaffold the solution structure and implement ChangelogTranslatorService.
