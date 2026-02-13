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