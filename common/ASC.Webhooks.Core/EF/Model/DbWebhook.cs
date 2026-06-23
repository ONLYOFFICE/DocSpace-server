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

namespace ASC.Webhooks.Core.EF.Model;

public class DbWebhook
{
    public int Id { get; set; }
    [MaxLength(200)]
    public string Route { get; set; }
    [MaxLength(10)]
    public string Method { get; set; }
}

public static class DbWebhookExtension
{
    public static ModelBuilderWrapper AddDbWebhooks(this ModelBuilderWrapper modelBuilder)
    {
        return modelBuilder
            .Add(MySqlAddDbWebhook, Provider.MySql)
            .Add(PgSqlAddDbWebhook, Provider.PostgreSql);
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddDbWebhook()
        {
            modelBuilder.Entity<DbWebhook>(entity =>
            {
                entity.HasKey(e => new { e.Id })
                    .HasName("PRIMARY");

                entity.ToTable("webhooks")
                    .HasCharSet("utf8");

                entity.Property(e => e.Id)
                    .HasColumnType("int")
                    .HasColumnName("id");

                entity.Property(e => e.Route)
                    .HasColumnName("route")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Method)
                    .HasColumnName("method")
                    .HasDefaultValueSql("''");
            });
        }

        private void PgSqlAddDbWebhook()
        {
            modelBuilder.Entity<DbWebhook>(entity =>
            {
                // Define primary key
                entity.HasKey(e => new { e.Id })
                    .HasName("webhooks_pkey"); // Default naming convention for PostgreSQL primary keys

                // Define table name
                entity.ToTable("webhooks"); // PostgreSQL typically uses snake_case for table names

                // Define properties
                entity.Property(e => e.Id)
                    .HasColumnName("id") // PostgreSQL uses snake_case for column names
                    .HasColumnType("integer"); // PostgreSQL uses 'integer' for int type

                entity.Property(e => e.Route)
                    .HasColumnName("route")
                    .HasColumnType("character varying(200)") // Equivalent to MaxLength(200)
                    .HasDefaultValue(string.Empty); // Set default value to an empty string

                entity.Property(e => e.Method)
                    .HasColumnName("method")
                    .HasColumnType("character varying(10)") // Equivalent to MaxLength(10)
                    .HasDefaultValue(string.Empty); // Set default value to an empty string
            });
        }
    }
}