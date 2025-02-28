// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Webhooks.Core.EF.Model;

public class DbWebhooksConfig : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public string Uri { get; set; }
    [MaxLength(50)]
    public string SecretKey { get; set; }
    public bool Enabled { get; set; }
    public bool SSL { get; set; }
    public WebhookTrigger Trigger { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public DateTime? LastFailureOn { get; set; }
    public string LastFailureContent { get; set; }
    public DateTime? LastSuccessOn { get; set; }

    public DbTenant Tenant { get; set; }
    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class WebhooksConfigExtension
{
    public static ModelBuilderWrapper AddWebhooksConfig(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbWebhooksConfig>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddWebhooksConfig, Provider.MySql)
            .Add(PgSqlAddWebhooksConfig, Provider.PostgreSql);

        return modelBuilder;
    }
    public static void MySqlAddWebhooksConfig(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWebhooksConfig>(entity =>
        {
            entity.HasKey(e => new { e.Id })
                .HasName("PRIMARY");

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("tenant_id");

            entity.ToTable("webhooks_config")
                .HasCharSet("utf8");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("id");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired();

            entity.Property(e => e.SecretKey)
                .HasColumnName("secret_key")
                .HasDefaultValueSql("''");

            entity.Property(e => e.Uri)
                .HasColumnName("uri")
                .HasDefaultValueSql("''")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Enabled)
                .HasColumnName("enabled")
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(1)");

            entity.Property(e => e.SSL)
                .HasColumnName("ssl")
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(1)");

            entity.Property(e => e.Trigger)
                .HasColumnName("trigger")
                .IsRequired()
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.CreatedBy)
                .IsRequired(false)
                .HasColumnName("created_by")
                .HasColumnType("varchar(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreatedOn)
                .IsRequired(false)
                .HasColumnName("created_on")
                .HasColumnType("datetime");

            entity.Property(e => e.ModifiedBy)
                .IsRequired(false)
                .HasColumnName("modified_by")
                .HasColumnType("varchar(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ModifiedOn)
                .IsRequired(false)
                .HasColumnName("modified_on")
                .HasColumnType("datetime");

            entity.Property(e => e.LastFailureOn)
                .IsRequired(false)
                .HasColumnName("last_failure_on")
                .HasColumnType("datetime");

            entity.Property(e => e.LastFailureContent)
                .IsRequired(false)
                .HasColumnName("last_failure_content")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.LastSuccessOn)
                .IsRequired(false)
                .HasColumnName("last_success_on")
                .HasColumnType("datetime");
        });
    }

    public static void PgSqlAddWebhooksConfig(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWebhooksConfig>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("webhooks_config_pkey");

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("idx_webhooks_config_tenant_id");

            entity.ToTable("webhooks_config");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("integer");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying");

            entity.Property(e => e.SecretKey)
                .HasColumnName("secret_key")
                .HasDefaultValueSql("''")
                .HasColumnType("text");

            entity.Property(e => e.Uri)
                .HasColumnName("uri")
                .IsRequired(false)
                .HasColumnType("text");

            entity.Property(e => e.Enabled)
                .HasColumnName("enabled")
                .HasDefaultValueSql("true")
                .HasColumnType("boolean");

            entity.Property(e => e.SSL)
                .HasColumnName("ssl")
                .HasDefaultValueSql("true")
                .HasColumnType("boolean");

            entity.Property(e => e.Trigger)
                .HasColumnName("trigger")
                .IsRequired()
                .HasDefaultValueSql("0");

            entity.Property(e => e.CreatedBy)
                .IsRequired(false)
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            entity.Property(e => e.CreatedOn)
                .IsRequired(false)
                .HasColumnName("created_on");

            entity.Property(e => e.ModifiedBy)
                .IsRequired(false)
                .HasColumnName("modified_by")
                .HasColumnType("uuid");

            entity.Property(e => e.ModifiedOn)
                .IsRequired(false)
                .HasColumnName("modified_on");

            entity.Property(e => e.LastFailureOn)
                .IsRequired(false)
                .HasColumnName("last_failure_on");

            entity.Property(e => e.LastFailureContent)
                .IsRequired(false)
                .HasColumnName("last_failure_content");

            entity.Property(e => e.LastSuccessOn)
                .IsRequired(false)
                .HasColumnName("last_success_on");
        });
    }
}

public class WebhooksConfigWithStatus
{
    public DbWebhooksConfig WebhooksConfig { get; init; }
    public int? Status { get; set; }
}
