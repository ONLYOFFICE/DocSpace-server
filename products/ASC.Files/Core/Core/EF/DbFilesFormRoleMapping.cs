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

public class DbFilesFormRoleMapping : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    public int FormId { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(255)]
    public string RoleName { get; set; }
    [MaxLength(6)]
    public string RoleColor { get; set; }
    public int Sequence { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime SubmissionDate { get; set; }
    public bool Submitted { get; set; }

    public DbTenant Tenant { get; set; }
    public override object[] GetKeys()
    {
        return [TenantId, FormId, RoleName, UserId];
    }
}

public static class DbFilesFormRoleMappingExtension
{
    public static ModelBuilderWrapper AddDbFilesFormRoleMapping(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesFormRoleMapping>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesFormRoleMapping, Provider.MySql)
            .Add(PgSqlAddDbFilesFormRoleMapping, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesFormRoleMapping()
        {
            modelBuilder.Entity<DbFilesFormRoleMapping>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.FormId, e.RoleName, e.UserId })
                    .HasName("PRIMARY");

                entity.ToTable("files_form_role_mapping")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.FormId })
                    .HasDatabaseName("tenant_id_form_id");

                entity.HasIndex(e => new { e.TenantId, e.FormId, e.UserId })
                    .HasDatabaseName("tenant_id_form_id_user_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.FormId).HasColumnName("form_id");
                entity.Property(e => e.RoomId).HasColumnName("room_id");
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasColumnName("role_name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.RoleColor)
                    .HasColumnName("role_color")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Sequence).HasColumnName("sequence");

                entity.Property(e => e.OpenedAt)
                    .HasColumnName("opened_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.SubmissionDate)
                    .HasColumnName("submission_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Submitted)
                    .HasColumnName("submitted")
                    .HasColumnType("tinyint(1)");

            });
        }

        public void PgSqlAddDbFilesFormRoleMapping()
        {
            modelBuilder.Entity<DbFilesFormRoleMapping>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.FormId, e.RoleName, e.UserId })
                    .HasName("files_form_role_mapping_pkey");

                entity.ToTable("files_form_role_mapping", "onlyoffice");

                entity.HasIndex(e => new { e.TenantId, e.FormId })
                    .HasDatabaseName("tenant_id_form_id");

                entity.HasIndex(e => new { e.TenantId, e.FormId, e.UserId })
                    .HasDatabaseName("tenant_id_form_id_user_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.FormId).HasColumnName("form_id");
                entity.Property(e => e.RoomId).HasColumnName("room_id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasMaxLength(38);

                entity.Property(e => e.RoleName).HasColumnName("role_name").HasColumnType("varchar(255)");
                entity.Property(e => e.RoleColor).HasColumnName("role_color").HasColumnType("char(6)");
                entity.Property(e => e.Sequence).HasColumnName("sequence");

                entity.Property(e => e.OpenedAt)
                    .HasColumnName("opened_at")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.SubmissionDate)
                    .HasColumnName("submission_date")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Submitted).HasColumnName("submitted");

            });
        }
    }
}