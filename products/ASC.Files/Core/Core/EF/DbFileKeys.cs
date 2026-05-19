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

public class DbFileKeys : BaseEntity
{
    public Guid UserId { get; set; }
    
    public Guid PublicKeyId { get; set; }
    
    public string PrivateKeyEnc { get; set; }
    
    public int TenantId { get; set; }

    public int FileId { get; set; }
    
    public DateTime CreateOn { get; set; }
    
    public DbTenant Tenant { get; set; }
    
    public override object[] GetKeys()
    {
        return [ TenantId, FileId, UserId ];
    }
}

public static class DbFileKeysExtension
{
    public static ModelBuilderWrapper AddDbFileKeys(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFileKeys>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFileKeys, Provider.MySql)
            .Add(PgSqlAddDbFileKeys, Provider.PostgreSql);

        return modelBuilder;
    }

    private static void MySqlAddDbFileKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFileKeys>(entity =>
        {
            entity.ToTable("files_file_keys")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.FileId, e.UserId })
                .HasName("PRIMARY");

            entity.HasIndex(e => new { e.TenantId, e.UserId })
                .HasDatabaseName("tenant_id_user_id");

            entity.HasIndex(e => new { e.TenantId, e.FileId })
                .HasDatabaseName("tenant_id_file_id");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.FileId)
                .HasColumnName("file_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PublicKeyId)
                .HasColumnName("public_key_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PrivateKeyEnc)
                .IsRequired()
                .HasColumnName("private_key_enc")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("datetime");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void PgSqlAddDbFileKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFileKeys>(entity =>
        {
            entity.ToTable("files_file_keys");

            entity.HasKey(e => new { e.TenantId, e.FileId, e.UserId })
                .HasName("files_file_keys_pkey");

            entity.HasIndex(e => new { e.TenantId, e.UserId })
                .HasDatabaseName("tenant_id_user_id");

            entity.HasIndex(e => new { e.TenantId, e.FileId })
                .HasDatabaseName("tenant_id_file_id");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.FileId)
                .HasColumnName("file_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(36);

            entity.Property(e => e.PublicKeyId)
                .HasColumnName("public_key_id")
                .HasMaxLength(36);

            entity.Property(e => e.PrivateKeyEnc)
                .IsRequired()
                .HasColumnName("private_key_enc");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("timestamptz");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}