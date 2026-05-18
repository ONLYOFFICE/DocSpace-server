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

public class DbFilesSecurity : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    
    [MaxLength(50)]
    public string EntryId { get; set; }
    
    public int InternalEntryId { get; set; }
    
    public FileEntryType EntryType { get; set; }
    public SubjectType SubjectType { get; set; }
    public Guid Subject { get; set; }
    public Guid Owner { get; set; }
    public FileShare Share { get; set; }
    public DateTime TimeStamp { get; set; }
    public FileShareOptions Options { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, EntryId, EntryType, Subject];
    }
}

public static class DbFilesSecurityExtension
{
    public static ModelBuilderWrapper AddDbFilesSecurity(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesSecurity>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesSecurity, Provider.MySql)
            .Add(PgSqlAddDbFilesSecurity, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesSecurity()
        {
            modelBuilder.Entity<DbFilesSecurity>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType, e.Subject })
                    .HasName("PRIMARY");

                entity.ToTable("files_security")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.Owner)
                    .HasDatabaseName("owner");

                entity.HasIndex(e => new { e.TenantId, e.EntryType, e.EntryId, e.Owner })
                    .HasDatabaseName("tenant_id");

                entity.HasIndex(e => new { e.TenantId, e.Subject })
                    .HasDatabaseName("tenant_id_subject");
            
                entity.HasIndex(e => new { e.TenantId, e.InternalEntryId })
                    .HasDatabaseName("tenant_id_internal_entry_id");
            
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId)
                    .HasColumnName("entry_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            
                entity.Property(e => e.InternalEntryId)
                    .HasColumnName("internal_entry_id");

                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.Subject)
                    .HasColumnName("subject")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Owner)
                    .IsRequired()
                    .HasColumnName("owner")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Share).HasColumnName("security");

                entity.Property(e => e.TimeStamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("timestamp");

                entity.Property(e => e.SubjectType).HasColumnName("subject_type");

                entity.Property(e => e.Options)
                    .HasColumnName("options")
                    .HasColumnType("json")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddDbFilesSecurity()
        {
            modelBuilder.Entity<DbFilesSecurity>(entity =>
            {
                // Defining a composite primary key for PostgreSQL database
                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType, e.Subject })
                    .HasName("files_security_pkey");

                // Setting the table name
                entity.ToTable("files_security");

                // Adding indexes for efficiency
                entity.HasIndex(e => e.Owner)
                    .HasDatabaseName("idx_owner");

                entity.HasIndex(e => new { e.TenantId, e.EntryType, e.EntryId, e.Owner })
                    .HasDatabaseName("idx_tenant_id");

                entity.HasIndex(e => new { e.TenantId, e.Subject })
                    .HasDatabaseName("idx_tenant_id_subject");
            
                entity.HasIndex(e => new { e.TenantId, e.InternalEntryId })
                    .HasDatabaseName("idx_tenant_id_internal_entry_id");
            
                // Mapping Entity Properties to PostgreSQL Database Columns
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId)
                    .HasColumnName("entry_id")
                    .HasColumnType("character varying(50)");

                entity.Property(e => e.InternalEntryId)
                    .HasColumnName("internal_entry_id");
            
                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.Subject)
                    .HasColumnName("subject")
                    .HasColumnType("uuid");

                entity.Property(e => e.Owner)
                    .IsRequired()
                    .HasColumnName("owner")
                    .HasColumnType("uuid");

                entity.Property(e => e.Share).HasColumnName("security");

                entity.Property(e => e.TimeStamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.SubjectType).HasColumnName("subject_type");

                entity.Property(e => e.Options)
                    .HasColumnName("options")
                    .HasColumnType("jsonb");
            });
        }
    }
}