﻿namespace ASC.Migrations.Core.Identity;

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

    public virtual ICollection<IdentityAuthorization> IdentityAuthorizations { get; set; } = new List<IdentityAuthorization>();

    public virtual ICollection<IdentityConsent> IdentityConsents { get; set; } = new List<IdentityConsent>();

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

    public static void MySqlAddIdentityClient(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityCert>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("identity_certs");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(36);
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasColumnName("created_at");
            entity.Property(e => e.PairType).HasColumnName("pair_type");
            entity.Property(e => e.PrivateKey)
                .HasColumnType("text")
                .HasColumnName("private_key")
                .IsRequired();
            entity.Property(e => e.PublicKey)
                .HasColumnType("text")
                .HasColumnName("public_key")
                .IsRequired();
        });
    }

    public static void PgSqlAddIdentityClient(this ModelBuilder modelBuilder)
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
                .HasMaxLength(255);

            entity.Property(e => e.ModifiedOn)
                .HasColumnName("modified_on");

            entity.Property(e => e.ModifiedBy)
                .HasColumnName("modified_by")
                .HasMaxLength(255);

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
