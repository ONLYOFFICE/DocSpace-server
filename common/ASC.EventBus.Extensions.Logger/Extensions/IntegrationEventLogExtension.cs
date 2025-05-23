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

namespace ASC.EventBus.Extensions.Logger.Extensions;

public static class IntegrationEventLogExtension
{
    public static ModelBuilderWrapper AddIntegrationEventLog(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<IntegrationEventLogEntry>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddIntegrationEventLog, Provider.MySql)
            .Add(PgSqlAddIntegrationEventLog, Provider.PostgreSql);

        return modelBuilder;
    }
    public static void MySqlAddIntegrationEventLog(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationEventLogEntry>(entity =>
        {
            entity.ToTable("event_bus_integration_event_log")
                .HasCharSet("utf8");

            entity.HasKey(e => e.EventId)
                  .HasName("PRIMARY");

            entity.HasIndex(e => e.TenantId)
                  .HasDatabaseName("tenant_id");

            entity.Property(e => e.EventId)
                  .HasColumnName("event_id")
                  .HasColumnType("char(38)")
                  .HasCharSet("utf8")
                  .UseCollation("utf8_general_ci")
                  .IsRequired();

            entity.Property(e => e.Content)
                  .HasColumnName("content")
                  .HasColumnType("text")
                  .HasCharSet("utf8")
                  .UseCollation("utf8_general_ci")
                  .IsRequired();

            entity.Property(e => e.CreateOn)
                  .HasColumnName("create_on")
                  .HasColumnType("datetime")
                  .IsRequired();

            entity.Property(e => e.CreateBy)
                  .HasColumnName("create_by")
                  .HasColumnType("char(38)")
                  .HasCharSet("utf8")
                  .UseCollation("utf8_general_ci")
                  .IsRequired();

            entity.Property(e => e.State)
                  .HasColumnName("state")
                  .HasColumnType("int(11)")
                  .IsRequired();

            entity.Property(e => e.TimesSent)
                  .HasColumnName("times_sent")
                  .HasColumnType("int(11)")
                  .IsRequired();

            entity.Property(e => e.EventTypeName)
                  .HasColumnName("event_type_name")
                  .HasColumnType("varchar")
                  .HasCharSet("utf8")
                  .UseCollation("utf8_general_ci")
                  .IsRequired();

            entity.Property(e => e.TenantId)
                  .HasColumnName("tenant_id")
                  .HasColumnType("int(11)")
                  .IsRequired();
        });
    }

    public static void PgSqlAddIntegrationEventLog(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationEventLogEntry>(entity =>
        {
            entity.ToTable("event_bus_integration_event_log");

            entity.HasKey(e => e.EventId)
                  .HasName("pk_event_bus_integration_event_log");

            entity.HasIndex(e => e.TenantId)
                  .HasDatabaseName("ix_tenant_id");

            entity.Property(e => e.EventId)
                  .HasColumnName("event_id")
                  .HasColumnType("uuid")
                  .IsRequired();

            entity.Property(e => e.Content)
                  .HasColumnName("content")
                  .HasColumnType("text")
                  .IsRequired();

            entity.Property(e => e.CreateOn)
                  .HasColumnName("create_on")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            entity.Property(e => e.CreateBy)
                  .HasColumnName("create_by")
                  .HasColumnType("uuid")
                  .IsRequired();

            entity.Property(e => e.State)
                  .HasColumnName("state")
                  .HasColumnType("integer")
                  .IsRequired();

            entity.Property(e => e.TimesSent)
                  .HasColumnName("times_sent")
                  .HasColumnType("integer")
                  .IsRequired();

            entity.Property(e => e.EventTypeName)
                  .HasColumnName("event_type_name")
                  .HasColumnType("varchar")
                  .IsRequired();

            entity.Property(e => e.TenantId)
                  .HasColumnName("tenant_id")
                  .HasColumnType("integer")
                  .IsRequired();
        });
    }
}
