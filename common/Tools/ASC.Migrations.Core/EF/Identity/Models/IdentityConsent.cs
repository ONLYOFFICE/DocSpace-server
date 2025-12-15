namespace ASC.Migrations.Core.Identity;

public class IdentityConsent
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public bool? IsInvalidated { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}

public static class IdentityConsentExtension
{
    public static ModelBuilderWrapper AddIdentityConsent(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityConsent, Provider.MySql)
            .Add(PgSqlAddIdentityConsent, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityConsent()
        {
            modelBuilder.Entity<IdentityConsent>(entity =>
            {
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId }).HasName("PRIMARY");

                entity.ToTable("identity_consents");

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
            });
        }

        public void PgSqlAddIdentityConsent()
        {
            modelBuilder.Entity<IdentityConsent>(entity =>
            {
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId })
                    .HasName("pk_identity_consents"); // PostgreSQL prefers prefix "pk" for primary keys instead of "PRIMARY"

                entity.ToTable("identity_consents");

                entity.Property(e => e.PrincipalId)
                    .HasMaxLength(255)
                    .HasColumnName("principal_id");

                entity.Property(e => e.RegisteredClientId)
                    .HasMaxLength(36)
                    .HasColumnName("registered_client_id");

                entity.Property(e => e.IsInvalidated)
                    .HasDefaultValueSql("false") // PostgreSQL uses "false" for boolean false
                    .HasColumnName("is_invalidated");

                entity.Property(e => e.ModifiedAt)
                    .HasColumnType("timestamp with time zone") // PostgreSQL equivalent for a timestamp column
                    .HasColumnName("modified_at");
            });
        }
    }
}