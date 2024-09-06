namespace ASC.Migrations.Core.Identity;

public static class IdentityExtension
{
    public static ModelBuilderWrapper AddIdentity(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<IdentityAuthorization>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddIdentity, Provider.MySql);

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

        modelBuilder.Entity<IdentityClient>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PRIMARY");

            entity.ToTable("identity_clients");

            entity.HasIndex(e => e.ClientId, "UK_client_id").IsUnique();

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_clients_is_invalidated");

            entity.HasIndex(e => e.TenantId, "idx_identity_clients_tenant_id");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id");
            entity.Property(e => e.ClientSecret)
                .HasMaxLength(255)
                .HasColumnName("client_secret")
                .IsRequired();
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

            entity.HasOne(e => e.Tenant)
                   .WithMany()
                   .HasForeignKey(b => b.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityClientAllowedOrigin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_allowed_origins");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_allowed_origins_client_id");

            entity.Property(e => e.AllowedOrigin)
                .HasColumnType("tinytext")
                .HasColumnName("allowed_origin")
                .IsRequired();
            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id")
                .IsRequired();

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

        modelBuilder.Entity<IdentityClientRedirectUri>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_redirect_uris");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_redirect_uris_client_id");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36)
                .HasColumnName("client_id")
                .IsRequired();
            entity.Property(e => e.RedirectUri)
                .HasColumnType("tinytext")
                .HasColumnName("redirect_uri")
                .IsRequired();

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

        modelBuilder.Entity<IdentityConsent>(entity =>
        {
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId }).HasName("PRIMARY");

            entity.ToTable("identity_consents");

            entity.HasIndex(e => e.IsInvalidated, "idx_identity_consents_is_invalidated");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_consents_principal_id");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_consents_registered_client_id");

            entity.Property(e => e.PrincipalId)
            .HasMaxLength(255)
            .HasColumnName("principal_id");
            entity.Property(e => e.RegisteredClientId)
            .HasMaxLength(36)
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
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId, e.ScopeName }).HasName("PRIMARY");

            entity.ToTable("identity_consent_scopes");

            entity.HasIndex(e => e.PrincipalId, "idx_identity_consent_scopes_principal_id");

            entity.HasIndex(e => e.RegisteredClientId, "idx_identity_consent_scopes_registered_client_id");

            entity.HasIndex(e => e.ScopeName, "idx_identity_consent_scopes_scope_name");


            entity.Property(e => e.PrincipalId)
                .HasColumnName("principal_id")
                .HasMaxLength(255);

            entity.Property(e => e.RegisteredClientId)
                .HasMaxLength(36)
                .HasColumnName("registered_client_id");

            entity.Property(e => e.ScopeName)
                .HasColumnName("scope_name");

            entity.HasOne(d => d.Consent)
                .WithMany(p => p.IdentityConsentScopes)
                .HasForeignKey(d => new { d.PrincipalId, d.RegisteredClientId })
                .HasConstraintName("identity_consent_scopes_ibfk_1");


            entity.HasOne(d => d.ScopeNameNavigation)
            .WithMany(p => p.IdentityConsentScopes)
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

        modelBuilder.Entity<IdentityShedlock>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("PRIMARY");

            entity.ToTable("identity_shedlock");

            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            entity.Property(e => e.LockUntil)
                .HasColumnType("timestamp(3)")
                .HasColumnName("lock_until");
            entity.Property(e => e.LockedAt)
                .HasColumnType("timestamp(3)")
                .HasColumnName("locked_at");
            entity.Property(e => e.LockedBy)
                .HasMaxLength(255)
                .HasColumnName("locked_by")
                .IsRequired();
        });
    }
}
