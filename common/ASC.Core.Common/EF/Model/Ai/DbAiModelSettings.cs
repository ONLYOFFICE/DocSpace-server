// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Core.Common.EF.Model.Ai;

public class DbAiModelSettings : BaseEntity
{
    public int TenantId { get; set; }
    public int ProviderId { get; set; }

    [MaxLength(255)]
    [Required]
    public required string ModelId { get; set; }

    [MaxLength(255)]
    public string Alias { get; set; }

    public bool IsEnabled { get; set; }
    public AiModelCapabilities Capabilities { get; set; }

    public DbTenant Tenant { get; set; }
    public DbAiProvider Provider { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, ProviderId, ModelId];
    }
}

/// <summary>
/// The AI model capabilities.
/// </summary>
public class AiModelCapabilities
{
    [JsonIgnore]
    public static readonly AiModelCapabilities Default = new()
    {
        Vision = true,
        ToolCalling = true,
        Thinking = true
    };

    /// <summary>
    /// Indicates whether the model supports image and vision input.
    /// </summary>
    /// <example>true</example>
    public bool Vision { get; init; }

    /// <summary>
    /// Indicates whether the model supports tool (function) calling.
    /// </summary>
    /// <example>true</example>
    public bool ToolCalling { get; init; }

    /// <summary>
    /// Indicates whether the model supports extended thinking and reasoning.
    /// </summary>
    /// <example>false</example>
    public bool Thinking { get; init; }
}

public static class DbAiModelSettingsExtension
{
    public static ModelBuilderWrapper AddDbAiModelSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAiModelSettings>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbAiModelSettings>().Navigation(e => e.Provider).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbAiModelSettings, Provider.MySql)
            .Add(PgSqlAddDbAiModelSettings, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddDbAiModelSettings()
        {
            modelBuilder.Entity<DbAiModelSettings>(entity =>
            {
                entity.ToTable("ai_model_settings")
                    .HasCharSet("utf8");

                entity.HasKey(e => new { e.TenantId, e.ProviderId, e.ModelId })
                    .HasName("PRIMARY");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.ProviderId)
                    .HasColumnName("provider_id");

                entity.Property(e => e.ModelId)
                    .IsRequired()
                    .HasColumnName("model_id")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Alias)
                    .HasColumnName("alias")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.IsEnabled)
                    .HasColumnName("is_enabled")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Capabilities)
                    .HasColumnName("capabilities")
                    .HasColumnType("json")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.HasIndex(e => new { e.TenantId, e.ProviderId })
                    .HasDatabaseName("IX_tenant_id_provider_id");

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Provider)
                    .WithMany()
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void PgSqlAddDbAiModelSettings()
        {
            modelBuilder.Entity<DbAiModelSettings>(entity =>
            {
                entity.ToTable("ai_model_settings");

                entity.HasKey(e => new { e.TenantId, e.ProviderId, e.ModelId })
                    .HasName("pk_ai_model_settings");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id")
                    .HasColumnType("integer");

                entity.Property(e => e.ProviderId)
                    .HasColumnName("provider_id")
                    .HasColumnType("integer");

                entity.Property(e => e.ModelId)
                    .IsRequired()
                    .HasColumnName("model_id")
                    .HasColumnType("character varying")
                    .HasMaxLength(255);

                entity.Property(e => e.Alias)
                    .HasColumnName("alias")
                    .HasColumnType("character varying")
                    .HasMaxLength(255);

                entity.Property(e => e.IsEnabled)
                    .HasColumnName("is_enabled")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Capabilities)
                    .HasColumnName("capabilities")
                    .HasColumnType("jsonb");

                entity.HasIndex(e => new { e.TenantId, e.ProviderId })
                    .HasDatabaseName("IX_ai_model_settings_tenant_provider");

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Provider)
                    .WithMany()
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
