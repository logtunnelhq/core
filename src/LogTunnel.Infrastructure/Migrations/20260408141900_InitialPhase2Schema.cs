using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogTunnel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPhase2Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "UTC"),
                    llm_provider = table.Column<string>(type: "text", nullable: false),
                    llm_model = table.Column<string>(type: "text", nullable: false),
                    llm_api_key_enc = table.Column<byte[]>(type: "bytea", nullable: true),
                    llm_base_url = table.Column<string>(type: "text", nullable: true),
                    company_context = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.CheckConstraint("ck_tenants_llm_provider", "llm_provider IN ('Anthropic','OpenAI','Ollama')");
                    table.CheckConstraint("ck_tenants_status", "status IN ('active','suspended')");
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host = table.Column<string>(type: "text", nullable: false),
                    remote_url = table.Column<string>(type: "text", nullable: false),
                    default_branch = table.Column<string>(type: "text", nullable: false, defaultValue: "main"),
                    webhook_secret = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repositories", x => x.id);
                    table.CheckConstraint("ck_repositories_host", "host IN ('github')");
                    table.ForeignKey(
                        name: "FK_repositories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    dashboard_role = table.Column<string>(type: "text", nullable: false),
                    git_email = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_dashboard_role", "dashboard_role IN ('developer','team_lead','manager','ceo','marketing','admin')");
                    table.CheckConstraint("ck_users_status", "status IN ('active','disabled')");
                    table.ForeignKey(
                        name: "FK_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repository_project_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path_filter = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repository_project_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_repository_project_mappings_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_repository_project_mappings_repositories_repository_id",
                        column: x => x.repository_id,
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    delivery_id = table.Column<string>(type: "text", nullable: false),
                    signature_valid = table.Column<bool>(type: "boolean", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_deliveries", x => x.id);
                    table.CheckConstraint("ck_webhook_deliveries_outcome", "outcome IS NULL OR outcome IN ('ok','duplicate','invalid_signature','error')");
                    table.ForeignKey(
                        name: "FK_webhook_deliveries_repositories_repository_id",
                        column: x => x.repository_id,
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_webhook_deliveries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sha = table.Column<string>(type: "text", nullable: false),
                    parent_shas = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    message = table.Column<string>(type: "text", nullable: false),
                    author_email = table.Column<string>(type: "text", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    authored_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_files = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    ingested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commits", x => x.id);
                    table.ForeignKey(
                        name: "FK_commits_repositories_repository_id",
                        column: x => x.repository_id,
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commits_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commits_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "daily_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false),
                    raw_note = table.Column<string>(type: "text", nullable: false),
                    project_tags = table.Column<Guid[]>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'"),
                    blocker_status = table.Column<string>(type: "text", nullable: true),
                    blocker_note = table.Column<string>(type: "text", nullable: true),
                    frozen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_logs", x => x.id);
                    table.CheckConstraint("ck_daily_logs_blocker_status", "blocker_status IS NULL OR blocker_status IN ('need_help','waiting','unclear')");
                    table.ForeignKey(
                        name: "FK_daily_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standup_exports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exported_by = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    exported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standup_exports", x => x.id);
                    table.CheckConstraint("ck_standup_exports_channel", "channel IN ('slack','teams','copy','email')");
                    table.ForeignKey(
                        name: "FK_standup_exports_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_standup_exports_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_standup_exports_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_standup_exports_users_exported_by",
                        column: x => x.exported_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => new { x.team_id, x.user_id });
                    table.CheckConstraint("ck_team_members_role", "role IN ('member','lead')");
                    table.ForeignKey(
                        name: "FK_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_kind = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    inputs_hash = table.Column<string>(type: "text", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    generated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "ready"),
                    failure_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translations", x => x.id);
                    table.CheckConstraint("ck_translations_audience", "audience IN ('TechLead','Manager','CEO','Public')");
                    table.CheckConstraint("ck_translations_scope_kind", "scope_kind IN ('user','team','project','tenant')");
                    table.CheckConstraint("ck_translations_status", "status IN ('pending','ready','failed')");
                    table.ForeignKey(
                        name: "FK_translations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_translations_users_generated_by",
                        column: x => x.generated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "commit_projects",
                columns: table => new
                {
                    commit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commit_projects", x => new { x.commit_id, x.project_id });
                    table.ForeignKey(
                        name: "FK_commit_projects_commits_commit_id",
                        column: x => x.commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commit_projects_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_log_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    daily_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_note = table.Column<string>(type: "text", nullable: false),
                    project_tags = table.Column<Guid[]>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'"),
                    blocker_status = table.Column<string>(type: "text", nullable: true),
                    blocker_note = table.Column<string>(type: "text", nullable: true),
                    edit_reason = table.Column<string>(type: "text", nullable: true),
                    edited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_log_revisions", x => x.id);
                    table.CheckConstraint("ck_daily_log_revisions_blocker_status", "blocker_status IS NULL OR blocker_status IN ('need_help','waiting','unclear')");
                    table.ForeignKey(
                        name: "FK_daily_log_revisions_daily_logs_daily_log_id",
                        column: x => x.daily_log_id,
                        principalTable: "daily_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_log_revisions_users_edited_by",
                        column: x => x.edited_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "public_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    edited_content = table.Column<string>(type: "text", nullable: true),
                    workflow_status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    public_slug = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_translations", x => x.id);
                    table.CheckConstraint("ck_public_translations_workflow_status", "workflow_status IN ('draft','approved','published')");
                    table.ForeignKey(
                        name: "FK_public_translations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_public_translations_translations_translation_id",
                        column: x => x.translation_id,
                        principalTable: "translations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_public_translations_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "public_translation_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    public_translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_translation_events", x => x.id);
                    table.CheckConstraint("ck_public_translation_events_event_type", "event_type IN ('edited','approved','rejected','published','unpublished')");
                    table.ForeignKey(
                        name: "FK_public_translation_events_public_translations_public_transl~",
                        column: x => x.public_translation_id,
                        principalTable: "public_translations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_public_translation_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "commit_projects_project_idx",
                table: "commit_projects",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "commits_author_authored_idx",
                table: "commits",
                columns: new[] { "author_user_id", "authored_at" },
                descending: new[] { false, true },
                filter: "author_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "commits_changed_files_gin",
                table: "commits",
                column: "changed_files")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "commits_tenant_authored_idx",
                table: "commits",
                columns: new[] { "tenant_id", "authored_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_commits_repository_id_sha",
                table: "commits",
                columns: new[] { "repository_id", "sha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "daily_log_revisions_log_idx",
                table: "daily_log_revisions",
                columns: new[] { "daily_log_id", "edited_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_daily_log_revisions_edited_by",
                table: "daily_log_revisions",
                column: "edited_by");

            migrationBuilder.CreateIndex(
                name: "daily_logs_blockers_idx",
                table: "daily_logs",
                columns: new[] { "tenant_id", "log_date" },
                descending: new[] { false, true },
                filter: "blocker_status IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "daily_logs_project_tags_gin",
                table: "daily_logs",
                column: "project_tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_tenant_id_user_id_log_date",
                table: "daily_logs",
                columns: new[] { "tenant_id", "user_id", "log_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_user_id",
                table: "daily_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "project_members_user_idx",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_tenant_id_slug",
                table: "projects",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_public_translation_events_actor_id",
                table: "public_translation_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "public_translation_events_pt_idx",
                table: "public_translation_events",
                columns: new[] { "public_translation_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_public_translations_approved_by",
                table: "public_translations",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_public_translations_translation_id",
                table: "public_translations",
                column: "translation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "public_translations_slug_idx",
                table: "public_translations",
                columns: new[] { "tenant_id", "public_slug" },
                unique: true,
                filter: "public_slug IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "public_translations_status_idx",
                table: "public_translations",
                columns: new[] { "tenant_id", "workflow_status" });

            migrationBuilder.CreateIndex(
                name: "IX_repositories_tenant_id_remote_url",
                table: "repositories",
                columns: new[] { "tenant_id", "remote_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repository_project_mappings_project_id",
                table: "repository_project_mappings",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ux_repository_project_mappings_filtered",
                table: "repository_project_mappings",
                columns: new[] { "repository_id", "project_id", "path_filter" },
                unique: true,
                filter: "path_filter IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_repository_project_mappings_null_filter",
                table: "repository_project_mappings",
                columns: new[] { "repository_id", "project_id" },
                unique: true,
                filter: "path_filter IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_standup_exports_exported_by",
                table: "standup_exports",
                column: "exported_by");

            migrationBuilder.CreateIndex(
                name: "IX_standup_exports_project_id",
                table: "standup_exports",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_standup_exports_team_id",
                table: "standup_exports",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "standup_exports_tenant_date_idx",
                table: "standup_exports",
                columns: new[] { "tenant_id", "log_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "team_members_user_idx",
                table: "team_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_tenant_id_slug",
                table: "teams",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_translations_generated_by",
                table: "translations",
                column: "generated_by");

            migrationBuilder.CreateIndex(
                name: "IX_translations_tenant_id_scope_kind_scope_id_date_from_date_t~",
                table: "translations",
                columns: new[] { "tenant_id", "scope_kind", "scope_id", "date_from", "date_to", "audience" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "translations_pending_idx",
                table: "translations",
                columns: new[] { "tenant_id", "status" },
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "translations_tenant_generated_idx",
                table: "translations",
                columns: new[] { "tenant_id", "generated_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            // EF Core 8 doesn't model functional indexes; the
            // configuration declares users_git_email_idx as a regular
            // composite index, and we rewrite it here in raw SQL to
            // match the functional form in Schema/001_initial.sql:
            //   CREATE INDEX users_git_email_idx ON users (tenant_id, lower(git_email));
            migrationBuilder.Sql(
                "CREATE INDEX users_git_email_idx ON users (tenant_id, lower(git_email));");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_repository_id_delivery_id",
                table: "webhook_deliveries",
                columns: new[] { "repository_id", "delivery_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_tenant_id",
                table: "webhook_deliveries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "webhook_deliveries_repo_received_idx",
                table: "webhook_deliveries",
                columns: new[] { "repository_id", "received_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commit_projects");

            migrationBuilder.DropTable(
                name: "daily_log_revisions");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "public_translation_events");

            migrationBuilder.DropTable(
                name: "repository_project_mappings");

            migrationBuilder.DropTable(
                name: "standup_exports");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "webhook_deliveries");

            migrationBuilder.DropTable(
                name: "commits");

            migrationBuilder.DropTable(
                name: "daily_logs");

            migrationBuilder.DropTable(
                name: "public_translations");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "repositories");

            migrationBuilder.DropTable(
                name: "translations");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
