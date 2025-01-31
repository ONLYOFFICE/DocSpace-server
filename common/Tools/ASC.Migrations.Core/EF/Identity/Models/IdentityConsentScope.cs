namespace ASC.Migrations.Core.Identity;

public class IdentityConsentScope
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public string ScopeName { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;

    public virtual IdentityConsent Consent { get; set; } = null!;
}

public static class IdentityConsentScopeExtension
{
    public static ModelBuilderWrapper AddIdentityConsentScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityConsentScope, Provider.MySql)
            .Add(PgSqlAddIdentityConsentScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityConsentScope(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityConsentScope(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityConsentScope>(entity =>
        {
            // Define composite primary key
            entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId, e.ScopeName }).HasName("pk_identity_consent_scopes");

            entity.ToTable("identity_consent_scopes"); // Map to table

            // Define indexes
            entity.HasIndex(e => e.PrincipalId, "ix_identity_consent_scopes_principal_id");
            entity.HasIndex(e => e.RegisteredClientId, "ix_identity_consent_scopes_registered_client_id");
            entity.HasIndex(e => e.ScopeName, "ix_identity_consent_scopes_scope_name");

            // Define columns
            entity.Property(e => e.PrincipalId)
                .HasColumnName("principal_id")
                .HasMaxLength(255);

            entity.Property(e => e.RegisteredClientId)
                .HasMaxLength(36)
                .HasColumnName("registered_client_id");

            entity.Property(e => e.ScopeName)
                .HasColumnName("scope_name");

            // Define foreign key relations
            entity.HasOne(d => d.Consent)
                .WithMany(p => p.IdentityConsentScopes)
                .HasForeignKey(d => new { d.PrincipalId, d.RegisteredClientId })
                .HasConstraintName("fk_identity_consent_scopes_principal_client");

            entity.HasOne(d => d.ScopeNameNavigation)
                .WithMany(p => p.IdentityConsentScopes)
                .HasForeignKey(d => d.ScopeName)
                .HasConstraintName("fk_identity_consent_scopes_scope_name");
        });
    }
}