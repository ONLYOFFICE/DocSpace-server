namespace ASC.Migrations.Core.Identity;

public class IdentityAuthorization
{
    public string? Id { get; set; }

    public string RegisteredClientId { get; set; } = null!;

    public string PrincipalId { get; set; } = null!;

    public int TenantId { get; set; }

    public string? State { get; set; }

    public string? Attributes { get; set; }

    public string? AuthorizationGrantType { get; set; }

    public string? AuthorizedScopes { get; set; }

    public string? AuthorizationCodeValue { get; set; }

    public string? AuthorizationCodeMetadata { get; set; }

    public DateTime? AuthorizationCodeIssuedAt { get; set; }

    public DateTime? AuthorizationCodeExpiresAt { get; set; }

    public string? AccessTokenType { get; set; }

    public string? AccessTokenValue { get; set; }

    public string? AccessTokenHash { get; set; }

    public string? AccessTokenScopes { get; set; }

    public string? AccessTokenMetadata { get; set; }

    public DateTime? AccessTokenIssuedAt { get; set; }

    public DateTime? AccessTokenExpiresAt { get; set; }

    public string? RefreshTokenValue { get; set; }

    public string? RefreshTokenHash { get; set; }

    public string? RefreshTokenMetadata { get; set; }

    public DateTime? RefreshTokenIssuedAt { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool? IsInvalidated { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual IdentityClient RegisteredClient { get; set; } = null!;

    public DbTenant Tenant { get; set; }
}

public static class IdentityExtension
{
    public static ModelBuilderWrapper AddIdentity(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<IdentityAuthorization>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddIdentity, Provider.MySql)
            .Add(PgSqlAddIdentity, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddIdentity(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityAuthorization>(entity =>
        {
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId, e.AuthorizationGrantType }).HasName("PRIMARY");

            entity.ToTable("identity_authorizations");

            entity.HasIndex(e => e.Id, "UK_id").IsUnique();

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_authorizations_is_invalidated");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_authorizations_principal_id");

            entity.HasIndex(e => e.AuthorizationGrantType, "idx_identity_authorizations_grant_type");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_authorizations_registered_client_id");

            entity.Property(e => e.PrincipalId).HasColumnName("principal_id");
            entity.Property(e => e.RegisteredClientId)
            .HasColumnName("registered_client_id");
            entity.Property(e => e.AccessTokenExpiresAt)
                .HasMaxLength(6)
                .HasColumnName("access_token_expires_at");
            entity.Property(e => e.AccessTokenHash)
                .HasColumnType("text")
                .HasColumnName("access_token_hash");
            entity.Property(e => e.AccessTokenIssuedAt)
                .HasMaxLength(6)
                .HasColumnName("access_token_issued_at");
            entity.Property(e => e.AccessTokenMetadata)
                .HasColumnType("text")
                .HasColumnName("access_token_metadata");
            entity.Property(e => e.AccessTokenScopes)
                .HasColumnType("text")
                .HasColumnName("access_token_scopes");
            entity.Property(e => e.AccessTokenType)
                .HasMaxLength(255)
                .HasColumnName("access_token_type");
            entity.Property(e => e.AccessTokenValue)
                .HasColumnType("text")
                .HasColumnName("access_token_value");
            entity.Property(e => e.Attributes)
                .HasColumnType("text")
                .HasColumnName("attributes");
            entity.Property(e => e.AuthorizationCodeExpiresAt)
                .HasMaxLength(6)
                .HasColumnName("authorization_code_expires_at");
            entity.Property(e => e.AuthorizationCodeIssuedAt)
                .HasMaxLength(6)
                .HasColumnName("authorization_code_issued_at");
            entity.Property(e => e.AuthorizationCodeMetadata)
                .HasMaxLength(255)
                .HasColumnName("authorization_code_metadata");
            entity.Property(e => e.AuthorizationCodeValue)
                .HasColumnType("text")
                .HasColumnName("authorization_code_value");
            entity.Property(e => e.AuthorizationGrantType)
                .HasMaxLength(255)
                .HasColumnName("authorization_grant_type");
            entity.Property(e => e.AuthorizedScopes)
                .HasColumnType("text")
                .HasColumnName("authorized_scopes");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsInvalidated)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_invalidated");
            entity.Property(e => e.ModifiedAt)
                .HasMaxLength(6)
                .HasColumnName("modified_at");
            entity.Property(e => e.RefreshTokenExpiresAt)
                .HasMaxLength(6)
                .HasColumnName("refresh_token_expires_at");
            entity.Property(e => e.RefreshTokenHash)
                .HasColumnType("text")
                .HasColumnName("refresh_token_hash");
            entity.Property(e => e.RefreshTokenIssuedAt)
                .HasMaxLength(6)
                .HasColumnName("refresh_token_issued_at");
            entity.Property(e => e.RefreshTokenMetadata)
                .HasColumnType("text")
                .HasColumnName("refresh_token_metadata");
            entity.Property(e => e.RefreshTokenValue)
                .HasColumnType("text")
                .HasColumnName("refresh_token_value");
            entity.Property(e => e.State)
                .HasMaxLength(500)
                .HasColumnName("state");
            entity.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

            entity.HasOne(d => d.RegisteredClient).WithMany(p => p.IdentityAuthorizations)
                .HasForeignKey(d => d.RegisteredClientId)
                .HasConstraintName("FK_authorization_client_id");

            entity.HasOne(e => e.Tenant)
                   .WithMany()
                   .HasForeignKey(b => b.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public static void PgSqlAddIdentity(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityAuthorization>(entity =>
        {
            // Define table name and schema if needed (PostgreSQL supports schemas).
            entity.ToTable("IdentityAuthorizations");

            // Define primary key.
            entity.HasKey(e => e.Id);

            // Configure properties with explicit column types and settings (if necessary).
            entity.Property(e => e.Id)
                .HasColumnType("text"); // PostgreSQL-specific for string IDs.

            entity.Property(e => e.RegisteredClientId)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.PrincipalId)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasColumnType("integer");

            entity.Property(e => e.State)
                .HasColumnType("text");

            entity.Property(e => e.Attributes)
                .HasColumnType("jsonb"); // PostgreSQL supports JSONB for efficient querying.

            entity.Property(e => e.AuthorizationGrantType)
                .HasColumnType("text");

            entity.Property(e => e.AuthorizedScopes)
                .HasColumnType("text");

            entity.Property(e => e.AuthorizationCodeValue)
                .HasColumnType("text");

            entity.Property(e => e.AuthorizationCodeMetadata)
                .HasColumnType("jsonb");

            entity.Property(e => e.AuthorizationCodeIssuedAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.AuthorizationCodeExpiresAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.AccessTokenType)
                .HasColumnType("text");

            entity.Property(e => e.AccessTokenValue)
                .HasColumnType("text");

            entity.Property(e => e.AccessTokenHash)
                .HasColumnType("text");

            entity.Property(e => e.AccessTokenScopes)
                .HasColumnType("text");

            entity.Property(e => e.AccessTokenMetadata)
                .HasColumnType("jsonb");

            entity.Property(e => e.AccessTokenIssuedAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.AccessTokenExpiresAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.RefreshTokenValue)
                .HasColumnType("text");

            entity.Property(e => e.RefreshTokenHash)
                .HasColumnType("text");

            entity.Property(e => e.RefreshTokenMetadata)
                .HasColumnType("jsonb");

            entity.Property(e => e.RefreshTokenIssuedAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.RefreshTokenExpiresAt)
                .HasColumnType("timestamp");

            entity.Property(e => e.IsInvalidated)
                .HasColumnType("boolean");

            entity.Property(e => e.ModifiedAt)
                .HasColumnType("timestamp");

            // Configure relationships and navigation properties.
            entity.HasOne(e => e.RegisteredClient)
                .WithMany()
                .HasForeignKey(e => e.RegisteredClientId)
                .OnDelete(DeleteBehavior.Cascade); // Example setting, adjust as per requirements.

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade); // Example setting, adjust as per requirements.
        });
    }
}