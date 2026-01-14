// (c) Copyright Ascensio System SIA 2009-2026
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