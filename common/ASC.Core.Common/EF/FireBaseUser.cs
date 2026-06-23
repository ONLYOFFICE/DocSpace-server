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

/// <summary>
/// The Firebase user parameters.
/// </summary>
public class FireBaseUser : BaseEntity
{
    /// <summary>
    /// The Firebase user ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// The Firebase device token.
    /// </summary>
    /// <example>token123</example>
    [MaxLength(255)]
    public string FirebaseDeviceToken { get; set; }

    /// <summary>
    /// The Firebase application.
    /// </summary>
    /// <example>web</example>
    [MaxLength(20)]
    public string Application { get; set; }

    /// <summary>
    /// Specifies if the user is subscribed to the push notifications or not.
    /// </summary>
    /// <example>true</example>
    public bool? IsSubscribed { get; set; }

    /// <summary>
    /// The database tenant parameters.
    /// </summary>
    /// <example>{"id": 1, "name": "Main Tenant", "alias": "main", "mappedDomain": "example.com", "version": 5 }</example>
    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class FireBaseUserExtension
{
    public static ModelBuilderWrapper AddFireBaseUsers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<FireBaseUser>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddFireBaseUsers, Provider.MySql)
            .Add(PgSqlAddFireBaseUsers, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddFireBaseUsers()
        {
            modelBuilder.Entity<FireBaseUser>(entity =>
            {
                entity.HasKey(e => new { e.Id })
                    .HasName("PRIMARY");

                entity.ToTable("firebase_users");

                entity.HasIndex(e => new { e.TenantId, e.UserId })
                    .HasDatabaseName("user_id");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.IsSubscribed).HasColumnName("is_subscribed");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("varchar(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.FirebaseDeviceToken)
                    .HasColumnName("firebase_device_token")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Application)
                    .HasColumnName("application")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

            });
        }

        public void PgSqlAddFireBaseUsers()
        {

            modelBuilder.Entity<FireBaseUser>(entity =>
            {
                entity.HasKey(e => new { e.Id })
                    .HasName("PK_FireBaseUser");

                entity.ToTable("firebase_users");

                entity.HasIndex(e => new { e.TenantId, e.UserId })
                    .HasDatabaseName("IX_firebase_users_user_id");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.IsSubscribed).HasColumnName("is_subscribed");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.FirebaseDeviceToken)
                    .HasColumnName("firebase_device_token")
                    .HasColumnType("varchar");

                entity.Property(e => e.Application)
                    .HasColumnName("application")
                    .HasColumnType("varchar");
            });
        }
    }
}