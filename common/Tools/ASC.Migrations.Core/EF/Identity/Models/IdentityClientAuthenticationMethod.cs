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

namespace ASC.Migrations.Core.Identity;

public class IdentityClientAuthenticationMethod
{
    public string ClientId { get; set; } = null!;

    public string AuthenticationMethod { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}

public static class IdentityClientAuthenticationMethodExtension
{
    public static ModelBuilderWrapper AddIdentityClientAuthenticationMethod(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClientAuthenticationMethod, Provider.MySql)
            .Add(PgSqlAddIdentityClientAuthenticationMethod, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityClientAuthenticationMethod()
        {
            modelBuilder.Entity<IdentityClientAuthenticationMethod>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("identity_client_authentication_methods");

                entity.HasIndex(e => e.ClientId, "idx_client_authentication_methods_client_id");

                entity.Property(e => e.AuthenticationMethod)
                    .HasColumnType("enum('client_secret_post','none')")
                    .HasColumnName("authentication_method")
                    .IsRequired();
                entity.Property(e => e.ClientId)
                    .HasMaxLength(36)
                    .HasColumnName("client_id")
                    .IsRequired();

                entity.HasOne(d => d.Client).WithMany()
                    .HasForeignKey(d => d.ClientId)
                    .HasConstraintName("identity_client_authentication_methods_ibfk_1");
            });
        }

        public void PgSqlAddIdentityClientAuthenticationMethod()
        {
            modelBuilder.Entity<IdentityClientAuthenticationMethod>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("identity_client_authentication_methods"); // Sets the table name

                entity.HasIndex(e => e.ClientId, "idx_client_authentication_methods_client_id"); // Defines an index for ClientId

                entity.Property(e => e.AuthenticationMethod)
                    .HasColumnType("text") // In PostgreSQL, "text" is often used for unbounded strings
                    .HasColumnName("authentication_method")
                    .IsRequired(); // Marks the column as not nullable

                entity.Property(e => e.ClientId)
                    .HasMaxLength(36) // Indicates the string length constraint
                    .HasColumnName("client_id")
                    .IsRequired(); // Marks the column as not nullable

                entity.HasOne(d => d.Client).WithMany()
                    .HasForeignKey(d => d.ClientId)
                    .HasConstraintName("identity_client_authentication_methods_fk_client_id"); // Defines a foreign key constraint
            });
        }
    }
}