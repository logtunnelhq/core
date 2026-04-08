# LogTunnel

[![build](https://img.shields.io/github/actions/workflow/status/your-org/logtunnel/ci.yml?branch=main&label=build)](https://github.com/your-org/logtunnel/actions)
[![license](https://img.shields.io/badge/license-AGPL--3.0-blue)](LICENSE)
[![nuget](https://img.shields.io/nuget/v/LogTunnel?label=nuget)](https://www.nuget.org/packages/LogTunnel)

LogTunnel translates raw Git commits into four audience-specific changelogs — one set of commits, four outputs (tech lead, manager, CEO, public-facing), generated in parallel by the LLM of your choice.

## The problem

Software teams ship code every week. The people who actually need to know what changed — managers, executives, customers, investors — almost never hear about it in language they understand. Developers write commits for themselves: `fix: null ref in payment handler`, `feat: idempotency key on order creation`. That's perfect for the next dev who has to debug it. It's useless to anyone else.

So a PM wanting to write a customer newsletter has three options: interrupt a developer, guess at what shipped, or skip the update. Existing tools (Headway, Beamer, Announcekit) solve the publishing step, not the translation step — they all assume someone has already turned commits into prose. LogTunnel is the missing step. It takes the commits and produces four parallel versions, each tuned for a specific reader, using prompt templates you control.

## Quick start

```sh
# 1. Install the CLI
dotnet tool install -g LogTunnel

# 2. Pick an LLM provider — Anthropic, OpenAI, or a local Ollama
export LLM__PROVIDER=Anthropic
export LLM__MODEL=claude-sonnet-4-20250514
export LLM__APIKEY=sk-ant-...

# 3. Inside your repo, create a .logtunnel.json
cd ~/code/your-product
logtunnel configure

# 4. Translate
git log --oneline -20 > commits.txt
logtunnel translate --input commits.txt
```

That's the whole loop. The first run prints all four audience variants to your terminal. Pass `--output ./changelogs/` to write them to disk as `tech-lead.md`, `manager.md`, `ceo.md`, `public.md` instead. Pass `--audience CEO` to render only one.

If you don't want the global tool, clone the repo and run `dotnet run --project src/LogTunnel.Cli -- translate ...` instead. See [Self-hosting](#self-hosting) below.

## CLI reference

### `logtunnel translate`

In-process translation. Reads `.logtunnel.json`, calls the configured LLM for each audience in parallel, prints or writes the result.

| Flag | Short | Required | Default | Description |
|---|---|---|---|---|
| `--input` | `-i` | yes | — | File containing raw commit messages |
| `--config` | `-c` | no | `./.logtunnel.json` | Path to the project config |
| `--audience` | `-a` | no | all | Filter to one audience: `TechLead`, `Manager`, `CEO`, `Public` (case-insensitive) |
| `--output` | `-o` | no | stdout | Directory to write per-audience `.md` files into |

Exits `0` on success and `1` on any failure (missing input, missing config, unknown audience, LLM error). Errors go to stderr as a single line; framework noise is filtered out.

### `logtunnel configure`

Interactive prompts for company context, then writes `.logtunnel.json` to the current directory. No flags.

The file is seeded with sensible defaults for all four audiences — edit it later to tune tone, format, and per-audience instructions. If a `.logtunnel.json` already exists, configure asks `[y/N]` before overwriting.

Configure has zero LLM dependency by design. You can run it on a fresh checkout before you have an API key.

## API reference

The same engine ships behind a minimal HTTP API for self-hosters who want to call it from a service or webhook. Three endpoints, no auth — auth is a Phase 2 concern that lives in the hosted platform.

### `GET /health`

```sh
curl http://localhost:5000/health
```

```json
{
  "status": "healthy",
  "version": "1.0.0.0"
}
```

### `POST /translate`

```sh
curl -X POST http://localhost:5000/translate \
  -H "Content-Type: application/json" \
  -d '{
    "rawCommits": "fix: null ref in payment handler\nfeat: idempotency key on order creation",
    "context": {
      "productDescription": "B2B invoicing tool for freelancers",
      "targetCustomers": "Freelance designers and developers",
      "terminology": "Say members not users"
    },
    "audiences": [
      { "type": "TechLead", "tone": "Technical and direct", "format": "Bullet points with PR refs" },
      { "type": "CEO",      "tone": "Plain English",        "format": "Max 3 bullet points" }
    ]
  }'
```

```json
{
  "id": "8f3c2b6a-1d4e-4a91-b8e7-2a9c4f6e1234",
  "generatedAt": "2026-04-07T15:42:18.337+00:00",
  "outputs": {
    "TechLead": "- Fix null reference in payment handler...\n- Add idempotency key to POST /orders...",
    "CEO":      "- We eliminated a class of payment failures.\n- Order creation is now duplicate-safe."
  }
}
```

Validation failures return `400 Bad Request` with [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457). Translator failures return `502 Bad Gateway` with the underlying message.

### `POST /configure`

Persists a `.logtunnel.json` on the server's disk. Useful for the hosted platform's onboarding flow; self-hosters more often write the file by hand or via `logtunnel configure`.

```sh
curl -X POST http://localhost:5000/configure \
  -H "Content-Type: application/json" \
  -d '{
    "companyContext": {
      "productDescription": "B2B invoicing tool for freelancers",
      "targetCustomers": "Freelance designers and developers",
      "terminology": "Say members not users"
    },
    "audienceConfigs": [
      { "type": "TechLead", "tone": "Technical and direct", "format": "Bullet points with PR refs" },
      { "type": "CEO",      "tone": "Plain English",        "format": "Max 3 bullet points" }
    ]
  }'
```

```json
{
  "configId": "51db1eca-5846-4652-b966-bf5a94c6b8de",
  "path": "/var/logtunnel/.logtunnel.json"
}
```

By default the file lands in the API's current working directory. Override with `LogTunnel__Config__OutputDirectory=/some/path` (or the equivalent `appsettings.json` section).

## Self-hosting

```sh
git clone https://github.com/your-org/logtunnel.git
cd logtunnel

# Pick any provider
export LLM__PROVIDER=Anthropic
export LLM__MODEL=claude-sonnet-4-20250514
export LLM__APIKEY=sk-ant-...

# Run the API
dotnet run --project src/LogTunnel.Api

# Or run the CLI directly without installing the global tool
dotnet run --project src/LogTunnel.Cli -- translate --input commits.txt
```

The API listens on `http://localhost:5000` by default. Override with `--urls http://0.0.0.0:8080` (or any standard ASP.NET Core hosting flag).

The translator's prompt templates live in `docs/prompts/` as plain markdown — `tech-lead.md`, `manager.md`, `ceo.md`, `public.md`. Edit them, restart, and the changes pick up automatically. No recompile, no redeploy. This is intentional: the prompts are the product. You should be able to tune how LogTunnel talks to each audience without touching C#.

The build runs against .NET 8. Tests live under `tests/LogTunnel.Core.Tests`; run them with:

```sh
dotnet test
```

## Supported LLM providers

The connector is pluggable. Set `LLM__PROVIDER` to one of the values below; the registrar wires the right Semantic Kernel chat completion service at startup. Unknown providers fail fast with a clear error listing the supported set.

| Provider | Config value | Notes |
|---|---|---|
| Anthropic Claude | `Anthropic` | Official Anthropic .NET SDK. Any Claude model id (`claude-sonnet-4-20250514`, `claude-opus-4-...`, ...). |
| OpenAI | `OpenAI` | `gpt-4o`, `gpt-4o-mini`, anything OpenAI-compatible. |
| Ollama (local) | `Ollama` | `llama3.2`, `mistral`, any local model. Defaults to `http://localhost:11434`. No API key required. |
| DeepSeek | `OpenAI` | DeepSeek's API is OpenAI-compatible. Set `LLM__BASEURL=https://api.deepseek.com/v1`. |
| Azure OpenAI | `OpenAI` | Set `LLM__BASEURL` to your Azure resource endpoint. |

`LLM__BASEURL` is optional — leave it unset for the provider's default. `LLM__APIKEY` is required for Anthropic and OpenAI; ignored by Ollama.

Adding a new provider is a switch case in `src/LogTunnel.Core/Llm/LlmConnectorRegistrar.cs` plus a row in this table.

## Example output

Given these commits:

```
fix: null ref in payment handler when customer profile incomplete
feat: idempotency key on order creation
refactor: extract DiscountCalculationService out of OrderService
chore: bump stripe sdk to 45.2.0
fix: timezone bug on invoice due date display (#412)
```

and a `.logtunnel.json` describing a B2B invoicing tool for freelancers, you get four parallel outputs.

**Tech Lead**

```markdown
**Reliability**
- Add idempotency key to `POST /orders`. Duplicate requests now return the
  original order rather than charging the workspace twice.
- Fix null reference in payment handler when a member's profile is incomplete.

**Refactor**
- Extract `DiscountCalculationService` out of `OrderService`. No behavioural
  change; sets up the upcoming pricing experiments.

**Bug fixes**
- Invoice due dates now render in the workspace's timezone, not UTC (#412).

**Dependencies**
- Stripe SDK 45.2.0.
```

**Manager**

```markdown
- Fixed a payment crash that hit members whose profile wasn't fully filled in.
- Duplicate orders are now prevented at the API level — no more accidental
  double charges if a request is retried.
- Invoice due dates now show in each workspace's local timezone instead of UTC.
- Internal cleanup to make discount logic easier to change ahead of the
  pricing work next sprint.
```

**CEO**

```markdown
- We eliminated a class of double-charge incidents.
- Payment reliability is up; an outstanding crash class is gone.
- Cleared the path for the pricing experiments we discussed.
```

**Public**

```markdown
We've made checkout more reliable. If your connection drops mid-payment and
your client retries, we now make sure you only get charged once.

Invoices now show due dates in your workspace's timezone, so "due Friday"
means Friday where you are — not somewhere else in the world.

More flexible pricing options are coming soon. We've reshaped some things
under the hood to make that possible.
```

Same five commits. Four registers. The PM, the CEO, and the customer all get the same week of work in language that fits how they actually think.

## Contributing

Issues and pull requests are welcome. The fastest way to land a change is to open an issue first describing what you want to do — especially for anything that touches the prompt templates in `docs/prompts/`. The prompt design *is* the product, and changes there need a quick conversation before they go in. For everything else (bug fixes, new providers, additional tests, documentation) just open a PR.

There's no CLA. By contributing you agree your changes are licensed under AGPL-3.0 along with the rest of the project. The `tests/LogTunnel.Core.Tests` suite must stay green; run `dotnet test` from the repo root before pushing. If you're adding a new LLM provider, please also add it to the [provider table](#supported-llm-providers) in this README and to the connector switch in `src/LogTunnel.Core/Llm/LlmConnectorRegistrar.cs`.

## License

[AGPL-3.0](LICENSE). The translation engine, CLI, and minimal API are open source forever. The hosted platform features (webhooks, public changelog pages, custom domains, SSO, the company-context memory store) live in a private repository under a separate license.

If you want to build a competing hosted product on top of LogTunnel, the AGPL means your modifications need to be open-sourced too. If you just want to run it for your own team or your own customers, you can do whatever you like with it.
