namespace ASC.Migrations.Core.Identity;

public class IdentityClientRedirectUri
{
    public string ClientId { get; set; } = null!;

    public string RedirectUri { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}

public static class IdentityClientRedirectUriExtension
{
    public static ModelBuilderWrapper AddIdentityClientRedirectUri(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClientRedirectUri, Provider.MySql)
            .Add(PgSqlAddIdentityClientRedirectUri, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityClientRedirectUri(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityClientRedirectUri(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityClientRedirectUri>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_redirect_uris");

            entity.HasIndex(e => e.ClientId, "idx_identity_client_redirect_uris_client_id");

            entity.Property(e => e.ClientId)
                .HasMaxLength(36) // Limiting the size of the ClientId column
                .HasColumnName("client_id")
                .IsRequired();

            entity.Property(e => e.RedirectUri)
                .HasColumnType("text") // PostgreSQL uses "text" to represent long text values
                .HasColumnName("redirect_uri")
                .IsRequired();

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_redirect_uris_fk_client");
        });
    }
}