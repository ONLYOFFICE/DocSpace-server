namespace ASC.Migrations.Core.Identity;

public partial class IdentityContext : DbContext
{
    public IdentityContext()
    {
    }

    public IdentityContext(DbContextOptions<IdentityContext> options)
        : base(options)
    {
    }

    public virtual DbSet<IdentityAuthorization> IdentityAuthorizations { get; set; }

    public virtual DbSet<IdentityCert> IdentityCerts { get; set; }

    public virtual DbSet<IdentityClient> IdentityClients { get; set; }

    public virtual DbSet<IdentityClientAllowedOrigin> IdentityClientAllowedOrigins { get; set; }

    public virtual DbSet<IdentityClientAuthenticationMethod> IdentityClientAuthenticationMethods { get; set; }

    public virtual DbSet<IdentityClientRedirectUri> IdentityClientRedirectUris { get; set; }

    public virtual DbSet<IdentityClientScope> IdentityClientScopes { get; set; }

    public virtual DbSet<IdentityConsent> IdentityConsents { get; set; }

    public virtual DbSet<IdentityConsentScope> IdentityConsentScopes { get; set; }

    public virtual DbSet<IdentityScope> IdentityScopes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityAuthorization>(entity =>
        {
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId }).HasName("PRIMARY");

            entity.ToTable("identity_authorizations");

            entity.HasIndex(e => e.Id, "UK_id").IsUnique();

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_authorizations_is_invalidated");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_authorizations_principal_id");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_authorizations_registered_client_id");

            entity.Property(e => e.PrincipalId).HasColumnName("principal_id");
            entity.Property(e => e.RegisteredClientId).HasColumnName("registered_client_id");
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
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.HasOne(d => d.RegisteredClient).WithMany(p => p.IdentityAuthorizations)
                .HasForeignKey(d => d.RegisteredClientId)
                .HasConstraintName("FK_authorization_client_id");
        });

        modelBuilder.Entity<IdentityCert>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("identity_certs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasColumnName("created_at");
            entity.Property(e => e.PairType).HasColumnName("pair_type");
            entity.Property(e => e.PrivateKey)
                .HasColumnType("text")
                .HasColumnName("private_key");
            entity.Property(e => e.PublicKey)
                .HasColumnType("text")
                .HasColumnName("public_key");
        });

        modelBuilder.Entity<IdentityClient>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PRIMARY");

            entity.ToTable("identity_clients");

            entity.HasIndex(e => e.ClientId, "UK_client_id").IsUnique();

            entity.HasIndex(e => e.ClientSecret, "UK_client_secret").IsUnique();

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_clients_is_invalidated");

            entity.HasIndex(e => e.TenantId, "idx_identity_clients_tenant_id");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");
            entity.Property(e => e.ClientSecret).HasColumnName("client_secret");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
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
                .HasMaxLength(255)
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
        });

        modelBuilder.Entity<IdentityClientAllowedOrigin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_allowed_origins");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_allowed_origins_client_id");

            entity.Property(e => e.AllowedOrigin)
                .HasColumnType("tinytext")
                .HasColumnName("allowed_origin");
            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_allowed_origins_ibfk_1");
        });

        modelBuilder.Entity<IdentityClientAuthenticationMethod>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_authentication_methods");

            entity.HasIndex(e => e.ClientId, "idx_client_authentication_methods_client_id");

            entity.Property(e => e.AuthenticationMethod)
                .HasColumnType("enum('client_secret_post','none')")
                .HasColumnName("authentication_method");
            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_authentication_methods_ibfk_1");
        });

        modelBuilder.Entity<IdentityClientRedirectUri>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_redirect_uris");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_redirect_uris_client_id");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");
            entity.Property(e => e.RedirectUri)
                .HasColumnType("tinytext")
                .HasColumnName("redirect_uri");

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_redirect_uris_ibfk_1");
        });

        modelBuilder.Entity<IdentityClientScope>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_scopes");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_scopes_client_id");

            entity.HasIndex(e => e.ScopeName, "idx_identity_client_scopes_scope_name");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");
            entity.Property(e => e.ScopeName).HasColumnName("scope_name");

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_scopes_ibfk_1");

            entity.HasOne(d => d.ScopeNameNavigation).WithMany()
                .HasForeignKey(d => d.ScopeName)
                .HasConstraintName("identity_client_scopes_ibfk_2");
        });

        modelBuilder.Entity<IdentityConsent>(entity =>
        {
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId }).HasName("PRIMARY");

            entity.ToTable("identity_consents");

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_consents_is_invalidated");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_consents_principal_id");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_consents_registered_client_id");

            entity.Property(e => e.PrincipalId)
            .HasMaxLength(36)
            .HasColumnName("principal_id");
            entity.Property(e => e.RegisteredClientId)
            .HasMaxLength(255)
            .HasColumnName("registered_client_id");
            entity.Property(e => e.IsInvalidated)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_invalidated");
            entity.Property(e => e.ModifiedAt)
                .HasMaxLength(6)
                .HasColumnName("modified_at");

            entity.HasOne(d => d.RegisteredClient).WithMany(p => p.IdentityConsents)
                .HasForeignKey(d => d.RegisteredClientId)
                .HasConstraintName("identity_consents_ibfk_1");
        });

        modelBuilder.Entity<IdentityConsentScope>(entity =>
        {
            entity.HasKey(e => new { e.RegisteredClientId, e.PrincipalId, e.ScopeName }).HasName("PRIMARY");

            entity.ToTable("identity_consent_scopes");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_consent_scopes_principal_id");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_consent_scopes_registered_client_id");

            entity.HasIndex(e => e.ScopeName, "idx_identity_consent_scopes_scope_name");

            entity.Property(e => e.RegisteredClientId)
                .HasMaxLength(36)
                .HasColumnName("registered_client_id");
            entity.Property(e => e.PrincipalId).HasColumnName("principal_id");
            entity.Property(e => e.ScopeName).HasColumnName("scope_name");

            entity.HasOne(d => d.Consent)
                .WithMany(p => p.IdentityConsentScopes)
                .HasForeignKey(d => new { d.RegisteredClientId, d.PrincipalId })
                .HasConstraintName("identity_consent_scopes_ibfk_1");


            entity.HasOne(d => d.ScopeNameNavigation).WithMany(p => p.IdentityConsentScopes)
                .HasForeignKey(d => d.ScopeName)
                .HasConstraintName("identity_consent_scopes_ibfk_2");
        });

        modelBuilder.Entity<IdentityScope>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("PRIMARY");

            entity.ToTable("identity_scopes");

            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Group)
                .HasMaxLength(255)
                .HasColumnName("group");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
