// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Core.Common.EF.Model.Ai;

[EnumExtensions]
public enum ProviderType
{
    [Description("PortalAi")]
    PortalAi = 0,

    [Description("OpenAi")]
    OpenAi = 1,

    [Description("TogetherAi")]
    TogetherAi = 2,

    [Description("OpenAiCompatible")]
    OpenAiCompatible = 3,

    [Description("Anthropic")]
    Anthropic = 4,

    [Description("OpenRouter")]
    OpenRouter = 5,

    [Description("DeepSeek")]
    DeepSeek = 6,

    [Description("XAi")]
    XAi = 7,

    [Description("GoogleAi")]
    GoogleAi = 8
}

public class DbAiProvider : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public ProviderType Type { get; set; }

    [MaxLength(255)]
    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Url { get; set; }

    [Required]
    public required string Key { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public bool HasModelSettings { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class ModelsProviderExtension
{
    public static ModelBuilderWrapper AddDbAiProviders(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAiProvider>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Add(AddMySqlModelsProviders, Provider.MySql);

        return modelBuilder;
    }

    public static void AddMySqlModelsProviders(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbAiProvider>(entity =>
        {
            entity.ToTable("ai_providers")
                .HasCharSet("utf8");

            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.Type)
                .HasColumnName("type");

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Url)
                .HasColumnName("url")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Key)
                .HasColumnName("key")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreatedOn)
                .HasColumnName("created_on")
                .HasColumnType("datetime");

            entity.Property(e => e.ModifiedOn)
                .HasColumnName("modified_on")
                .HasColumnType("datetime");

            entity.Property(e => e.HasModelSettings)
                .HasColumnName("has_model_settings")
                .HasDefaultValueSql("'0'");

            entity.HasIndex(e => new { e.TenantId, e.Id })
                .HasDatabaseName("IX_tenant_id_id");
        });
    }
}
