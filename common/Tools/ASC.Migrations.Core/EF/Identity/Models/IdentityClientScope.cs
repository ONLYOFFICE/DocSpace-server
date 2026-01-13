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

public class IdentityClientScope
{
    public string ClientId { get; set; } = null!;

    public string ScopeName { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;
}

public static class IdentityClientScopeExtension
{
    public static ModelBuilderWrapper AddIdentityClientScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClientScope, Provider.MySql)
            .Add(PgSqlAddIdentityClientScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityClientScope()
        {
            modelBuilder.Entity<IdentityClientScope>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("identity_client_scopes");

                entity.HasIndex(e => e.ClientId, "idx_identity_client_scopes_client_id");

                entity.HasIndex(e => e.ScopeName, "idx_identity_client_scopes_scope_name");

                entity.Property(e => e.ClientId)
                    .HasMaxLength(36)
                    .HasColumnName("client_id")
                    .IsRequired();
                entity.Property(e => e.ScopeName)
                    .HasColumnName("scope_name")
                    .IsRequired();

                entity.HasOne(d => d.Client).WithMany()
                    .HasForeignKey(d => d.ClientId)
                    .HasConstraintName("identity_client_scopes_ibfk_1");

                entity.HasOne(d => d.ScopeNameNavigation).WithMany()
                    .HasForeignKey(d => d.ScopeName)
                    .HasConstraintName("identity_client_scopes_ibfk_2");
            });
        }

        public void PgSqlAddIdentityClientScope()
        {
            modelBuilder.Entity<IdentityClientScope>(entity =>
            {
                // Define the table for PostgreSQL
                entity
                    .HasNoKey() // Indicates no primary key, similar to MySQL variant
                    .ToTable("identity_client_scopes");

                // Index for ClientId
                entity.HasIndex(e => e.ClientId).HasDatabaseName("idx_identity_client_scopes_client_id");

                // Index for ScopeName
                entity.HasIndex(e => e.ScopeName).HasDatabaseName("idx_identity_client_scopes_scope_name");

                // Configure ClientId property
                entity.Property(e => e.ClientId)
                    .HasMaxLength(36) // Specifies max length of 36
                    .HasColumnName("client_id") // Maps the property to the column named "client_id"
                    .IsRequired(); // This field is required

                // Configure ScopeName property
                entity.Property(e => e.ScopeName)
                    .HasColumnName("scope_name") // Maps the property to the column named "scope_name"
                    .IsRequired(); // This field is also required

                // Configure relationships
                // Link 'ClientId' to the related 'IdentityClient' entity
                entity.HasOne(d => d.Client).WithMany()
                    .HasForeignKey(d => d.ClientId)
                    .HasConstraintName("identity_client_scopes_client_id_fk");

                // Link 'ScopeName' to the related 'IdentityScope' entity
                entity.HasOne(d => d.ScopeNameNavigation).WithMany()
                    .HasForeignKey(d => d.ScopeName)
                    .HasConstraintName("identity_client_scopes_scope_name_fk");
            });
        }
    }
}