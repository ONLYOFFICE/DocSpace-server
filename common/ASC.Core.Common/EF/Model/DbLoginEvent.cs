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

namespace ASC.MessagingSystem.EF.Model;

public class DbLoginEvent : MessageEvent
{
    [MaxLength(200)]
    public string Login { get; set; }
    public bool Active { get; set; }
    [MaxLength(500)]
    public string DescriptionRaw { get; set; }

    public DbTenant Tenant { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class DbLoginEventMapper(EventTypeConverter converter)
{
    private partial DbLoginEvent Map(EventMessage source);

    public DbLoginEvent MapManual(EventMessage source)
    {
        var result = Map(source);
        converter.Convert(source, result);
        return result;
    }
}

public static class LoginEventsExtension
{
    public static ModelBuilderWrapper AddLoginEvents(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbLoginEvent>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddLoginEvents, Provider.MySql)
            .Add(PgSqlAddLoginEvents, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddLoginEvents()
        {
            modelBuilder.Entity<DbLoginEvent>(entity =>
            {
                entity.ToTable("login_events")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("date");

                entity.HasIndex(e => new { e.TenantId, e.UserId })
                    .HasDatabaseName("tenant_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Action)
                    .HasColumnName("action")
                    .IsRequired(false);

                entity.Property(e => e.Browser)
                    .HasColumnName("browser")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.DescriptionRaw)
                    .HasColumnName("description")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Ip)
                    .HasColumnName("ip")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Login)
                    .HasColumnName("login")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Page)
                    .HasColumnName("page")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Platform)
                    .HasColumnName("platform")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.Active)
                    .HasColumnName("active");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddLoginEvents()
        {
            modelBuilder.Entity<DbLoginEvent>(entity =>
            {
                entity.ToTable("login_events");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("idx_date");

                entity.HasIndex(e => new { e.TenantId, e.UserId })
                    .HasDatabaseName("idx_tenant_id_user_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Action)
                    .HasColumnName("action")
                    .IsRequired(false);

                entity.Property(e => e.Browser)
                    .HasColumnName("browser")
                    .HasColumnType("varchar");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.DescriptionRaw)
                    .HasColumnName("description")
                    .HasColumnType("varchar");

                entity.Property(e => e.Ip)
                    .HasColumnName("ip")
                    .HasColumnType("varchar");

                entity.Property(e => e.Login)
                    .HasColumnName("login")
                    .HasColumnType("varchar");

                entity.Property(e => e.Page)
                    .HasColumnName("page")
                    .HasColumnType("varchar");

                entity.Property(e => e.Platform)
                    .HasColumnName("platform")
                    .HasColumnType("varchar");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.Active)
                    .HasColumnName("active");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");
            });
        }
    }
}