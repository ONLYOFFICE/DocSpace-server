// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Core.Database.Models;

public class DbMcpServerOptions : BaseEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; }
    
    [MaxLength(128)] 
    public required string Name { get; set; }
    
    [MaxLength(255)]
    public string? Description { get; set; }
    public required string Endpoint { get; set; }
    public string? Headers { get; set; }
    
    public bool Enabled { get; set; }

    public DbTenant Tenant { get; set; } = null!;

    public override object[] GetKeys()
    {
        return [Id];
    }

    public async Task<McpServerOptions> ToMcpServerOptions(InstanceCrypto crypto)
    {
        var options = new McpServerOptions
        {
            Id = Id, 
            TenantId = TenantId, 
            Name = Name,
            Description = Description,
            Endpoint = new Uri(Endpoint),
            Enabled = Enabled,
        };

        if (Headers == null)
        {
            return options;
        }

        var headersJson = await crypto.DecryptAsync(Headers);
        options.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);

        return options;
    }
}

public static class DbMcpServerOptionsExtensions
{
    public static ModelBuilderWrapper AddMcpServers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbMcpServerOptions>().Navigation(e => e.Tenant).AutoInclude(false);
        return modelBuilder.Add(MySqlAddMcpServerOptions, ASC.Core.Common.EF.Provider.MySql);
    }

    private static void MySqlAddMcpServerOptions(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMcpServerOptions>(entity =>
        {
            entity.ToTable("ai_mcp_servers")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.HasIndex(e => new { e.TenantId, e.Id })
                .HasDatabaseName("IX_tenant_id_id");

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
            
            entity.Property(e => e.Enabled)
                .HasColumnName("enabled");
        });
    }
}