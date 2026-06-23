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

namespace ASC.Files.Core.EF;

public class DbFilesThirdpartyAccount : BaseEntity, IDbFile, IDbSearch
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Provider { get; set; }
    
    [MaxLength(400)]
    public string Title { get; set; }
    
    [MaxLength(100)]
    public string UserName { get; set; }
    
    [MaxLength(512)]
    public string Password { get; set; }
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public FolderType FolderType { get; set; }
    public FolderType RoomType { get; set; }
    public DateTime CreateOn { get; set; }
    public string Url { get; set; }
    public int TenantId { get; set; }
    public string FolderId { get; set; }
    public bool Private { get; set; }
    public bool HasLogo { get; set; }
    
    [MaxLength(6)]
    public string Color { get; set; }
    
    [MaxLength(50)]
    public string Cover { get; set; }
    
    
    public DateTime ModifiedOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbFilesThirdpartyAccountExtension
{
    public static ModelBuilderWrapper AddDbFilesThirdpartyAccount(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesThirdpartyAccount>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesThirdpartyAccount, Provider.MySql)
            .Add(PgSqlAddDbFilesThirdpartyAccount, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesThirdpartyAccount()
        {
            modelBuilder.Entity<DbFilesThirdpartyAccount>(entity =>
            {
                entity.ToTable("files_thirdparty_account")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.TenantId).HasDatabaseName("tenant_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("datetime");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("customer_title")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.FolderType)
                    .HasColumnName("folder_type")
                    .HasDefaultValueSql("'0'");
                entity.Property(e => e.RoomType).HasColumnName("room_type");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasColumnName("password")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Provider)
                    .IsRequired()
                    .HasColumnName("provider")
                    .HasColumnType("varchar")
                    .HasDefaultValueSql("'0'")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Token)
                    .HasColumnName("token")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Url)
                    .HasColumnName("url")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasColumnName("user_name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.FolderId)
                    .HasColumnName("folder_id")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Private).HasColumnName("private");

                entity.Property(e => e.HasLogo).HasColumnName("has_logo");

                entity.Property(e => e.Color)
                    .HasColumnName("color")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            
                entity.Property(e => e.Cover)
                    .HasColumnName("cover")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        public void PgSqlAddDbFilesThirdpartyAccount()
        {
            modelBuilder.Entity<DbFilesThirdpartyAccount>(entity =>
            {
                entity.ToTable("files_thirdparty_account");

                entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_files_thirdparty_account_tenant_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("customer_title");

                entity.Property(e => e.FolderType).HasColumnName("folder_type");

                entity.Property(e => e.RoomType).HasColumnName("room_type");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasColumnName("password");

                entity.Property(e => e.Provider)
                    .IsRequired()
                    .HasColumnName("provider");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Token).HasColumnName("token");

                entity.Property(e => e.Url).HasColumnName("url");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasColumnName("user_name");

                entity.Property(e => e.FolderId).HasColumnName("folder_id");

                entity.Property(e => e.Private).HasColumnName("private");

                entity.Property(e => e.HasLogo).HasColumnName("has_logo");

                entity.Property(e => e.Color)
                    .HasColumnName("color")
                    .HasColumnType("char(6)");
            
                entity.Property(e => e.Cover)
                    .HasColumnName("cover")
                    .HasColumnType("char(6)");
            
                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

        }
    }
}