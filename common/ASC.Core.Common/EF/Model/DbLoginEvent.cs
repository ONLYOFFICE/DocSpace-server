﻿// (c) Copyright Ascensio System SIA 2009-2025
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

using Profile = AutoMapper.Profile;

namespace ASC.MessagingSystem.EF.Model;

public class DbLoginEvent : MessageEvent, IMapFrom<EventMessage>
{
    [MaxLength(200)]
    public string Login { get; set; }
    public bool Active { get; set; }
    [MaxLength(500)]
    public string DescriptionRaw { get; set; }

    public DbTenant Tenant { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<MessageEvent, DbLoginEvent>();
        profile.CreateMap<EventMessage, DbLoginEvent>()
            .ConvertUsing<EventTypeConverter>();
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

    public static void MySqlAddLoginEvents(this ModelBuilder modelBuilder)
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

    public static void PgSqlAddLoginEvents(this ModelBuilder modelBuilder)
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
