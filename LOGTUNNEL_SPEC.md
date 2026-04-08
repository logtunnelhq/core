# LogTunnel — Full Project Specification

**Version:** 1.0  
**Date:** April 2026  
**Status:** Pre-build — validated concept, ready to execute

---

## 1. The Problem

Software teams ship code every week. The people who need to know what changed —
managers, customers, executives, investors — never hear about it clearly.

Developers write terse commit messages for themselves:
```
fix: null ref in payment handler
feat: idempotency key on order creation
refactor: extract discount calculation service
```

A product manager needs to write:
- A customer newsletter explaining new features
- An investor update showing product progress
- An internal team briefing for support staff
- An executive summary for the CEO

Currently this requires the PM to interrupt developers, guess at meaning, or skip
it entirely. The result is stakeholders who are permanently out of the loop.

**No tool solves this specific translation problem well.** Existing changelog tools
(Headwayapp, Beamer, Announcekit) solve publishing. None solve the translation
from developer language to stakeholder language.

---

## 2. The Solution

LogTunnel takes raw commit messages and company context as input and produces four
distinct, audience-appropriate changelog entries as output.

**Input:**
- Raw Git commits or PR descriptions (pasted or via webhook)
- Company context — what the product does, who it serves, terminology rules
- Per-audience configuration — tone, format, length constraints

**Output — four simultaneous versions:**

| Audience | Tone | Focus |
|---|---|---|
| Tech lead | Technical, precise | All changes, PR refs, breaking changes |
| Manager | Clear, business-aware | What shipped, risks, impact on team |
| CEO | Plain English | Business outcomes only, 3 bullets max |
| Public | Positive, customer-facing | Features and improvements, no bug talk |

**The AI does the translation. The human controls the templates.**

This distinction is critical. The output sounds like the company because the
company defines the templates. AI is invisible infrastructure, not the pitch.

---

## 3. Business Model — Open Core

### Open Source (AGPL)

The core translation engine is free, open source, and self-hostable. This is
the trust and distribution mechanism. Developers can run it locally or on their
own server with their own API key.

**What is open:**
- Commit ingestion and parsing
- 4-audience translation engine
- Audience template configuration system
- CLI tool (`dotnet tool install -g logtunnel`)
- Minimal self-hosted API
- Full documentation

### Paid Hosted Platform

The hosted version adds convenience, automation, and collaboration that makes
self-hosting not worth the overhead for teams.

**Pricing:**

| Tier | Price | Key Features |
|---|---|---|
| Starter | $19/month | 3 projects, webhook, public page |
| Team | $49/month | Unlimited projects, analytics, Slack delivery |
| Company | $149/month | Custom domain, SSO, priority support |

**What stays paid:**
- GitHub/GitLab webhook — auto-generates on every merge
- Team collaboration and approval workflows
- Public changelog page (logtunnel.so/changelog/product)
- Custom domains
- Viewer analytics — who read what, how far
- Company context memory — AI learns product terminology over time
- Slack and email delivery
- SSO (Azure AD, Okta, Google Workspace)

**License:** AGPL for core — prevents competitors from hosting your code
commercially without open sourcing their modifications.

---

## 4. Market

**Primary target:** Software teams of 5–50 people who ship regularly and have
non-technical stakeholders who need to stay informed.

**Geographic target:** Global — fully online, no sales calls, card payment.
Tool sells itself. Geographic location of builder is irrelevant.

**Competitors and why the gap exists:**

| Tool | What they do | Gap |
|---|---|---|
| Headwayapp | Publish changelogs | No translation — you still write it |
| Beamer | In-app changelog widget | No translation — you still write it |
| Announcekit | Public changelog hosting | No translation — you still write it |
| Linear / GitHub Releases | Basic release notes | Developer-facing only |
| Notion / Confluence | General wikis | Not purpose-built for changelogs |

None of them translate developer language into stakeholder language. That gap
is the product.

---

## 5. Tech Stack

### Core (Open Source)

```
.NET 8                        — runtime
C# 12                         — language
Microsoft Semantic Kernel      — AI orchestration (one service only)
Claude API                     — LLM (claude-sonnet-4-20250514)
Minimal API                    — HTTP layer (no controllers)
```

### Platform (Paid)

```
PostgreSQL 16 + pgvector       — relational store + vector memory
Redis                          — caching and sessions
ASP.NET Core Identity          — user management
JWT                            — authentication
Stripe Billing                 — subscriptions and payments
React 18                       — minimal frontend (changelog pages only)
Azure                          — hosting
```

---

## 6. Architecture

### Solution Structure

```
LogTunnel/
├── src/
│   ├── LogTunnel.Core/
│   │   ├── Domain/
│   │   │   ├── Models/          — TranslationRequest, ChangelogOutput, etc.
│   │   │   ├── Enums/           — AudienceType, SubscriptionTier, etc.
│   │   │   └── Interfaces/      — IChangelogTranslator, IProjectRepository, etc.
│   │   └── Services/
│   │       └── ChangelogTranslatorService.cs   — THE core service
│   │
│   ├── LogTunnel.Infrastructure/
│   │   ├── Data/                — EF Core DbContext, migrations
│   │   ├── Repositories/        — IProjectRepository implementations
│   │   ├── Memory/              — pgvector memory store integration
│   │   └── External/            — GitHub, Slack, Stripe clients
│   │
│   ├── LogTunnel.Api/
│   │   ├── Endpoints/           — minimal API endpoint definitions
│   │   ├── Validators/          — FluentValidation validators
│   │   └── Program.cs
│   │
│   ├── LogTunnel.Cli/
│   │   └── Commands/            — translate, configure commands
│   │
│   └── LogTunnel.Platform/        — paid platform features
│       ├── Webhooks/            — GitHub webhook handler
│       ├── Delivery/            — Slack, email delivery
│       └── Memory/              — company context accumulation
│
├── tests/
│   ├── LogTunnel.Core.Tests/
│   ├── LogTunnel.Infrastructure.Tests/
│   └── LogTunnel.Api.Tests/
│
├── docs/
│   └── prompts/
│       ├── tech-lead.md
│       ├── manager.md
│       ├── ceo.md
│       └── public.md
│
├── CLAUDE.md                    — Claude Code permanent context
└── README.md
```

### Key Architectural Decisions

**1. Semantic Kernel in one place only**

ChangelogTranslatorService is the only class that touches Semantic Kernel or
the LLM API. Everything else in the system is plain .NET. This keeps the AI
dependency isolated and testable.

**2. Prompts as files, not strings**

All four audience prompts live in /docs/prompts/ as markdown files. They are
loaded at startup and injected. This means prompt changes do not require
recompilation and can be iterated without touching C# code.

**3. Result pattern, not exceptions**

Public methods return Result<T> for expected failure paths. Exceptions are for
truly unexpected failures only. This makes error handling explicit and testable.

**4. Company context memory is the moat**

The pgvector-backed memory store accumulates company-specific context over time.
The longer a team uses the hosted platform, the more accurately the AI
understands their product, their terminology, and their style. This context
cannot be transferred to a self-hosted setup without significant effort.
It is the primary retention mechanism.

---

## 7. Core Domain Models

```csharp
public record TranslationRequest(
    string RawCommits,
    CompanyContext Context,
    IReadOnlyList<AudienceConfig> Audiences,
    CancellationToken CancellationToken = default
);

public record CompanyContext(
    string ProductDescription,
    string TargetCustomers,
    string Terminology,
    string? AdditionalContext = null
);

public record AudienceConfig(
    AudienceType Type,
    string Tone,
    string Format,
    string? CustomInstructions = null
);

public enum AudienceType
{
    TechLead,
    Manager,
    CEO,
    Public
}

public record ChangelogOutput(
    Guid Id,
    DateTimeOffset GeneratedAt,
    IReadOnlyDictionary<AudienceType, string> Outputs
);

public record Result<T>(
    bool IsSuccess,
    T? Value,
    string? Error = null
)
{
    public static Result<T> Success(T value) => new(true, value);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

---

## 8. The API (Open Source)

Three endpoints. Nothing more in Phase 1.

```
POST /translate
  Body: { rawCommits, context, audiences }
  Returns: { id, generatedAt, outputs: { techLead, manager, ceo, public } }

POST /configure
  Body: { companyContext, audienceConfigs }
  Returns: { configId }
  Saves config to local file for CLI use

GET /health
  Returns: { status: "healthy", version: "x.x.x" }
```

---

## 9. The CLI (Open Source)

```bash
# Install
dotnet tool install -g logtunnel

# First time setup — saves .logtunnel.json in repo root
logtunnel configure

# Translate commits from a file
logtunnel translate --input commits.txt

# Translate commits from git log directly
git log --oneline -20 | logtunnel translate

# Translate and output to files
logtunnel translate --input commits.txt --output ./changelogs/

# Translate specific audience only
logtunnel translate --input commits.txt --audience ceo
```

Config file format (`.logtunnel.json` in repo root):

```json
{
  "context": {
    "productDescription": "B2B invoicing tool for freelancers",
    "targetCustomers": "Freelance designers and developers",
    "terminology": "Say members not users. Say workspace not account."
  },
  "audiences": [
    {
      "type": "TechLead",
      "tone": "Technical and direct",
      "format": "Bullet points with PR references"
    },
    {
      "type": "CEO",
      "tone": "Business focused, plain English",
      "format": "Maximum 3 bullet points. Outcomes only."
    }
  ]
}
```

---

## 10. Prompt Design

Each audience prompt is a system prompt stored as a markdown file. The structure:

```markdown
# Tech Lead Changelog Prompt

You are writing a changelog entry for the engineering team's tech lead.

## Audience
Senior developers who want full technical context. They care about:
- What exactly changed and why
- Any breaking changes or migration required
- Performance implications
- Dependencies affected

## Format
- Bullet point list
- Include PR/commit references where available
- Flag breaking changes with [BREAKING] prefix
- Flag deprecations with [DEPRECATED] prefix
- No length limit — completeness over brevity

## Tone
Technical, precise, peer-to-peer. No marketing language.

## Company Context
{{$companyContext}}

## Commits to Translate
{{$commits}}

## Custom Instructions
{{$customInstructions}}

Write the tech lead changelog now:
```

The `{{$variable}}` syntax is Semantic Kernel's prompt template format.

---

## 11. GitHub Webhook (Paid Platform — Phase 2)

Flow when a PR merges:

```
1. GitHub sends POST /webhooks/github
2. Verify HMAC-SHA256 signature against secret
3. Check event type — only process pull_request closed + merged = true
4. Extract commits from payload
5. Look up project by repository URL
6. Retrieve company context from memory store
7. Run ChangelogTranslatorService
8. Store output in database
9. Update public changelog page
10. Send Slack/email notification if configured
11. Return 200 OK to GitHub
```

Webhook must return 200 within 10 seconds. Run translation async if needed.

---

## 12. Company Context Memory (Paid Platform — Phase 2)

The memory system uses Semantic Kernel's memory abstraction over pgvector.

**What gets stored per project:**
- Product description and positioning
- Customer terminology rules
- Past changelog entries (learns style over time)
- Explicit corrections ("never describe X as Y")
- Product area classifications

**How it improves translation:**

On each translation, before calling the LLM:
1. Embed the incoming commits
2. Search memory for relevant context (top 5 matches)
3. Inject retrieved context into the prompt

Over time the model output becomes increasingly specific to the company's
actual product and communication style. This is the primary reason teams
choose the hosted version over self-hosting.

---

## 13. Launch Plan

### Phase 1 — Open Source Core

**Week 1–2:** Build core, API, CLI  
**Week 3:** Documentation, tests, README  
**Week 4:** Hacker News Show HN post, Reddit posts, Dev.to article  

Target: 100 GitHub stars in first month. Measure activation — did they
successfully run a translation?

### Phase 2 — Hosted Platform

**Week 5–8:** PostgreSQL, auth, webhook, memory  
**Week 9:** Stripe integration  
**Week 10:** Product Hunt launch, email list announcement  

Target: 10 paying customers in first 60 days. Single metric.

### Distribution Channels

1. Hacker News — Show HN, dev community
2. r/devops, r/ExperiencedDevs — problem-aware audience
3. Dev.to — SEO-friendly articles
4. Product Hunt — paid launch
5. Dev Twitter/X — share real output examples
6. Direct outreach to open source maintainers — they need this

---

## 14. Success Metrics

| Metric | Target | Timeline |
|---|---|---|
| GitHub stars | 100 | Month 1 |
| Open source activations | 50 unique users | Month 1 |
| Waitlist signups | 200 | Before paid launch |
| Paying customers | 10 | Month 3 |
| MRR | $500 | Month 3 |
| MRR | $2,000 | Month 6 |
| MRR | $5,000 | Month 12 |
| Churn | Under 5%/month | Ongoing |

---

## 15. What Is Out Of Scope

Do not build these until $5k MRR:

- Mobile app
- Browser extension
- GitLab support (GitHub only first)
- Multi-language UI
- White-label
- AI features beyond translation
- Social features
- API rate limiting (use simple throttling for now)
- Advanced analytics
- On-premise enterprise deployment

---

## 16. Risks

| Risk | Mitigation |
|---|---|
| Output quality not good enough | Spend serious time on prompts before launch |
| People just use ChatGPT instead | Company context memory and webhook automation are the moat |
| Headwayapp copies the feature | Speed and open source community loyalty |
| Low conversion free to paid | Webhook automation is the conversion driver — make it seamless |
| Anthropic API costs at scale | Pass API cost through in pricing, monitor per-translation cost |

---

*This document is the single source of truth for the LogTunnel project.
All architectural decisions and scope decisions made here supersede
anything suggested by AI tools during development.*
