-- ============================================================================
-- LogTunnel Phase 2 — Postgres schema (initial)
--
-- This file is the source of truth for the Phase 2 schema. It is committed
-- ahead of any C# / EF Core code so reviewers can eyeball the design
-- independently of EF's translation layer. The EF Core migration generated
-- in step 5 of the build order MUST stay close to this file; any
-- intentional divergence is documented in that commit's message.
--
-- Conventions
-- ----------------------------------------------------------------------------
-- - All primary keys are uuid (gen_random_uuid()).
-- - Every table except `tenants` carries `tenant_id uuid not null` for
--   tenancy isolation, with a foreign key ON DELETE CASCADE so deleting a
--   tenant deletes everything they own.
-- - All tables carry `created_at` and `updated_at` (timestamptz, default
--   now()), with the exception of immutable / append-only tables (audit
--   logs, commits, daily_log_revisions, public_translation_events) which
--   only carry the relevant single timestamp.
-- - Soft enums use CHECK constraints rather than Postgres ENUM types so
--   adding a value is a one-line ALTER TABLE rather than a CREATE TYPE +
--   migration dance.
-- - Foreign keys to `users` use ON DELETE RESTRICT by default; we don't
--   want to lose history when a person leaves. Hard deletes go through a
--   tombstone migration script.
-- - Every CHECK / UNIQUE / INDEX expression is named explicitly so EF Core
--   migrations can match against the same identifiers.
-- ============================================================================


-- gen_random_uuid() lives in the pgcrypto extension on older Postgres
-- builds; on Postgres 13+ it's in core but the extension is harmless.
CREATE EXTENSION IF NOT EXISTS pgcrypto;


-- ============================================================================
-- 1. Tenancy and identity
-- ============================================================================

CREATE TABLE tenants (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name            text NOT NULL,
    slug            text NOT NULL UNIQUE,                                          -- url-safe identifier, e.g. 'acme'
    timezone        text NOT NULL DEFAULT 'UTC',                                    -- IANA tz name; drives daily-log freeze at local midnight

    -- LLM connector configuration. Mirrors LogTunnel.Core.Configuration.LlmOptions.
    llm_provider    text NOT NULL CHECK (llm_provider IN ('Anthropic','OpenAI','Ollama')),
    llm_model       text NOT NULL,
    llm_api_key_enc bytea,                                                          -- nullable for Ollama; AES-encrypted at rest
    llm_base_url    text,                                                           -- optional override (Azure OpenAI, DeepSeek, custom Ollama, etc.)

    -- Mirrors LogTunnel.Core.Domain.CompanyContext as JSONB so the field set
    -- can evolve without a schema change. Required at tenant creation.
    company_context jsonb NOT NULL,                                                 -- { productDescription, targetCustomers, terminology, additionalContext }

    status          text NOT NULL DEFAULT 'active' CHECK (status IN ('active','suspended')),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);


-- One row per logged-in human. Belongs to exactly one tenant.
CREATE TABLE users (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    email           text NOT NULL,                                                  -- unique within a tenant; not globally unique
    display_name    text NOT NULL,
    password_hash   text NOT NULL,                                                  -- ASP.NET Core Identity hash
    -- dashboard_role is the UI default. Per-team and per-project membership
    -- refine actual permissions further (a developer can also be a team
    -- lead of a specific team via team_members.role = 'lead').
    dashboard_role  text NOT NULL CHECK (dashboard_role IN ('developer','team_lead','manager','ceo','marketing','admin')),
    git_email       text,                                                           -- the email that appears as the author in their commits, if different from the login email
    status          text NOT NULL DEFAULT 'active' CHECK (status IN ('active','disabled')),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, email)
);

-- Backfill / lookup index for resolving commits.author_email -> users.id.
-- Lower() to match author emails case-insensitively (Git is consistent but
-- humans aren't when they configure git config user.email).
CREATE INDEX users_git_email_idx ON users (tenant_id, lower(git_email));


-- ============================================================================
-- 2. Org structure: teams and projects
-- ============================================================================

CREATE TABLE teams (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name            text NOT NULL,
    slug            text NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, slug)
);

-- A user is on a team in some role. role='lead' is what makes them a
-- "team lead" for that specific team — independent of users.dashboard_role.
-- Lets a developer also be a team lead of one team without changing job title.
CREATE TABLE team_members (
    team_id         uuid NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id         uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    role            text NOT NULL CHECK (role IN ('member','lead')),
    joined_at       timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (team_id, user_id)
);
CREATE INDEX team_members_user_idx ON team_members (user_id);


CREATE TABLE projects (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name            text NOT NULL,
    slug            text NOT NULL,
    description     text,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, slug)
);

CREATE TABLE project_members (
    project_id      uuid NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    user_id         uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    joined_at       timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (project_id, user_id)
);
CREATE INDEX project_members_user_idx ON project_members (user_id);


-- ============================================================================
-- 3. Repositories — owned at tenant level, mapped many-to-many into projects.
--    This is the monorepo-friendly model: one repo can serve many projects
--    via path_filter globs.
-- ============================================================================

CREATE TABLE repositories (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    host              text NOT NULL CHECK (host IN ('github')),                     -- gitlab / bitbucket added later without schema change
    remote_url        text NOT NULL,                                                -- canonical https url, e.g. 'https://github.com/acme/web'
    default_branch    text NOT NULL DEFAULT 'main',
    webhook_secret    text NOT NULL,                                                -- HMAC verification secret per repo
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, remote_url)
);


-- Many-to-many: one repo can map to many logical projects. The optional
-- path_filter glob lets a monorepo serve multiple projects: commits
-- touching '/frontend/**' map to the Frontend project, '/api/**' to the
-- API project. NULL path_filter means "every commit in this repo belongs
-- to this project" — the simple non-monorepo case.
--
-- The COALESCE in the primary key allows the SAME (repo, project) pair to
-- coexist with different path_filters: a project might want both
-- '/frontend/**' AND '/shared/ui/**' from one repo.
CREATE TABLE repository_project_mappings (
    repository_id   uuid NOT NULL REFERENCES repositories(id) ON DELETE CASCADE,
    project_id      uuid NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    path_filter     text,                                                           -- nullable glob, e.g. '/frontend/**'
    created_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (repository_id, project_id, COALESCE(path_filter, ''))
);


-- ============================================================================
-- 4. Commits — ingested via the GitHub webhook handler in the next phase.
-- ============================================================================

CREATE TABLE commits (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    repository_id     uuid NOT NULL REFERENCES repositories(id) ON DELETE CASCADE,
    sha               text NOT NULL,
    parent_shas       text[] NOT NULL DEFAULT '{}',
    message           text NOT NULL,                                                -- subject + body, joined with two newlines
    author_email      text NOT NULL,                                                -- raw, from git
    author_user_id    uuid REFERENCES users(id) ON DELETE SET NULL,                 -- nullable, populated by users.git_email match
    authored_at       timestamptz NOT NULL,
    changed_files     text[] NOT NULL DEFAULT '{}',                                 -- list of touched paths; matched against repository_project_mappings.path_filter at ingest time
    ingested_at       timestamptz NOT NULL DEFAULT now(),
    UNIQUE (repository_id, sha)
);
CREATE INDEX commits_tenant_authored_idx ON commits (tenant_id, authored_at DESC);
CREATE INDEX commits_author_authored_idx ON commits (author_user_id, authored_at DESC) WHERE author_user_id IS NOT NULL;
CREATE INDEX commits_changed_files_gin    ON commits USING gin (changed_files);


-- Materialised mapping: which projects does each commit belong to,
-- evaluated against repository_project_mappings at ingest time.
-- Done as a real table (not a view) so the join is fast and so a mapping
-- change can be backfilled across history without re-fetching commit data
-- from GitHub.
CREATE TABLE commit_projects (
    commit_id       uuid NOT NULL REFERENCES commits(id) ON DELETE CASCADE,
    project_id      uuid NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    PRIMARY KEY (commit_id, project_id)
);
CREATE INDEX commit_projects_project_idx ON commit_projects (project_id);


-- ============================================================================
-- 5. Daily work logs — the centerpiece of Phase 2.
--    One row per (user, day). Editable until midnight in the tenant
--    timezone, then frozen — subsequent edits go to daily_log_revisions.
-- ============================================================================

CREATE TABLE daily_logs (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id           uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    log_date          date NOT NULL,                                                -- the calendar day in tenant timezone
    raw_note          text NOT NULL,                                                -- what the developer wrote, plain text
    project_tags      uuid[] NOT NULL DEFAULT '{}',                                 -- optional list of project ids the day touched (no FK enforcement, by design — the array is a hint)
    blocker_status    text CHECK (blocker_status IN ('need_help','waiting','unclear')), -- nullable; NULL = no blocker
    blocker_note      text,                                                         -- nullable, free-text follow-up
    frozen_at         timestamptz,                                                  -- set when the day rolls over in tenant timezone; NULL = still editable
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, user_id, log_date)
);
CREATE INDEX daily_logs_tenant_date_idx     ON daily_logs (tenant_id, log_date DESC);
-- Partial index for the team-lead "show me today's blockers" query:
CREATE INDEX daily_logs_blockers_idx        ON daily_logs (tenant_id, log_date) WHERE blocker_status IS NOT NULL;
-- GIN index for "show me everything Alice tagged the Frontend project for":
CREATE INDEX daily_logs_project_tags_gin    ON daily_logs USING gin (project_tags);


-- Append-only revision trail. Once daily_logs.frozen_at is set, every
-- subsequent edit writes a new row here instead of mutating the parent.
CREATE TABLE daily_log_revisions (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    daily_log_id      uuid NOT NULL REFERENCES daily_logs(id) ON DELETE CASCADE,
    raw_note          text NOT NULL,
    project_tags      uuid[] NOT NULL DEFAULT '{}',
    blocker_status    text CHECK (blocker_status IN ('need_help','waiting','unclear')),
    blocker_note      text,
    edit_reason       text,                                                         -- optional one-line reason supplied by the editor
    edited_by         uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    edited_at         timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX daily_log_revisions_log_idx ON daily_log_revisions (daily_log_id, edited_at DESC);


-- ============================================================================
-- 6. Translations — the persisted output of the translator service.
--    One row per (scope, date_range, audience). A background worker
--    re-renders when inputs_hash drifts from the actual upstream commits +
--    daily logs.
-- ============================================================================

CREATE TABLE translations (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    scope_kind        text NOT NULL CHECK (scope_kind IN ('user','team','project','tenant')),
    scope_id          uuid NOT NULL,                                                -- polymorphic FK; points into the matching table for scope_kind
    date_from         date NOT NULL,
    date_to           date NOT NULL,                                                -- inclusive; for daily standups date_from = date_to
    audience          text NOT NULL CHECK (audience IN ('TechLead','Manager','CEO','Public')),
    content           text NOT NULL,                                                -- the LLM's original markdown output, IMMUTABLE
    inputs_hash       text NOT NULL,                                                -- sha256 of (commit_shas ∪ daily_log_ids ∪ company_context_version)
    generated_at      timestamptz NOT NULL DEFAULT now(),
    generated_by      uuid REFERENCES users(id) ON DELETE SET NULL,                 -- nullable for system-triggered renders
    status            text NOT NULL DEFAULT 'ready' CHECK (status IN ('pending','ready','failed')),
    failure_reason    text,                                                         -- nullable, populated when status='failed'
    UNIQUE (tenant_id, scope_kind, scope_id, date_from, date_to, audience)
);
CREATE INDEX translations_tenant_generated_idx ON translations (tenant_id, generated_at DESC);
CREATE INDEX translations_pending_idx          ON translations (tenant_id, status) WHERE status = 'pending';


-- ============================================================================
-- 7. Public translations — marketing's edit / approve / publish workflow.
--    A row here only exists for translations whose audience='Public'.
--    The original LLM output stays in translations.content (immutable);
--    marketing's edits live in public_translations.edited_content.
-- ============================================================================

CREATE TABLE public_translations (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    translation_id    uuid NOT NULL UNIQUE REFERENCES translations(id) ON DELETE CASCADE,
    edited_content    text,                                                         -- nullable: NULL until marketing first opens the draft
    workflow_status   text NOT NULL DEFAULT 'draft'
                          CHECK (workflow_status IN ('draft','approved','published')),
    approved_by       uuid REFERENCES users(id) ON DELETE SET NULL,
    approved_at       timestamptz,
    published_at      timestamptz,
    public_slug       text,                                                         -- url path on the public changelog page (set at publish time)
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX public_translations_status_idx ON public_translations (tenant_id, workflow_status);
CREATE UNIQUE INDEX public_translations_slug_idx
    ON public_translations (tenant_id, public_slug)
    WHERE public_slug IS NOT NULL;


-- Audit trail of every workflow transition (draft → approved → published)
-- and every edit. Append-only.
CREATE TABLE public_translation_events (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    public_translation_id uuid NOT NULL REFERENCES public_translations(id) ON DELETE CASCADE,
    event_type        text NOT NULL CHECK (event_type IN ('edited','approved','rejected','published','unpublished')),
    actor_id          uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    notes             text,                                                         -- optional one-line note from the actor
    occurred_at       timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX public_translation_events_pt_idx ON public_translation_events (public_translation_id, occurred_at DESC);


-- ============================================================================
-- 8. Stand-up exports — analytics + audit trail of "team lead clicked Export".
-- ============================================================================

CREATE TABLE standup_exports (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    exported_by       uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    team_id           uuid REFERENCES teams(id) ON DELETE SET NULL,
    project_id        uuid REFERENCES projects(id) ON DELETE SET NULL,
    log_date          date NOT NULL,
    channel           text NOT NULL CHECK (channel IN ('slack','teams','copy','email')),
    payload           jsonb NOT NULL,                                               -- the rendered block we returned to the client
    exported_at       timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX standup_exports_tenant_date_idx ON standup_exports (tenant_id, log_date DESC);


-- ============================================================================
-- 9. Webhook deliveries — debugging + idempotency for GitHub retries.
-- ============================================================================

CREATE TABLE webhook_deliveries (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    repository_id     uuid NOT NULL REFERENCES repositories(id) ON DELETE CASCADE,
    event_type        text NOT NULL,                                                -- 'push' | 'pull_request' | etc
    delivery_id       text NOT NULL,                                                -- GitHub's X-GitHub-Delivery header, used for dedupe
    signature_valid   boolean NOT NULL,
    payload           jsonb NOT NULL,
    processed_at      timestamptz,
    outcome           text CHECK (outcome IN ('ok','duplicate','invalid_signature','error')),
    received_at       timestamptz NOT NULL DEFAULT now(),
    UNIQUE (repository_id, delivery_id)                                             -- idempotency: dedupe GitHub retries
);
CREATE INDEX webhook_deliveries_repo_received_idx ON webhook_deliveries (repository_id, received_at DESC);
