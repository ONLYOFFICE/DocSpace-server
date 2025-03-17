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

namespace ASC.Core.Common.EF.Model;

public class ApiKey : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string KeyPrefix { get; set; }
    public string HashedKey { get; set; }
    public List<string> Permissions { get; set; }
    public DateTime? LastUsed { get; set; }
    public DateTime CreateOn { get; set; } = DateTime.UtcNow;
    public Guid CreateBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int TenantId { get; set; }
    
    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbApiKeyExtension
{
    public static ModelBuilderWrapper AddDbApiKeys(this ModelBuilderWrapper modelBuilder)
    {
        return modelBuilder
            .Add(MySqlAddDbApiKeys, Provider.MySql)
            .Add(PgSqlAddDbApiKeys, Provider.PostgreSql);
    }

    private static void MySqlAddDbApiKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => new { e.Id })
                .HasName("PRIMARY");
                
            entity.ToTable("core_user_api_key")
                .HasCharSet("utf8");
            
            entity.HasIndex(a => new { a.TenantId, a.HashedKey })
                .HasDatabaseName("hashed_key");
            
            entity.HasIndex(a => a.IsActive)
                .HasDatabaseName("is_active");
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci")
                .IsRequired();
            
            entity.Property(e => e.KeyPrefix)
                .HasColumnName("key_prefix")
                .HasColumnType("varchar")
                .HasMaxLength(8)
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci")
                .IsRequired();

            entity.Property(e => e.HashedKey)
                .HasColumnName("hashed_key")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci")
                .IsRequired();
            
            entity.Property(e => e.Permissions)
                  .HasColumnName("permissions")
                  .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.LastUsed)
                .HasColumnName("last_used")
                .HasColumnType("datetime");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(e => e.CreateBy)
                .HasColumnName("create_by")
                .HasColumnType("varchar(38)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci")
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("datetime");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("int")
                .IsRequired();
          
        });
    }
    private static void PgSqlAddDbApiKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => new { e.Id })
                .HasName("pk_core_user_api_key");
                
            entity.ToTable("core_user_api_key");
            
            entity.HasIndex(a => new { a.TenantId, a.HashedKey })
                .HasDatabaseName("idx_core_user_api_key_tenant_id_hashed_key");
            
            entity.HasIndex(a => a.IsActive)
                .HasDatabaseName("idx_core_user_api_key_is_active");
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.KeyPrefix)
                .HasColumnName("key_prefix")
                .HasColumnType("varchar")
                .HasMaxLength(8)
                .IsRequired();

            entity.Property(e => e.HashedKey)
                .HasColumnName("hashed_key")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.Permissions)
                  .HasColumnName("permissions")
                  .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.LastUsed)
                .HasColumnName("last_used")
                .HasColumnType("timestamp");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("timestamp")
                .IsRequired();

            entity.Property(e => e.CreateBy)
                .HasColumnName("create_by")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer")
                .IsRequired();
          
        });
    }
}