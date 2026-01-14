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

public class IdentityClient
{
    public string ClientId { get; set; } = null!;

    public int TenantId { get; set; }

    public string? ClientSecret { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Logo { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? TermsUrl { get; set; }

    public string? PolicyUrl { get; set; }

    public string? LogoutRedirectUri { get; set; }

    public bool? IsPublic { get; set; }

    public bool? IsEnabled { get; set; }

    public bool? IsInvalidated { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public int Version { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class IdentityClientExtension
{
    public static ModelBuilderWrapper AddIIdentityClient(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClient, Provider.MySql)
            .Add(PgSqlAddIdentityClient, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityClient()
        {
            modelBuilder.Entity<IdentityClient>(entity =>
            {
                entity.HasKey(e => e.ClientId).HasName("PRIMARY");

                entity.ToTable("identity_clients");

                entity.HasIndex(e => e.ClientId, "UK_client_id").IsUnique();

                entity.HasIndex(e => e.ClientSecret, "idx_client_secret");

                entity.HasIndex(e => e.TenantId, "idx_identity_clients_tenant_id");

                entity.Property(e => e.ClientId)
                    .HasMaxLength(36)
                    .HasColumnName("client_id");
                entity.Property(e => e.ClientSecret)
                    .HasMaxLength(255)
                    .HasColumnName("client_secret")
                    .IsRequired();
                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(36)
                    .HasColumnName("created_by");
                entity.Property(e => e.CreatedOn)
                    .HasMaxLength(6)
                    .HasColumnName("created_on");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsEnabled)
                    .HasDefaultValueSql("'1'")
                    .HasColumnName("is_enabled");
                entity.Property(e => e.IsInvalidated)
                    .HasDefaultValueSql("'0'")
                    .HasColumnName("is_invalidated");
                entity.Property(e => e.IsPublic)
                    .HasDefaultValueSql("'0'")
                    .HasColumnName("is_public");
                entity.Property(e => e.Logo).HasColumnName("logo");
                entity.Property(e => e.LogoutRedirectUri)
                    .HasColumnType("tinytext")
                    .HasColumnName("logout_redirect_uri");
                entity.Property(e => e.ModifiedBy)
                    .HasMaxLength(36)
                    .HasColumnName("modified_by");
                entity.Property(e => e.ModifiedOn)
                    .HasMaxLength(6)
                    .HasColumnName("modified_on");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
                entity.Property(e => e.PolicyUrl)
                    .HasColumnType("tinytext")
                    .HasColumnName("policy_url");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.TermsUrl)
                    .HasColumnType("tinytext")
                    .HasColumnName("terms_url");
                entity.Property(e => e.WebsiteUrl)
                    .HasColumnType("tinytext")
                    .HasColumnName("website_url");

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .HasDefaultValueSql("0");

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(b => b.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public void PgSqlAddIdentityClient()
        {
            modelBuilder.Entity<IdentityClient>(entity =>
            {
                // Set up the primary key
                entity.HasKey(e => e.ClientId);

                // Map to a specific table
                entity.ToTable("identity_clients");

                // Map the properties to columns
                entity.Property(e => e.ClientId)
                    .HasColumnName("client_id")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id")
                    .IsRequired();

                entity.Property(e => e.ClientSecret)
                    .HasColumnName("client_secret")
                    .HasMaxLength(512);

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255);

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1000);

                entity.Property(e => e.Logo)
                    .HasColumnName("logo")
                    .HasMaxLength(512);

                entity.Property(e => e.WebsiteUrl)
                    .HasColumnName("website_url")
                    .HasMaxLength(512);

                entity.Property(e => e.TermsUrl)
                    .HasColumnName("terms_url")
                    .HasMaxLength(512);

                entity.Property(e => e.PolicyUrl)
                    .HasColumnName("policy_url")
                    .HasMaxLength(512);

                entity.Property(e => e.LogoutRedirectUri)
                    .HasColumnName("logout_redirect_uri")
                    .HasMaxLength(512);

                entity.Property(e => e.IsPublic)
                    .HasColumnName("is_public");

                entity.Property(e => e.IsEnabled)
                    .HasColumnName("is_enabled");

                entity.Property(e => e.IsInvalidated)
                    .HasColumnName("is_invalidated");

                entity.Property(e => e.CreatedOn)
                    .HasColumnName("created_on");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(36);

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("modified_by")
                    .HasMaxLength(36);

                entity.Property(e => e.Version)
                    .HasColumnName("version");

                // Define relationships
                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_identity_client_tenant");

                // Optional: Set indexes if needed
                entity.HasIndex(e => e.ClientId)
                    .HasDatabaseName("ix_identity_client_client_id")
                    .IsUnique();

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("ix_identity_client_tenant_id");
            });
        }
    }
}