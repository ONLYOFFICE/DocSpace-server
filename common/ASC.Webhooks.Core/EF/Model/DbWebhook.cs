// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Webhooks.Core.EF.Model;

public class DbWebhook : IMapFrom<Webhook>
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

    private static void MySqlAddDbWebhook(this ModelBuilder modelBuilder)
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

    private static void PgSqlAddDbWebhook(this ModelBuilder modelBuilder)
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