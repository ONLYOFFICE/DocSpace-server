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

namespace ASC.AI.Integration.Database.Models;

public class DbMcpServer : BaseEntity
{
    public int TenantId { get; init; }

    [MaxLength(128)]
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Config { get; set; }

    public DateTime CreatedAt { get; init; }

    public DbTenant Tenant { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [TenantId, Name];
    }
}

public static class DbMcpServerExtension
{
    public static ModelBuilderWrapper AddDbMcpServers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbMcpServer>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbMcpServers, Provider.MySql)
            .Add(PgSqlAddDbMcpServers, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbMcpServers(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMcpServer>(entity =>
        {
            entity.ToTable("ai_integration_mcp_servers")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.Name })
                .HasName("PRIMARY");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(128)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Config)
                .HasColumnName("config")
                .HasColumnType("json");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");
        });
    }

    public static void PgSqlAddDbMcpServers(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMcpServer>(entity =>
        {
            entity.ToTable("ai_integration_mcp_servers");

            entity.HasKey(e => new { e.TenantId, e.Name })
                .HasName("pk_ai_integration_mcp_servers");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("character varying")
                .HasMaxLength(128);

            entity.Property(e => e.Config)
                .HasColumnName("config")
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone");
        });
    }
}
