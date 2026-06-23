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

namespace ASC.Core.Common.EF;

public class UserGroup : BaseEntity
{
    public int TenantId { get; set; }
    public Guid Userid { get; set; }
    public Guid UserGroupId { get; set; }
    public UserGroupRefType RefType { get; set; }
    public bool Removed { get; set; }
    public DateTime LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, Userid, UserGroupId, RefType];
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class UserGroupMapper
{
    [MapProperty(nameof(UserGroupRef.GroupId), nameof(UserGroup.UserGroupId))]
    public static partial UserGroup Map(this UserGroupRef source);
}

public static class DbUserGroupExtension
{
    public static ModelBuilderWrapper AddUserGroup(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<UserGroup>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddUserGroup, Provider.MySql)
            .Add(PgSqlAddUserGroup, Provider.PostgreSql)
            .HasData(
            new UserGroup
            {
                TenantId = 1,
                Userid = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef"),
                UserGroupId = Guid.Parse("cd84e66b-b803-40fc-99f9-b2969a54a1de"),
                RefType = 0,
                LastModified = new DateTime(2022, 7, 8, 0, 0, 0, DateTimeKind.Utc)
            }
            );

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddUserGroup()
        {
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Userid, e.UserGroupId, e.RefType })
                    .HasName("PRIMARY");

                entity.ToTable("core_usergroup")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("last_modified");

                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.Userid)
                    .HasColumnName("userid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserGroupId)
                    .HasColumnName("groupid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.RefType).HasColumnName("ref_type");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamp");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");
            });
        }

        public void PgSqlAddUserGroup()
        {
            modelBuilder.Entity<UserGroup>(entity =>
            {
                // Define composite primary key
                entity.HasKey(e => new { e.TenantId, e.Userid, e.UserGroupId, e.RefType })
                    .HasName("core_usergroup_pkey");

                // Define the table name
                entity.ToTable("core_usergroup");

                // Define indexes
                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("core_usergroup_last_modified_idx");

                // Map properties to database columns
                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.Userid)
                    .HasColumnName("userid")
                    .HasColumnType("uuid"); // PostgreSQL UUID type

                entity.Property(e => e.UserGroupId)
                    .HasColumnName("groupid")
                    .HasColumnType("uuid"); // PostgreSQL UUID type

                entity.Property(e => e.RefType).HasColumnName("ref_type");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("boolean")
                    .HasDefaultValueSql("false");
            });
        }
    }
}