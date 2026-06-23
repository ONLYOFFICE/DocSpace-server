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

namespace ASC.AI.Core.Database.Models;

public class DbMcpServer : BaseEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; }
    
    [MaxLength(128)] 
    public required string Name { get; set; }
    
    [MaxLength(255)]
    public string? Description { get; set; }
    public required string Endpoint { get; set; }
    public string? Headers { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public bool HasIcon { get; set; }
    public DateTime ModifiedOn { get; set; }

    public DbTenant Tenant { get; set; } = null!;

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbMcpServerOptionsExtensions
{
    public static ModelBuilderWrapper AddDbMcpServers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbMcpServer>().Navigation(e => e.Tenant).AutoInclude(false);
        return modelBuilder.Add(MySqlAddMcpServerOptions, ASC.Core.Common.EF.Provider.MySql);
    }

    private static void MySqlAddMcpServerOptions(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMcpServer>(entity =>
        {
            entity.ToTable("ai_mcp_servers")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.HasIndex(e => new { e.TenantId, e.Id })
                .HasDatabaseName("IX_tenant_id_id");
            
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("IX_tenant_id_name")
                .IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(128)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.Endpoint)
                .HasColumnName("endpoint")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.Headers)
                .HasColumnName("headers")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ConnectionType)
                .HasColumnName("connection_type");
            
            entity.Property(e => e.HasIcon)
                .HasColumnName("has_icon");
            
            entity.Property(e => e.ModifiedOn)
                .HasColumnName("modified_on")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}