﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.EF;

public class DbFilesSecurity : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    [MaxLength(50)]
    public string EntryId { get; set; }
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

    public static void MySqlAddDbFilesSecurity(this ModelBuilder modelBuilder)
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

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

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

    public static void PgSqlAddDbFilesSecurity(this ModelBuilder modelBuilder)
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

            // Mapping Entity Properties to PostgreSQL Database Columns
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("character varying(50)");

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
