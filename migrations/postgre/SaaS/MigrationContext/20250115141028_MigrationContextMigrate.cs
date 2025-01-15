using System;
using ASC.Files.Core.EF;
using ASC.Files.Core.Security;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.PostgreSql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContextMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_links",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar", maxLength: 200, nullable: false),
                    uid = table.Column<string>(type: "varchar", maxLength: 200, nullable: false),
                    provider = table.Column<string>(type: "char(60)", maxLength: 60, nullable: true),
                    profile = table.Column<string>(type: "text", nullable: false),
                    linked = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_links", x => new { x.id, x.uid });
                });

            migrationBuilder.CreateTable(
                name: "dbip_lookup",
                columns: table => new
                {
                    addr_type = table.Column<string>(type: "text", nullable: false),
                    ip_start = table.Column<byte[]>(type: "bytea", nullable: false),
                    ip_end = table.Column<byte[]>(type: "bytea", nullable: false),
                    continent = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    country = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    stateprov_code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    StateProv = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    District = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    GeonameId = table.Column<int>(type: "integer", nullable: true),
                    TimezoneOffset = table.Column<float>(type: "real", nullable: false),
                    TimezoneName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WeatherCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbip_lookup", x => new { x.addr_type, x.ip_start });
                });

            migrationBuilder.CreateTable(
                name: "files_converts",
                columns: table => new
                {
                    input = table.Column<string>(type: "varchar", maxLength: 50, nullable: false, collation: "pg_catalog.default"),
                    output = table.Column<string>(type: "varchar", maxLength: 50, nullable: false, collation: "pg_catalog.default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_converts", x => new { x.input, x.output });
                });

            migrationBuilder.CreateTable(
                name: "hosting_instance_registration",
                columns: table => new
                {
                    instance_registration_id = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp", nullable: true),
                    worker_type_name = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instance_registration", x => x.instance_registration_id);
                });

            migrationBuilder.CreateTable(
                name: "identity_certs",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pair_type = table.Column<short>(type: "smallint", nullable: false),
                    private_key = table.Column<string>(type: "text", nullable: false),
                    public_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_scopes",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    group = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_scopes", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "identity_shedlock",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    lock_until = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("identity_shedlock_pkey", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "mobile_app_install",
                columns: table => new
                {
                    user_email = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    app_type = table.Column<int>(type: "integer", nullable: false),
                    registered_on = table.Column<DateTime>(type: "timestamp", nullable: false),
                    last_sign = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobile_app_install", x => new { x.user_email, x.app_type });
                });

            migrationBuilder.CreateTable(
                name: "notify_info",
                columns: table => new
                {
                    notify_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    state = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    modify_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notify_info", x => x.notify_id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Region = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    ConnectionString = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Region);
                });

            migrationBuilder.CreateTable(
                name: "tenants_forbiden",
                columns: table => new
                {
                    address = table.Column<string>(type: "varchar", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_forbiden", x => x.address);
                });

            migrationBuilder.CreateTable(
                name: "tenants_quota",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    description = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    features = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.00m),
                    product_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_quota", x => x.tenant);
                });

            migrationBuilder.CreateTable(
                name: "tenants_tenants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    alias = table.Column<string>(type: "varchar", maxLength: 100, nullable: false),
                    mappeddomain = table.Column<string>(type: "varchar", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "2"),
                    version_changed = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    language = table.Column<string>(type: "char(10)", maxLength: 10, nullable: false, defaultValueSql: "'en-US'"),
                    timezone = table.Column<string>(type: "varchar", maxLength: 50, nullable: true),
                    trusteddomains = table.Column<string>(type: "varchar", maxLength: 1024, nullable: true),
                    trusteddomainsenabled = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "false"),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    statuschanged = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    creationdatetime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<string>(type: "varchar", maxLength: 38, nullable: true),
                    industry = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    last_modified = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    calls = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants_version",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    url = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_version", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhooks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, defaultValue: ""),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("webhooks_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webstudio_index",
                columns: table => new
                {
                    index_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webstudio_index", x => x.index_name);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    initiator = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    target = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "varchar", maxLength: 20000, nullable: true),
                    ip = table.Column<string>(type: "varchar", maxLength: 50, nullable: true),
                    browser = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    platform = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    page = table.Column<string>(type: "varchar", maxLength: 300, nullable: true),
                    action = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_events_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "backup_backup",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    is_scheduled = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying", maxLength: 255, nullable: false),
                    hash = table.Column<string>(type: "character varying", maxLength: 64, nullable: false),
                    storage_type = table.Column<int>(type: "integer", nullable: false),
                    storage_base_path = table.Column<string>(type: "character varying", maxLength: 255, nullable: true, defaultValueSql: "NULL"),
                    storage_path = table.Column<string>(type: "character varying", maxLength: 255, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "'0001-01-01 00:00:00'"),
                    storage_params = table.Column<string>(type: "text", nullable: true, defaultValueSql: "NULL"),
                    removed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_backup_backup_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "backup_schedule",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    cron = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    backups_stored = table.Column<int>(type: "integer", nullable: false),
                    storage_type = table.Column<int>(type: "integer", nullable: false),
                    storage_base_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    last_backup_time = table.Column<DateTime>(type: "timestamp", nullable: false),
                    storage_params = table.Column<string>(type: "text", nullable: true),
                    dump = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.tenant_id);
                    table.ForeignKey(
                        name: "FK_backup_schedule_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_acl",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<Guid>(type: "uuid", nullable: false),
                    @object = table.Column<string>(name: "object", type: "text", maxLength: 255, nullable: false),
                    acetype = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_acl", x => new { x.tenant, x.subject, x.action, x.@object });
                    table.ForeignKey(
                        name: "FK_core_acl_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    categoryid = table.Column<Guid>(type: "uuid", nullable: true),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true),
                    sid = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    removed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_core_group_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_settings",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<string>(type: "character varying", maxLength: 128, nullable: false),
                    value = table.Column<byte[]>(type: "bytea", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("core_settings_pkey", x => new { x.tenant, x.id });
                    table.ForeignKey(
                        name: "FK_core_settings_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_subscription",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "varchar", maxLength: 38, nullable: false),
                    action = table.Column<string>(type: "varchar", maxLength: 128, nullable: false),
                    recipient = table.Column<string>(type: "varchar", maxLength: 38, nullable: false),
                    @object = table.Column<string>(name: "object", type: "varchar", maxLength: 128, nullable: false),
                    unsubscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_subscription", x => new { x.tenant, x.source, x.action, x.recipient, x.@object });
                    table.ForeignKey(
                        name: "FK_core_subscription_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_subscriptionmethod",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "varchar", maxLength: 38, nullable: false),
                    action = table.Column<string>(type: "varchar", maxLength: 128, nullable: false),
                    recipient = table.Column<string>(type: "varchar", maxLength: 38, nullable: false),
                    sender = table.Column<string>(type: "varchar", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_subscriptionmethod", x => new { x.tenant, x.source, x.action, x.recipient });
                    table.ForeignKey(
                        name: "FK_core_subscriptionmethod_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_user_relations",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    source_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreUserRelations", x => new { x.tenant_id, x.source_user_id, x.target_user_id });
                    table.ForeignKey(
                        name: "FK_core_user_relations_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_userdav",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("core_userdav_pkey", x => new { x.tenant_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_core_userdav_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_usergroup",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    groupid = table.Column<Guid>(type: "uuid", nullable: false),
                    ref_type = table.Column<int>(type: "integer", nullable: false),
                    removed = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    last_modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("core_usergroup_pkey", x => new { x.tenant, x.userid, x.groupid, x.ref_type });
                    table.ForeignKey(
                        name: "FK_core_usergroup_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_userphoto",
                columns: table => new
                {
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    photo = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userphoto", x => x.userid);
                    table.ForeignKey(
                        name: "FK_core_userphoto_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_usersecurity",
                columns: table => new
                {
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    pwdhash = table.Column<string>(type: "varchar", maxLength: 512, nullable: true),
                    lastmodified = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usersecurity", x => x.userid);
                    table.ForeignKey(
                        name: "FK_core_usersecurity_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_bus_integration_event_log",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type_name = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    times_sent = table.Column<int>(type: "integer", nullable: false),
                    create_on = table.Column<DateTime>(type: "timestamp", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_bus_integration_event_log", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_event_bus_integration_event_log_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_bunch_objects",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    right_node = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    left_node = table.Column<string>(type: "varchar", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_bunch_objects", x => new { x.tenant_id, x.right_node });
                    table.ForeignKey(
                        name: "FK_files_bunch_objects_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_file",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    version_group = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "1"),
                    current_version = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    folder_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    title = table.Column<string>(type: "character varying", maxLength: 400, nullable: false),
                    content_length = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "0"),
                    file_status = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    category = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    converted_type = table.Column<string>(type: "character varying", maxLength: 10, nullable: true),
                    comment = table.Column<string>(type: "character varying", maxLength: 255, nullable: true),
                    changes = table.Column<string>(type: "text", nullable: true),
                    encrypted = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    forcesave = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    thumb = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_file", x => new { x.tenant_id, x.id, x.version });
                    table.ForeignKey(
                        name: "FK_files_file_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_folder",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    title = table.Column<string>(type: "character varying", maxLength: 400, nullable: false),
                    folder_type = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    foldersCount = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    filesCount = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    counter = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_folder", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_folder_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_link",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    linked_id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    linked_for = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.source_id, x.linked_id });
                    table.ForeignKey(
                        name: "FK_files_link_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_order",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<int>(type: "integer", nullable: false),
                    entry_type = table.Column<short>(type: "smallint", nullable: false),
                    parent_folder_id = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("primary", x => new { x.tenant_id, x.entry_id, x.entry_type });
                    table.ForeignKey(
                        name: "FK_files_order_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_properties",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_properties", x => new { x.tenant_id, x.entry_id });
                    table.ForeignKey(
                        name: "FK_files_properties_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_security",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "pg_catalog.default"),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<int>(type: "integer", nullable: false),
                    owner = table.Column<Guid>(type: "uuid", nullable: false),
                    security = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    options = table.Column<FileShareOptions>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("files_security_pkey", x => new { x.tenant_id, x.entry_id, x.entry_type, x.subject });
                    table.ForeignKey(
                        name: "FK_files_security_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_tag",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "varchar", maxLength: 255, nullable: false, collation: "pg_catalog.default"),
                    owner = table.Column<Guid>(type: "uuid", nullable: false),
                    flag = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_tag_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_tag_link",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_on = table.Column<DateTime>(type: "timestamp", nullable: true),
                    tag_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_tag_link", x => new { x.tenant_id, x.tag_id, x.entry_id, x.entry_type });
                    table.ForeignKey(
                        name: "FK_files_tag_link_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_thirdparty_account",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    token = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folder_type = table.Column<int>(type: "integer", nullable: false),
                    room_type = table.Column<int>(type: "integer", nullable: false),
                    create_on = table.Column<DateTime>(type: "timestamp", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    folder_id = table.Column<string>(type: "text", nullable: true),
                    @private = table.Column<bool>(name: "private", type: "boolean", nullable: false),
                    has_logo = table.Column<bool>(type: "boolean", nullable: false),
                    color = table.Column<string>(type: "char(6)", maxLength: 6, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_thirdparty_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_thirdparty_account_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_thirdparty_app",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app = table.Column<string>(type: "character varying (50)", maxLength: 50, nullable: false),
                    token = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_thirdparty_app", x => new { x.user_id, x.app });
                    table.ForeignKey(
                        name: "FK_files_thirdparty_app_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_thirdparty_id_mapping",
                columns: table => new
                {
                    hash_id = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("files_thirdparty_id_mapping_pkey", x => x.hash_id);
                    table.ForeignKey(
                        name: "FK_files_thirdparty_id_mapping_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "firebase_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    firebase_device_token = table.Column<string>(type: "varchar", maxLength: 255, nullable: true),
                    application = table.Column<string>(type: "varchar", maxLength: 20, nullable: true),
                    is_subscribed = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireBaseUser", x => x.id);
                    table.ForeignKey(
                        name: "FK_firebase_users_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_clients",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    client_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    logo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    website_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    terms_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    policy_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    logout_redirect_uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    is_invalidated = table.Column<bool>(type: "boolean", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_clients", x => x.client_id);
                    table.ForeignKey(
                        name: "fk_identity_client_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "varchar", maxLength: 500, nullable: true),
                    ip = table.Column<string>(type: "varchar", maxLength: 50, nullable: true),
                    browser = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    platform = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page = table.Column<string>(type: "varchar", maxLength: 300, nullable: true),
                    action = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_login_events_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notify_queue",
                columns: table => new
                {
                    notify_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    sender = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reciever = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    subject = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    sender_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reply_to = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    creation_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    attachments = table.Column<string>(type: "text", nullable: true),
                    auto_submitted = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notify_queue", x => x.notify_id);
                    table.ForeignKey(
                        name: "FK_notify_queue_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "short_links",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    @short = table.Column<string>(name: "short", type: "char(15)", maxLength: 15, nullable: true),
                    link = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_short_links_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "telegram_users",
                columns: table => new
                {
                    portal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_user", x => new { x.tenant_id, x.portal_user_id });
                    table.ForeignKey(
                        name: "FK_telegram_users_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants_iprestrictions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    for_admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_iprestrictions", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenants_iprestrictions_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants_partners",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    partner_id = table.Column<string>(type: "character varying", maxLength: 36, nullable: true),
                    affiliate_id = table.Column<string>(type: "character varying", maxLength: 50, nullable: true),
                    campaign = table.Column<string>(type: "character varying", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tenant_partner_pkey", x => x.tenant_id);
                    table.ForeignKey(
                        name: "FK_tenants_partners_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants_quotarow",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counter = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "0"),
                    tag = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_quotarow", x => new { x.tenant, x.user_id, x.path });
                    table.ForeignKey(
                        name: "FK_tenants_quotarow_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants_tariff",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    stamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    customer_id = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    comment = table.Column<string>(type: "varchar", maxLength: 255, nullable: true),
                    create_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_tariff", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenants_tariff_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants_tariffrow",
                columns: table => new
                {
                    tariff_id = table.Column<int>(type: "integer", nullable: false),
                    quota = table.Column<int>(type: "integer", nullable: false),
                    tenant = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tenants_tariffrow_pkey", x => new { x.tenant, x.tariff_id, x.quota });
                    table.ForeignKey(
                        name: "FK_tenants_tariffrow_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying", maxLength: 64, nullable: true),
                    last_name = table.Column<string>(type: "character varying", maxLength: 64, nullable: true),
                    sex = table.Column<bool>(type: "boolean", nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    activation_status = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying", maxLength: 255, nullable: true),
                    WorkFromDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    title = table.Column<string>(type: "character varying", maxLength: 64, nullable: true),
                    culture_name = table.Column<string>(type: "character varying", maxLength: 20, nullable: true),
                    contacts = table.Column<string>(type: "character varying", maxLength: 1024, nullable: true),
                    mobile_phone = table.Column<string>(type: "character varying", maxLength: 255, nullable: true),
                    mobile_phone_activation = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "character varying", maxLength: 512, nullable: true),
                    Sid = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SsoNameId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SsoSessionId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    removed = table.Column<bool>(type: "boolean", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Spam = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                    table.ForeignKey(
                        name: "FK_Users_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhooks_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying", maxLength: 50, nullable: false),
                    secret_key = table.Column<string>(type: "text", maxLength: 50, nullable: true, defaultValueSql: "''"),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    uri = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    ssl = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true")
                },
                constraints: table =>
                {
                    table.PrimaryKey("webhooks_config_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_webhooks_config_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webstudio_settings",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webstudio_settings", x => new { x.tenant_id, x.id, x.user_id });
                    table.ForeignKey(
                        name: "FK_webstudio_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_audit_reference",
                columns: table => new
                {
                    entry_id = table.Column<int>(type: "integer", nullable: false),
                    entry_type = table.Column<byte>(type: "smallint", nullable: false),
                    audit_event_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_audit_reference", x => new { x.entry_id, x.entry_type, x.audit_event_id });
                    table.ForeignKey(
                        name: "FK_files_audit_reference_audit_events_audit_event_id",
                        column: x => x.audit_event_id,
                        principalTable: "audit_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_folder_tree",
                columns: table => new
                {
                    folder_id = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_folder_tree", x => new { x.parent_id, x.folder_id });
                    table.ForeignKey(
                        name: "FK_files_folder_tree_files_folder_folder_id",
                        column: x => x.folder_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files_room_settings",
                columns: table => new
                {
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    @private = table.Column<bool>(name: "private", type: "boolean", nullable: false, defaultValueSql: "false"),
                    has_logo = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    color = table.Column<string>(type: "char(6)", maxLength: 6, nullable: true),
                    cover = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    indexing = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    quota = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "-2"),
                    watermark = table.Column<DbRoomWatermark>(type: "jsonb", nullable: true),
                    deny_download = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    lifetime = table.Column<DbRoomDataLifetime>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files_room_settings", x => new { x.tenant_id, x.room_id });
                    table.ForeignKey(
                        name: "FK_files_room_settings_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_files_room_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_client_allowed_origins",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    allowed_origin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_allowed_origins_ibfk_1",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_client_authentication_methods",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    authentication_method = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_authentication_methods_fk_client_id",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_client_redirect_uris",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_redirect_uris_fk_client",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_client_scopes",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    scope_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_scopes_client_id_fk",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "identity_client_scopes_scope_name_fk",
                        column: x => x.scope_name,
                        principalTable: "identity_scopes",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_consents",
                columns: table => new
                {
                    principal_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    registered_client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    is_invalidated = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_consents", x => new { x.principal_id, x.registered_client_id });
                    table.ForeignKey(
                        name: "fk_identity_consents_registered_client",
                        column: x => x.registered_client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdentityAuthorizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RegisteredClientId = table.Column<string>(type: "text", nullable: false),
                    PrincipalId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "text", nullable: true),
                    Attributes = table.Column<string>(type: "jsonb", nullable: true),
                    AuthorizationGrantType = table.Column<string>(type: "text", nullable: true),
                    AuthorizedScopes = table.Column<string>(type: "text", nullable: true),
                    AuthorizationCodeValue = table.Column<string>(type: "text", nullable: true),
                    AuthorizationCodeMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    AuthorizationCodeIssuedAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    AuthorizationCodeExpiresAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    AccessTokenType = table.Column<string>(type: "text", nullable: true),
                    AccessTokenValue = table.Column<string>(type: "text", nullable: true),
                    AccessTokenHash = table.Column<string>(type: "text", nullable: true),
                    AccessTokenScopes = table.Column<string>(type: "text", nullable: true),
                    AccessTokenMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    AccessTokenIssuedAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    RefreshTokenValue = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    RefreshTokenIssuedAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    IsInvalidated = table.Column<bool>(type: "boolean", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp", nullable: true),
                    IdentityClientClientId = table.Column<string>(type: "character varying(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityAuthorizations_identity_clients_IdentityClientClien~",
                        column: x => x.IdentityClientClientId,
                        principalTable: "identity_clients",
                        principalColumn: "client_id");
                    table.ForeignKey(
                        name: "FK_IdentityAuthorizations_identity_clients_RegisteredClientId",
                        column: x => x.RegisteredClientId,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdentityAuthorizations_tenants_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhooks_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<int>(type: "integer", nullable: false),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    webhook_id = table.Column<int>(type: "integer", nullable: false),
                    request_headers = table.Column<string>(type: "json", nullable: true),
                    request_payload = table.Column<string>(type: "text", nullable: true),
                    response_headers = table.Column<string>(type: "json", nullable: true),
                    response_payload = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    uid = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhooks_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_webhooks_logs_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_webhooks_logs_webhooks_config_config_id",
                        column: x => x.config_id,
                        principalTable: "webhooks_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identity_consent_scopes",
                columns: table => new
                {
                    principal_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    registered_client_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    scope_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_consent_scopes", x => new { x.principal_id, x.registered_client_id, x.scope_name });
                    table.ForeignKey(
                        name: "fk_identity_consent_scopes_principal_client",
                        columns: x => new { x.principal_id, x.registered_client_id },
                        principalTable: "identity_consents",
                        principalColumns: new[] { "principal_id", "registered_client_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_consent_scopes_scope_name",
                        column: x => x.scope_name,
                        principalTable: "identity_scopes",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "files_converts",
                columns: new[] { "input", "output" },
                values: new object[,]
                {
                    { ".csv", ".ods" },
                    { ".csv", ".ots" },
                    { ".csv", ".pdf" },
                    { ".csv", ".xlsm" },
                    { ".csv", ".xlsx" },
                    { ".csv", ".xltm" },
                    { ".csv", ".xltx" },
                    { ".djvu", ".pdf" },
                    { ".doc", ".docm" },
                    { ".doc", ".docx" },
                    { ".doc", ".dotm" },
                    { ".doc", ".dotx" },
                    { ".doc", ".epub" },
                    { ".doc", ".fb2" },
                    { ".doc", ".html" },
                    { ".doc", ".odt" },
                    { ".doc", ".ott" },
                    { ".doc", ".pdf" },
                    { ".doc", ".rtf" },
                    { ".doc", ".txt" },
                    { ".docm", ".docx" },
                    { ".docm", ".dotm" },
                    { ".docm", ".dotx" },
                    { ".docm", ".epub" },
                    { ".docm", ".fb2" },
                    { ".docm", ".html" },
                    { ".docm", ".odt" },
                    { ".docm", ".ott" },
                    { ".docm", ".pdf" },
                    { ".docm", ".rtf" },
                    { ".docm", ".txt" },
                    { ".doct", ".docx" },
                    { ".docx", ".docm" },
                    { ".docx", ".dotm" },
                    { ".docx", ".dotx" },
                    { ".docx", ".epub" },
                    { ".docx", ".fb2" },
                    { ".docx", ".html" },
                    { ".docx", ".odt" },
                    { ".docx", ".ott" },
                    { ".docx", ".pdf" },
                    { ".docx", ".rtf" },
                    { ".docx", ".txt" },
                    { ".docxf", ".docm" },
                    { ".docxf", ".docx" },
                    { ".docxf", ".dotm" },
                    { ".docxf", ".dotx" },
                    { ".docxf", ".epub" },
                    { ".docxf", ".fb2" },
                    { ".docxf", ".html" },
                    { ".docxf", ".odt" },
                    { ".docxf", ".ott" },
                    { ".docxf", ".pdf" },
                    { ".docxf", ".rtf" },
                    { ".docxf", ".txt" },
                    { ".dot", ".docm" },
                    { ".dot", ".docx" },
                    { ".dot", ".dotm" },
                    { ".dot", ".dotx" },
                    { ".dot", ".epub" },
                    { ".dot", ".fb2" },
                    { ".dot", ".html" },
                    { ".dot", ".odt" },
                    { ".dot", ".ott" },
                    { ".dot", ".pdf" },
                    { ".dot", ".rtf" },
                    { ".dot", ".txt" },
                    { ".dotm", ".docm" },
                    { ".dotm", ".docx" },
                    { ".dotm", ".dotx" },
                    { ".dotm", ".epub" },
                    { ".dotm", ".fb2" },
                    { ".dotm", ".html" },
                    { ".dotm", ".odt" },
                    { ".dotm", ".ott" },
                    { ".dotm", ".pdf" },
                    { ".dotm", ".rtf" },
                    { ".dotm", ".txt" },
                    { ".dotx", ".docm" },
                    { ".dotx", ".docx" },
                    { ".dotx", ".dotm" },
                    { ".dotx", ".epub" },
                    { ".dotx", ".fb2" },
                    { ".dotx", ".html" },
                    { ".dotx", ".odt" },
                    { ".dotx", ".ott" },
                    { ".dotx", ".pdf" },
                    { ".dotx", ".rtf" },
                    { ".dotx", ".txt" },
                    { ".dps", ".odp" },
                    { ".dps", ".otp" },
                    { ".dps", ".pdf" },
                    { ".dps", ".potm" },
                    { ".dps", ".potx" },
                    { ".dps", ".ppsm" },
                    { ".dps", ".ppsx" },
                    { ".dps", ".pptm" },
                    { ".dps", ".pptx" },
                    { ".dpt", ".odp" },
                    { ".dpt", ".otp" },
                    { ".dpt", ".pdf" },
                    { ".dpt", ".potm" },
                    { ".dpt", ".potx" },
                    { ".dpt", ".ppsm" },
                    { ".dpt", ".ppsx" },
                    { ".dpt", ".pptm" },
                    { ".dpt", ".pptx" },
                    { ".epub", ".docm" },
                    { ".epub", ".docx" },
                    { ".epub", ".dotm" },
                    { ".epub", ".dotx" },
                    { ".epub", ".fb2" },
                    { ".epub", ".html" },
                    { ".epub", ".odt" },
                    { ".epub", ".ott" },
                    { ".epub", ".pdf" },
                    { ".epub", ".rtf" },
                    { ".epub", ".txt" },
                    { ".et", ".csv" },
                    { ".et", ".ods" },
                    { ".et", ".ots" },
                    { ".et", ".pdf" },
                    { ".et", ".xlsm" },
                    { ".et", ".xlsx" },
                    { ".et", ".xltm" },
                    { ".et", ".xltx" },
                    { ".ett", ".csv" },
                    { ".ett", ".ods" },
                    { ".ett", ".ots" },
                    { ".ett", ".pdf" },
                    { ".ett", ".xlsm" },
                    { ".ett", ".xlsx" },
                    { ".ett", ".xltm" },
                    { ".ett", ".xltx" },
                    { ".fb2", ".docm" },
                    { ".fb2", ".docx" },
                    { ".fb2", ".dotm" },
                    { ".fb2", ".dotx" },
                    { ".fb2", ".epub" },
                    { ".fb2", ".html" },
                    { ".fb2", ".odt" },
                    { ".fb2", ".ott" },
                    { ".fb2", ".pdf" },
                    { ".fb2", ".rtf" },
                    { ".fb2", ".txt" },
                    { ".fodp", ".odp" },
                    { ".fodp", ".otp" },
                    { ".fodp", ".pdf" },
                    { ".fodp", ".potm" },
                    { ".fodp", ".potx" },
                    { ".fodp", ".ppsm" },
                    { ".fodp", ".ppsx" },
                    { ".fodp", ".pptm" },
                    { ".fodp", ".pptx" },
                    { ".fods", ".csv" },
                    { ".fods", ".ods" },
                    { ".fods", ".ots" },
                    { ".fods", ".pdf" },
                    { ".fods", ".xlsm" },
                    { ".fods", ".xlsx" },
                    { ".fods", ".xltm" },
                    { ".fods", ".xltx" },
                    { ".fodt", ".docm" },
                    { ".fodt", ".docx" },
                    { ".fodt", ".dotm" },
                    { ".fodt", ".dotx" },
                    { ".fodt", ".epub" },
                    { ".fodt", ".fb2" },
                    { ".fodt", ".html" },
                    { ".fodt", ".odt" },
                    { ".fodt", ".ott" },
                    { ".fodt", ".pdf" },
                    { ".fodt", ".rtf" },
                    { ".fodt", ".txt" },
                    { ".htm", ".docm" },
                    { ".htm", ".docx" },
                    { ".htm", ".dotm" },
                    { ".htm", ".dotx" },
                    { ".htm", ".epub" },
                    { ".htm", ".fb2" },
                    { ".htm", ".html" },
                    { ".htm", ".odt" },
                    { ".htm", ".ott" },
                    { ".htm", ".pdf" },
                    { ".htm", ".rtf" },
                    { ".htm", ".txt" },
                    { ".html", ".docm" },
                    { ".html", ".docx" },
                    { ".html", ".dotm" },
                    { ".html", ".dotx" },
                    { ".html", ".epub" },
                    { ".html", ".fb2" },
                    { ".html", ".odt" },
                    { ".html", ".ott" },
                    { ".html", ".pdf" },
                    { ".html", ".rtf" },
                    { ".html", ".txt" },
                    { ".mht", ".docm" },
                    { ".mht", ".docx" },
                    { ".mht", ".dotm" },
                    { ".mht", ".dotx" },
                    { ".mht", ".epub" },
                    { ".mht", ".fb2" },
                    { ".mht", ".html" },
                    { ".mht", ".odt" },
                    { ".mht", ".ott" },
                    { ".mht", ".pdf" },
                    { ".mht", ".rtf" },
                    { ".mht", ".txt" },
                    { ".mhtml", ".docm" },
                    { ".mhtml", ".docx" },
                    { ".mhtml", ".dotm" },
                    { ".mhtml", ".dotx" },
                    { ".mhtml", ".epub" },
                    { ".mhtml", ".fb2" },
                    { ".mhtml", ".html" },
                    { ".mhtml", ".odt" },
                    { ".mhtml", ".ott" },
                    { ".mhtml", ".pdf" },
                    { ".mhtml", ".rtf" },
                    { ".mhtml", ".txt" },
                    { ".odp", ".otp" },
                    { ".odp", ".pdf" },
                    { ".odp", ".potm" },
                    { ".odp", ".potx" },
                    { ".odp", ".ppsm" },
                    { ".odp", ".ppsx" },
                    { ".odp", ".pptm" },
                    { ".odp", ".pptx" },
                    { ".ods", ".csv" },
                    { ".ods", ".ots" },
                    { ".ods", ".pdf" },
                    { ".ods", ".xlsm" },
                    { ".ods", ".xlsx" },
                    { ".ods", ".xltm" },
                    { ".ods", ".xltx" },
                    { ".odt", ".docm" },
                    { ".odt", ".docx" },
                    { ".odt", ".dotm" },
                    { ".odt", ".dotx" },
                    { ".odt", ".epub" },
                    { ".odt", ".fb2" },
                    { ".odt", ".html" },
                    { ".odt", ".ott" },
                    { ".odt", ".pdf" },
                    { ".odt", ".rtf" },
                    { ".odt", ".txt" },
                    { ".oform", ".pdf" },
                    { ".otp", ".odp" },
                    { ".otp", ".pdf" },
                    { ".otp", ".potm" },
                    { ".otp", ".potx" },
                    { ".otp", ".ppsm" },
                    { ".otp", ".ppsx" },
                    { ".otp", ".pptm" },
                    { ".otp", ".pptx" },
                    { ".ots", ".csv" },
                    { ".ots", ".ods" },
                    { ".ots", ".pdf" },
                    { ".ots", ".xlsm" },
                    { ".ots", ".xlsx" },
                    { ".ots", ".xltm" },
                    { ".ots", ".xltx" },
                    { ".ott", ".docm" },
                    { ".ott", ".docx" },
                    { ".ott", ".dotm" },
                    { ".ott", ".dotx" },
                    { ".ott", ".epub" },
                    { ".ott", ".fb2" },
                    { ".ott", ".html" },
                    { ".ott", ".odt" },
                    { ".ott", ".pdf" },
                    { ".ott", ".rtf" },
                    { ".ott", ".txt" },
                    { ".oxps", ".docm" },
                    { ".oxps", ".docx" },
                    { ".oxps", ".dotm" },
                    { ".oxps", ".dotx" },
                    { ".oxps", ".epub" },
                    { ".oxps", ".fb2" },
                    { ".oxps", ".html" },
                    { ".oxps", ".odt" },
                    { ".oxps", ".ott" },
                    { ".oxps", ".pdf" },
                    { ".oxps", ".rtf" },
                    { ".oxps", ".txt" },
                    { ".pdf", ".docm" },
                    { ".pdf", ".docx" },
                    { ".pdf", ".dotm" },
                    { ".pdf", ".dotx" },
                    { ".pdf", ".epub" },
                    { ".pdf", ".fb2" },
                    { ".pdf", ".html" },
                    { ".pdf", ".odt" },
                    { ".pdf", ".ott" },
                    { ".pdf", ".rtf" },
                    { ".pdf", ".txt" },
                    { ".pot", ".odp" },
                    { ".pot", ".otp" },
                    { ".pot", ".pdf" },
                    { ".pot", ".potm" },
                    { ".pot", ".potx" },
                    { ".pot", ".ppsm" },
                    { ".pot", ".ppsx" },
                    { ".pot", ".pptm" },
                    { ".pot", ".pptx" },
                    { ".potm", ".odp" },
                    { ".potm", ".otp" },
                    { ".potm", ".pdf" },
                    { ".potm", ".potx" },
                    { ".potm", ".ppsm" },
                    { ".potm", ".ppsx" },
                    { ".potm", ".pptm" },
                    { ".potm", ".pptx" },
                    { ".potx", ".odp" },
                    { ".potx", ".otp" },
                    { ".potx", ".pdf" },
                    { ".potx", ".potm" },
                    { ".potx", ".ppsm" },
                    { ".potx", ".ppsx" },
                    { ".potx", ".pptm" },
                    { ".potx", ".pptx" },
                    { ".pps", ".odp" },
                    { ".pps", ".otp" },
                    { ".pps", ".pdf" },
                    { ".pps", ".potm" },
                    { ".pps", ".potx" },
                    { ".pps", ".ppsm" },
                    { ".pps", ".ppsx" },
                    { ".pps", ".pptm" },
                    { ".pps", ".pptx" },
                    { ".ppsm", ".odp" },
                    { ".ppsm", ".otp" },
                    { ".ppsm", ".pdf" },
                    { ".ppsm", ".potm" },
                    { ".ppsm", ".potx" },
                    { ".ppsm", ".ppsx" },
                    { ".ppsm", ".pptm" },
                    { ".ppsm", ".pptx" },
                    { ".ppsx", ".odp" },
                    { ".ppsx", ".otp" },
                    { ".ppsx", ".pdf" },
                    { ".ppsx", ".potm" },
                    { ".ppsx", ".potx" },
                    { ".ppsx", ".ppsm" },
                    { ".ppsx", ".pptm" },
                    { ".ppsx", ".pptx" },
                    { ".ppt", ".odp" },
                    { ".ppt", ".otp" },
                    { ".ppt", ".pdf" },
                    { ".ppt", ".potm" },
                    { ".ppt", ".potx" },
                    { ".ppt", ".ppsm" },
                    { ".ppt", ".ppsx" },
                    { ".ppt", ".pptm" },
                    { ".ppt", ".pptx" },
                    { ".pptm", ".odp" },
                    { ".pptm", ".otp" },
                    { ".pptm", ".pdf" },
                    { ".pptm", ".potm" },
                    { ".pptm", ".potx" },
                    { ".pptm", ".ppsm" },
                    { ".pptm", ".ppsx" },
                    { ".pptm", ".pptx" },
                    { ".pptt", ".pptx" },
                    { ".pptx", ".odp" },
                    { ".pptx", ".otp" },
                    { ".pptx", ".pdf" },
                    { ".pptx", ".potm" },
                    { ".pptx", ".potx" },
                    { ".pptx", ".ppsm" },
                    { ".pptx", ".ppsx" },
                    { ".pptx", ".pptm" },
                    { ".rtf", ".docm" },
                    { ".rtf", ".docx" },
                    { ".rtf", ".dotm" },
                    { ".rtf", ".dotx" },
                    { ".rtf", ".epub" },
                    { ".rtf", ".fb2" },
                    { ".rtf", ".html" },
                    { ".rtf", ".odt" },
                    { ".rtf", ".ott" },
                    { ".rtf", ".pdf" },
                    { ".rtf", ".txt" },
                    { ".stw", ".docm" },
                    { ".stw", ".docx" },
                    { ".stw", ".dotm" },
                    { ".stw", ".dotx" },
                    { ".stw", ".epub" },
                    { ".stw", ".fb2" },
                    { ".stw", ".html" },
                    { ".stw", ".odt" },
                    { ".stw", ".ott" },
                    { ".stw", ".pdf" },
                    { ".stw", ".rtf" },
                    { ".stw", ".txt" },
                    { ".sxc", ".csv" },
                    { ".sxc", ".ods" },
                    { ".sxc", ".ots" },
                    { ".sxc", ".pdf" },
                    { ".sxc", ".xlsm" },
                    { ".sxc", ".xlsx" },
                    { ".sxc", ".xltm" },
                    { ".sxc", ".xltx" },
                    { ".sxi", ".odp" },
                    { ".sxi", ".otp" },
                    { ".sxi", ".pdf" },
                    { ".sxi", ".potm" },
                    { ".sxi", ".potx" },
                    { ".sxi", ".ppsm" },
                    { ".sxi", ".ppsx" },
                    { ".sxi", ".pptm" },
                    { ".sxi", ".pptx" },
                    { ".sxw", ".docm" },
                    { ".sxw", ".docx" },
                    { ".sxw", ".dotm" },
                    { ".sxw", ".dotx" },
                    { ".sxw", ".epub" },
                    { ".sxw", ".fb2" },
                    { ".sxw", ".html" },
                    { ".sxw", ".odt" },
                    { ".sxw", ".ott" },
                    { ".sxw", ".pdf" },
                    { ".sxw", ".rtf" },
                    { ".sxw", ".txt" },
                    { ".txt", ".docm" },
                    { ".txt", ".docx" },
                    { ".txt", ".dotm" },
                    { ".txt", ".dotx" },
                    { ".txt", ".epub" },
                    { ".txt", ".fb2" },
                    { ".txt", ".html" },
                    { ".txt", ".odt" },
                    { ".txt", ".ott" },
                    { ".txt", ".pdf" },
                    { ".txt", ".rtf" },
                    { ".wps", ".docm" },
                    { ".wps", ".docx" },
                    { ".wps", ".dotm" },
                    { ".wps", ".dotx" },
                    { ".wps", ".epub" },
                    { ".wps", ".fb2" },
                    { ".wps", ".html" },
                    { ".wps", ".odt" },
                    { ".wps", ".ott" },
                    { ".wps", ".pdf" },
                    { ".wps", ".rtf" },
                    { ".wps", ".txt" },
                    { ".wpt", ".docm" },
                    { ".wpt", ".docx" },
                    { ".wpt", ".dotm" },
                    { ".wpt", ".dotx" },
                    { ".wpt", ".epub" },
                    { ".wpt", ".fb2" },
                    { ".wpt", ".html" },
                    { ".wpt", ".odt" },
                    { ".wpt", ".ott" },
                    { ".wpt", ".pdf" },
                    { ".wpt", ".rtf" },
                    { ".wpt", ".txt" },
                    { ".xls", ".csv" },
                    { ".xls", ".ods" },
                    { ".xls", ".ots" },
                    { ".xls", ".pdf" },
                    { ".xls", ".xlsm" },
                    { ".xls", ".xlsx" },
                    { ".xls", ".xltm" },
                    { ".xls", ".xltx" },
                    { ".xlsb", ".csv" },
                    { ".xlsb", ".ods" },
                    { ".xlsb", ".ots" },
                    { ".xlsb", ".pdf" },
                    { ".xlsb", ".xlsm" },
                    { ".xlsb", ".xlsx" },
                    { ".xlsb", ".xltm" },
                    { ".xlsb", ".xltx" },
                    { ".xlsm", ".csv" },
                    { ".xlsm", ".ods" },
                    { ".xlsm", ".ots" },
                    { ".xlsm", ".pdf" },
                    { ".xlsm", ".xlsx" },
                    { ".xlsm", ".xltm" },
                    { ".xlsm", ".xltx" },
                    { ".xlst", ".xlsx" },
                    { ".xlsx", ".csv" },
                    { ".xlsx", ".ods" },
                    { ".xlsx", ".ots" },
                    { ".xlsx", ".pdf" },
                    { ".xlsx", ".xlsm" },
                    { ".xlsx", ".xltm" },
                    { ".xlsx", ".xltx" },
                    { ".xlt", ".csv" },
                    { ".xlt", ".ods" },
                    { ".xlt", ".ots" },
                    { ".xlt", ".pdf" },
                    { ".xlt", ".xlsm" },
                    { ".xlt", ".xlsx" },
                    { ".xlt", ".xltm" },
                    { ".xlt", ".xltx" },
                    { ".xltm", ".csv" },
                    { ".xltm", ".ods" },
                    { ".xltm", ".ots" },
                    { ".xltm", ".pdf" },
                    { ".xltm", ".xlsm" },
                    { ".xltm", ".xlsx" },
                    { ".xltm", ".xltx" },
                    { ".xltx", ".csv" },
                    { ".xltx", ".ods" },
                    { ".xltx", ".ots" },
                    { ".xltx", ".pdf" },
                    { ".xltx", ".xlsm" },
                    { ".xltx", ".xlsx" },
                    { ".xltx", ".xltm" },
                    { ".xml", ".docm" },
                    { ".xml", ".docx" },
                    { ".xml", ".dotm" },
                    { ".xml", ".dotx" },
                    { ".xml", ".epub" },
                    { ".xml", ".fb2" },
                    { ".xml", ".html" },
                    { ".xml", ".odt" },
                    { ".xml", ".ott" },
                    { ".xml", ".pdf" },
                    { ".xml", ".rtf" },
                    { ".xml", ".txt" },
                    { ".xps", ".docm" },
                    { ".xps", ".docx" },
                    { ".xps", ".dotm" },
                    { ".xps", ".dotx" },
                    { ".xps", ".epub" },
                    { ".xps", ".fb2" },
                    { ".xps", ".html" },
                    { ".xps", ".odt" },
                    { ".xps", ".ott" },
                    { ".xps", ".pdf" },
                    { ".xps", ".rtf" },
                    { ".xps", ".txt" }
                });

            migrationBuilder.InsertData(
                table: "identity_scopes",
                columns: new[] { "name", "group", "type" },
                values: new object[,]
                {
                    { "accounts:read", "accounts", "read" },
                    { "accounts:write", "accounts", "write" },
                    { "accounts.self:read", "profiles", "read" },
                    { "accounts.self:write", "profiles", "write" },
                    { "files:read", "files", "read" },
                    { "files:write", "files", "write" },
                    { "openid", "openid", "openid" },
                    { "rooms:read", "rooms", "read" },
                    { "rooms:write", "rooms", "write" }
                });

            migrationBuilder.InsertData(
                table: "tenants_forbiden",
                column: "address",
                values: new object[]
                {
                    "controlpanel",
                    "localhost"
                });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "visible" },
                values: new object[] { -9, "since 01.04.2024", "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic", "admin", 20m, "1006", true });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "product_id" },
                values: new object[,]
                {
                    { -8, null, "free,oauth,total_size:107374182400,manager:100,room:100", "zoom", null },
                    { -7, null, "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20,statistic", "nonprofit", "1007" },
                    { -6, null, "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic", "subscription", "1001" },
                    { -5, null, "manager:1", "admin1", "1005" },
                    { -4, null, "total_size:1073741824", "disk", "1004" },
                    { -3, null, "free,oauth,total_size:2147483648,manager:3,room:12", "startup", null }
                });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id" },
                values: new object[] { -2, "until 01.04.2024", "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic", "admin", 15m, "1002" });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "product_id" },
                values: new object[] { -1, null, "trial,audit,ldap,sso,customization,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic", "trial", null });

            migrationBuilder.InsertData(
                table: "tenants_tenants",
                columns: new[] { "id", "alias", "creationdatetime", "last_modified", "mappeddomain", "name", "owner_id", "payment_id", "status", "statuschanged", "timezone", "trusteddomains", "version_changed" },
                values: new object[] { -1, "settings", new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Web Office", new Guid("00000000-0000-0000-0000-000000000000"), null, 1, null, null, null, null });

            migrationBuilder.InsertData(
                table: "tenants_tenants",
                columns: new[] { "id", "alias", "creationdatetime", "last_modified", "mappeddomain", "name", "owner_id", "payment_id", "statuschanged", "timezone", "trusteddomains", "version_changed" },
                values: new object[] { 1, "localhost", new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Web Office", new Guid("66faa6e4-f133-11ea-b126-00ffeec8b4ef"), null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "id", "activation_status", "birth_date", "contacts", "create_date", "CreatedBy", "culture_name", "email", "first_name", "last_modified", "last_name", "location", "mobile_phone", "mobile_phone_activation", "notes", "removed", "sex", "Sid", "Spam", "SsoNameId", "SsoSessionId", "status", "tenant_id", "TerminatedDate", "title", "username", "WorkFromDate" },
                values: new object[] { new Guid("66faa6e4-f133-11ea-b126-00ffeec8b4ef"), 0, null, null, new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "", "Administrator", new DateTime(2021, 3, 9, 9, 52, 55, 765, DateTimeKind.Utc).AddTicks(1420), "", null, null, 0, null, false, null, null, null, null, null, 1, 1, null, null, "administrator", new DateTime(2021, 3, 9, 9, 52, 55, 764, DateTimeKind.Utc).AddTicks(9157) });

            migrationBuilder.InsertData(
                table: "core_acl",
                columns: new[] { "action", "object", "subject", "tenant", "acetype" },
                values: new object[,]
                {
                    { new Guid("ef5e6790-f346-4b6e-b662-722bc28cb0db"), "", new Guid("5d5b7260-f7f7-49f1-a1c9-95fbb6a12604"), -1, 0 },
                    { new Guid("f11e8f3f-46e6-4e55-90e3-09c22ec565bd"), "", new Guid("5d5b7260-f7f7-49f1-a1c9-95fbb6a12604"), -1, 0 },
                    { new Guid("e0759a42-47f0-4763-a26a-d5aa665bec35"), "", new Guid("712d9ec3-5d2b-4b13-824f-71f00191dcca"), -1, 0 },
                    { new Guid("3e74aff2-7c0c-4089-b209-6495b8643471"), "", new Guid("88f11e7c-7407-4bea-b4cb-070010cdbb6b"), -1, 0 },
                    { new Guid("08d75c97-cf3f-494b-90d1-751c941fe2dd"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("0d1f72a8-63da-47ea-ae42-0900e4ac72a9"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("13e30b51-5b4d-40a5-8575-cb561899eeb1"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("19f658ae-722b-4cd8-8236-3ad150801d96"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("2c6552b3-b2e0-4a00-b8fd-13c161e337b1"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("3e74aff2-7c0c-4089-b209-6495b8643471"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("40bf31f4-3132-4e76-8d5c-9828a89501a3"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("49ae8915-2b30-4348-ab74-b152279364fb"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("948ad738-434b-4a88-8e38-7569d332910a"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("9d75a568-52aa-49d8-ad43-473756cd8903"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("d49f4e30-da10-4b39-bc6d-b41ef6e039d3"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("d852b66f-6719-45e1-8657-18f0bb791690"), "", new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), -1, 0 },
                    { new Guid("13e30b51-5b4d-40a5-8575-cb561899eeb1"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("49ae8915-2b30-4348-ab74-b152279364fb"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("63e9f35f-6bb5-4fb1-afaa-e4c2f4dec9bd"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("9018c001-24c2-44bf-a1db-d1121a570e74"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("d1f3b53d-d9e2-4259-80e7-d24380978395"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("e0759a42-47f0-4763-a26a-d5aa665bec35"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("e37239bd-c5b5-4f1e-a9f8-3ceeac209615"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("f11e88d7-f185-4372-927c-d88008d2c483"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("f11e8f3f-46e6-4e55-90e3-09c22ec565bd"), "", new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), -1, 0 },
                    { new Guid("00e7dfc5-ac49-4fd3-a1d6-98d84e877ac4"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("0d68b142-e20a-446e-a832-0d6b0b65a164"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("14be970f-7af5-4590-8e81-ea32b5f7866d"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("18ecc94d-6afa-4994-8406-aee9dff12ce2"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("298530eb-435e-4dc6-a776-9abcd95c70e9"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("430eaf70-1886-483c-a746-1a18e3e6bb63"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("557d6503-633b-4490-a14c-6473147ce2b3"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("662f3db7-9bc8-42cf-84da-2765f563e9b0"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("724cbb75-d1c9-451e-bae0-4de0db96b1f7"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("7cb5c0d1-d254-433f-abe3-ff23373ec631"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("91b29dcd-9430-4403-b17a-27d09189be88"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("a18480a4-6d18-4c71-84fa-789888791f45"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("b630d29b-1844-4bda-bbbe-cf5542df3559"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("c62a9e8d-b24c-4513-90aa-7ff0f8ba38eb"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("d7cdb020-288b-41e5-a857-597347618533"), "", new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), -1, 0 },
                    { new Guid("088d5940-a80f-4403-9741-d610718ce95c"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("08d66144-e1c9-4065-9aa1-aa4bba0a7bc8"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("388c29d3-c662-4a61-bf47-fc2f7094224a"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("63e9f35f-6bb5-4fb1-afaa-e4c2f4dec9bd"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("6f05c382-8bca-4469-9424-c807a98c40d7"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|1e04460243b54d7982f3fd6208a11960", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|28b10049dd204f54b986873bc14ccfc7", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|2a9230378b2d487b9a225ac0918acf3f", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|32d24cb57ece46069c9419216ba42086", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|37620ae5c40b45ce855a39dd7d76a1fa", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|3cfd481b46f24a4ab55cb8c0c9def02c", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|46cfa73af32046cf8d5bcd82e1d67f26", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|6743007c6f954d208c88a8601ce5e76d", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|6a598c7491ae437da5f4ad339bd11bb2", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|742cf945cbbc4a5782d61600a12cf8ca", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|853b6eb973ee438d9b098ffeedf36234", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|bf88953e3c434850a3fbb1e43ad53a3e", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|e67be73df9ae4ce18fec1880cb518cb4", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|ea942538e68e49079394035336ee0ba8", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 1 },
                    { new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ASC.Web.Core.WebItemSecurity+WebItemSecurityObject|f4d98afdd336433287783c6945c81ea0", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("9018c001-24c2-44bf-a1db-d1121a570e74"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("a362fe79-684e-4d43-a599-65bc1f4e167f"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("c426c349-9ad4-47cd-9b8f-99fc30675951"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("d11ebcb9-0e6e-45e6-a6d0-99c41d687598"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("d1f3b53d-d9e2-4259-80e7-d24380978395"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("e0759a42-47f0-4763-a26a-d5aa665bec35"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("e37239bd-c5b5-4f1e-a9f8-3ceeac209615"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("fbc37705-a04c-40ad-a68c-ce2f0423f397"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 },
                    { new Guid("fcac42b8-9386-48eb-a938-d19b3c576912"), "", new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), -1, 0 }
                });

            migrationBuilder.InsertData(
                table: "core_settings",
                columns: new[] { "id", "tenant", "last_modified", "value" },
                values: new object[,]
                {
                    { "CompanyWhiteLabelSettings", -1, new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new byte[] { 245, 71, 4, 138, 72, 101, 23, 21, 135, 217, 206, 188, 138, 73, 108, 96, 29, 150, 3, 31, 44, 28, 62, 145, 96, 53, 57, 66, 238, 118, 93, 172, 211, 22, 244, 181, 244, 40, 146, 67, 111, 196, 162, 27, 154, 109, 248, 255, 181, 17, 253, 127, 42, 65, 19, 90, 26, 206, 203, 145, 159, 159, 243, 105, 24, 71, 188, 165, 53, 85, 57, 37, 186, 251, 57, 96, 18, 162, 218, 80, 0, 101, 250, 100, 66, 97, 24, 51, 240, 215, 216, 169, 105, 100, 15, 253, 29, 83, 182, 236, 203, 53, 68, 251, 2, 150, 149, 148, 58, 136, 84, 37, 151, 82, 92, 227, 30, 52, 111, 40, 154, 155, 7, 126, 149, 100, 169, 87, 10, 129, 228, 138, 177, 101, 77, 67, 177, 216, 189, 201, 1, 213, 136, 216, 107, 198, 253, 221, 106, 255, 198, 17, 68, 14, 110, 90, 174, 182, 68, 222, 188, 77, 157, 19, 26, 68, 86, 97, 15, 81, 24, 171, 214, 114, 191, 175, 56, 56, 48, 52, 125, 82, 253, 113, 71, 41, 201, 5, 8, 118, 162, 191, 99, 196, 48, 198, 223, 79, 204, 174, 31, 97, 236, 20, 213, 218, 85, 34, 16, 74, 196, 209, 235, 14, 71, 209, 32, 131, 195, 84, 11, 66, 74, 19, 115, 255, 99, 69, 235, 210, 204, 15, 13, 4, 143, 127, 152, 125, 212, 91 } },
                    { "FullTextSearchSettings", -1, new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new byte[] { 8, 120, 207, 5, 153, 181, 23, 202, 162, 211, 218, 237, 157, 6, 76, 62, 220, 238, 175, 67, 31, 53, 166, 246, 66, 220, 173, 160, 72, 23, 227, 81, 50, 39, 187, 177, 222, 110, 43, 171, 235, 158, 16, 119, 178, 207, 49, 140, 72, 152, 20, 84, 94, 135, 117, 1, 246, 51, 251, 190, 148, 2, 44, 252, 221, 2, 91, 83, 149, 151, 58, 245, 16, 148, 52, 8, 187, 86, 150, 46, 227, 93, 163, 95, 47, 131, 116, 207, 95, 209, 38, 149, 53, 148, 73, 215, 206, 251, 194, 199, 189, 17, 42, 229, 135, 82, 23, 154, 162, 165, 158, 94, 23, 128, 30, 88, 12, 204, 96, 250, 236, 142, 189, 211, 214, 18, 196, 136, 102, 102, 217, 109, 108, 240, 96, 96, 94, 100, 201, 10, 31, 170, 128, 192 } },
                    { "SmtpSettings", -1, new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new byte[] { 240, 82, 224, 144, 161, 163, 117, 13, 173, 205, 78, 153, 97, 218, 4, 170, 81, 239, 1, 151, 226, 192, 98, 60, 241, 44, 88, 56, 191, 164, 10, 155, 72, 186, 239, 203, 227, 113, 88, 119, 49, 215, 227, 220, 158, 124, 96, 9, 116, 47, 158, 65, 93, 86, 219, 15, 10, 224, 142, 50, 248, 144, 75, 44, 68, 28, 198, 87, 198, 69, 67, 234, 238, 38, 32, 68, 162, 139, 67, 53, 220, 176, 240, 196, 233, 64, 29, 137, 31, 160, 99, 105, 249, 132, 202, 45, 71, 92, 134, 194, 55, 145, 121, 97, 197, 130, 119, 105, 131, 21, 133, 35, 10, 102, 172, 119, 135, 230, 251, 86, 253, 62, 55, 56, 146, 103, 164, 106 } }
                });

            migrationBuilder.InsertData(
                table: "core_subscription",
                columns: new[] { "action", "object", "recipient", "source", "tenant" },
                values: new object[,]
                {
                    { "AddRelationshipEvent", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "CreateNewContact", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "ExportCompleted", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "ResponsibleForOpportunity", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "ResponsibleForTask", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "SetAccess", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                    { "new bookmark created", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "28b10049-dd20-4f54-b986-873bc14ccfc7", -1 },
                    { "BirthdayReminder", "", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "37620ae5-c40b-45ce-855a-39dd7d76a1fa", -1 },
                    { "calendar_sharing", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                    { "event_alert", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                    { "new feed", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6504977c-75af-4691-9099-084d3ddeea04", -1 },
                    { "new post", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6a598c74-91ae-437d-a5f4-ad339bd11bb2", -1 },
                    { "sharedocument", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                    { "sharefolder", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                    { "new wiki page", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "742cf945-cbbc-4a57-82d6-1600a12cf8ca", -1 },
                    { "new topic in forum", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "853b6eb9-73ee-438d-9b09-8ffeedf36234", -1 },
                    { "new photo uploaded", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "9d51954f-db9b-4aed-94e3-ed70b914e101", -1 },
                    { "admin_notify", "", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1 },
                    { "periodic_notify", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                    { "rooms_activity", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                    { "send_whats_new", "", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 }
                });

            migrationBuilder.InsertData(
                table: "core_subscriptionmethod",
                columns: new[] { "action", "recipient", "source", "tenant", "sender" },
                values: new object[,]
                {
                    { "AddRelationshipEvent", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "CreateNewContact", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "ExportCompleted", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "ResponsibleForOpportunity", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "ResponsibleForTask", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "SetAccess", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1, "email.sender|messanger.sender" },
                    { "new bookmark created", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "28b10049-dd20-4f54-b986-873bc14ccfc7", -1, "email.sender|messanger.sender" },
                    { "BirthdayReminder", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "37620ae5-c40b-45ce-855a-39dd7d76a1fa", -1, "email.sender|messanger.sender" },
                    { "calendar_sharing", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1, "email.sender|messanger.sender" },
                    { "event_alert", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1, "email.sender|messanger.sender" },
                    { "invitetoproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "milestonedeadline", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "newcommentformessage", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "newcommentformilestone", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "newcommentfortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "projectcreaterequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "projecteditrequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "removefromproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "responsibleforproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "responsiblefortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "taskclosed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1, "email.sender|messanger.sender" },
                    { "new feed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6504977c-75af-4691-9099-084d3ddeea04", -1, "email.sender|messanger.sender" },
                    { "new post", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6a598c74-91ae-437d-a5f4-ad339bd11bb2", -1, "email.sender|messanger.sender" },
                    { "sharedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1, "email.sender|messanger.sender" },
                    { "sharefolder", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1, "email.sender|messanger.sender" },
                    { "updatedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1, "email.sender|messanger.sender" },
                    { "new wiki page", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "742cf945-cbbc-4a57-82d6-1600a12cf8ca", -1, "email.sender|messanger.sender" },
                    { "new topic in forum", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "853b6eb9-73ee-438d-9b09-8ffeedf36234", -1, "email.sender|messanger.sender" },
                    { "new photo uploaded", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "9d51954f-db9b-4aed-94e3-ed70b914e101", -1, "email.sender|messanger.sender" },
                    { "admin_notify", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1, "email.sender" },
                    { "periodic_notify", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1, "email.sender" },
                    { "send_whats_new", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1, "email.sender" }
                });

            migrationBuilder.InsertData(
                table: "core_usergroup",
                columns: new[] { "ref_type", "tenant", "groupid", "userid", "last_modified" },
                values: new object[] { 0, 1, new Guid("cd84e66b-b803-40fc-99f9-b2969a54a1de"), new Guid("66faa6e4-f133-11ea-b126-00ffeec8b4ef"), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "core_usersecurity",
                columns: new[] { "userid", "lastmodified", "pwdhash", "tenant" },
                values: new object[] { new Guid("66faa6e4-f133-11ea-b126-00ffeec8b4ef"), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", 1 });

            migrationBuilder.InsertData(
                table: "webstudio_settings",
                columns: new[] { "id", "tenant_id", "user_id", "data" },
                values: new object[] { new Guid("9a925891-1f92-4ed7-b277-d6f649739f06"), 1, new Guid("00000000-0000-0000-0000-000000000000"), "{\"Completed\":false}" });

            migrationBuilder.CreateIndex(
                name: "ix_account_links_uid",
                table: "account_links",
                column: "uid");

            migrationBuilder.CreateIndex(
                name: "date",
                table: "audit_events",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "expires_on",
                table: "backup_backup",
                column: "expires_on");

            migrationBuilder.CreateIndex(
                name: "is_scheduled",
                table: "backup_backup",
                column: "is_scheduled");

            migrationBuilder.CreateIndex(
                name: "tenant_id",
                table: "backup_backup",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_last_modified",
                table: "core_group",
                column: "last_modified");

            migrationBuilder.CreateIndex(
                name: "ix_parentid",
                table: "core_group",
                columns: new[] { "tenant", "parentid" });

            migrationBuilder.CreateIndex(
                name: "core_usergroup_last_modified_idx",
                table: "core_usergroup",
                column: "last_modified");

            migrationBuilder.CreateIndex(
                name: "idx_userphoto_tenant",
                table: "core_userphoto",
                column: "tenant");

            migrationBuilder.CreateIndex(
                name: "idx_pwdhash",
                table: "core_usersecurity",
                column: "pwdhash");

            migrationBuilder.CreateIndex(
                name: "idx_tenant",
                table: "core_usersecurity",
                column: "tenant");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_id",
                table: "event_bus_integration_event_log",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_audit_reference_audit_event_id",
                table: "files_audit_reference",
                column: "audit_event_id");

            migrationBuilder.CreateIndex(
                name: "idx_files_bunch_objects_left_node",
                table: "files_bunch_objects",
                column: "left_node");

            migrationBuilder.CreateIndex(
                name: "folder_id",
                table: "files_file",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "id",
                table: "files_file",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "modified_on",
                table: "files_file",
                column: "modified_on");

            migrationBuilder.CreateIndex(
                name: "tenant_id_folder_id_content_length",
                table: "files_file",
                columns: new[] { "tenant_id", "folder_id", "content_length" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_folder_id_modified_on",
                table: "files_file",
                columns: new[] { "tenant_id", "folder_id", "modified_on" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_folder_id_title",
                table: "files_file",
                columns: new[] { "tenant_id", "folder_id", "title" });

            migrationBuilder.CreateIndex(
                name: "modified_on",
                table: "files_folder",
                column: "modified_on");

            migrationBuilder.CreateIndex(
                name: "parent_id",
                table: "files_folder",
                columns: new[] { "tenant_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_parent_id_modified_on",
                table: "files_folder",
                columns: new[] { "tenant_id", "parent_id", "modified_on" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_parent_id_title",
                table: "files_folder",
                columns: new[] { "tenant_id", "parent_id", "title" });

            migrationBuilder.CreateIndex(
                name: "ix_folder_id",
                table: "files_folder_tree",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "linked_for",
                table: "files_link",
                columns: new[] { "tenant_id", "source_id", "linked_id", "linked_for" });

            migrationBuilder.CreateIndex(
                name: "parent_folder_id",
                table: "files_order",
                columns: new[] { "tenant_id", "parent_folder_id", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "IX_files_room_settings_room_id",
                table: "files_room_settings",
                column: "room_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_owner",
                table: "files_security",
                column: "owner");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_id",
                table: "files_security",
                columns: new[] { "tenant_id", "entry_type", "entry_id", "owner" });

            migrationBuilder.CreateIndex(
                name: "name",
                table: "files_tag",
                columns: new[] { "tenant_id", "owner", "name", "flag" });

            migrationBuilder.CreateIndex(
                name: "idx_create_on",
                table: "files_tag_link",
                column: "create_on");

            migrationBuilder.CreateIndex(
                name: "idx_entry_id",
                table: "files_tag_link",
                columns: new[] { "tenant_id", "entry_id", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "tenant_id",
                table: "files_thirdparty_account",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_thirdparty_app_tenant_id",
                table: "files_thirdparty_app",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_thirdparty_id_mapping_tenantid_hashid",
                table: "files_thirdparty_id_mapping",
                columns: new[] { "tenant_id", "hash_id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebase_users_user_id",
                table: "firebase_users",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_type_name",
                table: "hosting_instance_registration",
                column: "worker_type_name");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_allowed_origins_client_id",
                table: "identity_client_allowed_origins",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_client_authentication_methods_client_id",
                table: "identity_client_authentication_methods",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_redirect_uris_client_id",
                table: "identity_client_redirect_uris",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_scopes_client_id",
                table: "identity_client_scopes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_scopes_scope_name",
                table: "identity_client_scopes",
                column: "scope_name");

            migrationBuilder.CreateIndex(
                name: "ix_identity_client_client_id",
                table: "identity_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_client_tenant_id",
                table: "identity_clients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_consent_scopes_principal_id",
                table: "identity_consent_scopes",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_consent_scopes_registered_client_id",
                table: "identity_consent_scopes",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_consent_scopes_scope_name",
                table: "identity_consent_scopes",
                column: "scope_name");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_is_invalidated",
                table: "identity_consents",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuthorizations_IdentityClientClientId",
                table: "IdentityAuthorizations",
                column: "IdentityClientClientId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuthorizations_RegisteredClientId",
                table: "IdentityAuthorizations",
                column: "RegisteredClientId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuthorizations_TenantId",
                table: "IdentityAuthorizations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_date",
                table: "login_events",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_id_user_id",
                table: "login_events",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notify_info_state",
                table: "notify_info",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "idx_creation_date",
                table: "notify_queue",
                column: "creation_date");

            migrationBuilder.CreateIndex(
                name: "IX_notify_queue_tenant_id",
                table: "notify_queue",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_short_links_short",
                table: "short_links",
                column: "short",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "tenant_id",
                table: "short_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_id",
                table: "telegram_users",
                column: "telegram_user_id");

            migrationBuilder.CreateIndex(
                name: "tenant",
                table: "tenants_iprestrictions",
                column: "tenant");

            migrationBuilder.CreateIndex(
                name: "last_modified",
                table: "tenants_quotarow",
                column: "last_modified");

            migrationBuilder.CreateIndex(
                name: "tenant",
                table: "tenants_tariff",
                column: "tenant");

            migrationBuilder.CreateIndex(
                name: "alias",
                table: "tenants_tenants",
                column: "alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "last_modified",
                table: "tenants_tenants",
                column: "last_modified");

            migrationBuilder.CreateIndex(
                name: "mappeddomain",
                table: "tenants_tenants",
                column: "mappeddomain");

            migrationBuilder.CreateIndex(
                name: "version",
                table: "tenants_tenants",
                column: "version");

            migrationBuilder.CreateIndex(
                name: "IX_Users_tenant_id",
                table: "Users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_webhooks_config_tenant_id",
                table: "webhooks_config",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_webhooks_logs_config_id",
                table: "webhooks_logs",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhooks_logs_tenant_id",
                table: "webhooks_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_webstudio_settings_Id",
                table: "webstudio_settings",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_links");

            migrationBuilder.DropTable(
                name: "backup_backup");

            migrationBuilder.DropTable(
                name: "backup_schedule");

            migrationBuilder.DropTable(
                name: "core_acl");

            migrationBuilder.DropTable(
                name: "core_group");

            migrationBuilder.DropTable(
                name: "core_settings");

            migrationBuilder.DropTable(
                name: "core_subscription");

            migrationBuilder.DropTable(
                name: "core_subscriptionmethod");

            migrationBuilder.DropTable(
                name: "core_user_relations");

            migrationBuilder.DropTable(
                name: "core_userdav");

            migrationBuilder.DropTable(
                name: "core_usergroup");

            migrationBuilder.DropTable(
                name: "core_userphoto");

            migrationBuilder.DropTable(
                name: "core_usersecurity");

            migrationBuilder.DropTable(
                name: "dbip_lookup");

            migrationBuilder.DropTable(
                name: "event_bus_integration_event_log");

            migrationBuilder.DropTable(
                name: "files_audit_reference");

            migrationBuilder.DropTable(
                name: "files_bunch_objects");

            migrationBuilder.DropTable(
                name: "files_converts");

            migrationBuilder.DropTable(
                name: "files_file");

            migrationBuilder.DropTable(
                name: "files_folder_tree");

            migrationBuilder.DropTable(
                name: "files_link");

            migrationBuilder.DropTable(
                name: "files_order");

            migrationBuilder.DropTable(
                name: "files_properties");

            migrationBuilder.DropTable(
                name: "files_room_settings");

            migrationBuilder.DropTable(
                name: "files_security");

            migrationBuilder.DropTable(
                name: "files_tag");

            migrationBuilder.DropTable(
                name: "files_tag_link");

            migrationBuilder.DropTable(
                name: "files_thirdparty_account");

            migrationBuilder.DropTable(
                name: "files_thirdparty_app");

            migrationBuilder.DropTable(
                name: "files_thirdparty_id_mapping");

            migrationBuilder.DropTable(
                name: "firebase_users");

            migrationBuilder.DropTable(
                name: "hosting_instance_registration");

            migrationBuilder.DropTable(
                name: "identity_certs");

            migrationBuilder.DropTable(
                name: "identity_client_allowed_origins");

            migrationBuilder.DropTable(
                name: "identity_client_authentication_methods");

            migrationBuilder.DropTable(
                name: "identity_client_redirect_uris");

            migrationBuilder.DropTable(
                name: "identity_client_scopes");

            migrationBuilder.DropTable(
                name: "identity_consent_scopes");

            migrationBuilder.DropTable(
                name: "identity_shedlock");

            migrationBuilder.DropTable(
                name: "IdentityAuthorizations");

            migrationBuilder.DropTable(
                name: "login_events");

            migrationBuilder.DropTable(
                name: "mobile_app_install");

            migrationBuilder.DropTable(
                name: "notify_info");

            migrationBuilder.DropTable(
                name: "notify_queue");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "short_links");

            migrationBuilder.DropTable(
                name: "telegram_users");

            migrationBuilder.DropTable(
                name: "tenants_forbiden");

            migrationBuilder.DropTable(
                name: "tenants_iprestrictions");

            migrationBuilder.DropTable(
                name: "tenants_partners");

            migrationBuilder.DropTable(
                name: "tenants_quota");

            migrationBuilder.DropTable(
                name: "tenants_quotarow");

            migrationBuilder.DropTable(
                name: "tenants_tariff");

            migrationBuilder.DropTable(
                name: "tenants_tariffrow");

            migrationBuilder.DropTable(
                name: "tenants_version");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "webhooks");

            migrationBuilder.DropTable(
                name: "webhooks_logs");

            migrationBuilder.DropTable(
                name: "webstudio_index");

            migrationBuilder.DropTable(
                name: "webstudio_settings");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "files_folder");

            migrationBuilder.DropTable(
                name: "identity_consents");

            migrationBuilder.DropTable(
                name: "identity_scopes");

            migrationBuilder.DropTable(
                name: "webhooks_config");

            migrationBuilder.DropTable(
                name: "identity_clients");

            migrationBuilder.DropTable(
                name: "tenants_tenants");
        }
    }
}
