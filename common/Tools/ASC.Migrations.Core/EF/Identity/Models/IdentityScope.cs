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

public class IdentityScope
{
    public string Name { get; set; } = null!;

    public string Group { get; set; } = null!;

    public string Type { get; set; } = null!;

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}

public static class IdentityScopeExtension
{
    public static ModelBuilderWrapper AddIdentityScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityScope, Provider.MySql)
            .Add(PgSqlAddIdentityScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityScope()
        {
            modelBuilder.Entity<IdentityScope>(entity =>
            {
                entity.HasKey(e => e.Name).HasName("PRIMARY");

                entity.ToTable("identity_scopes");

                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Group)
                    .HasMaxLength(255)
                    .HasColumnName("group")
                    .IsRequired();
                entity.Property(e => e.Type)
                    .HasMaxLength(255)
                    .HasColumnName("type")
                    .IsRequired();

                entity.HasData(new IdentityScope
                {
                    Name = "accounts:read",
                    Group = "accounts",
                    Type = "read"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "accounts:write",
                    Group = "accounts",
                    Type = "write"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "accounts.self:read",
                    Group = "profiles",
                    Type = "read"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "accounts.self:write",
                    Group = "profiles",
                    Type = "write"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "files:read",
                    Group = "files",
                    Type = "read"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "files:write",
                    Group = "files",
                    Type = "write"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "openid",
                    Group = "openid",
                    Type = "openid"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "rooms:read",
                    Group = "rooms",
                    Type = "read"
                });

                entity.HasData(new IdentityScope
                {
                    Name = "rooms:write",
                    Group = "rooms",
                    Type = "write"
                });
            });
        }

        public void PgSqlAddIdentityScope()
        {
            modelBuilder.Entity<IdentityScope>(entity =>
            {
                // Define primary key
                entity.HasKey(e => e.Name).HasName("pk_identity_scopes");

                // Set the table name
                entity.ToTable("identity_scopes");

                // Map columns
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Group)
                    .HasMaxLength(255)
                    .HasColumnName("group")
                    .IsRequired();
                entity.Property(e => e.Type)
                    .HasMaxLength(255)
                    .HasColumnName("type")
                    .IsRequired();

                // Add seed data for PostgreSQL
                entity.HasData(new IdentityScope { Name = "accounts:read", Group = "accounts", Type = "read" });

                entity.HasData(new IdentityScope { Name = "accounts:write", Group = "accounts", Type = "write" });

                entity.HasData(new IdentityScope { Name = "accounts.self:read", Group = "profiles", Type = "read" });

                entity.HasData(new IdentityScope { Name = "accounts.self:write", Group = "profiles", Type = "write" });

                entity.HasData(new IdentityScope { Name = "files:read", Group = "files", Type = "read" });

                entity.HasData(new IdentityScope { Name = "files:write", Group = "files", Type = "write" });

                entity.HasData(new IdentityScope { Name = "openid", Group = "openid", Type = "openid" });

                entity.HasData(new IdentityScope { Name = "rooms:read", Group = "rooms", Type = "read" });

                entity.HasData(new IdentityScope { Name = "rooms:write", Group = "rooms", Type = "write" });
            });
        }
    }
}