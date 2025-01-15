namespace ASC.Migrations.Core.Identity;

public class IdentityClientAllowedOrigin
{
    public string ClientId { get; set; } = null!;

    public string AllowedOrigin { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}

public static class IdentityClientAllowedOriginExtension
{
    public static ModelBuilderWrapper AddIdentityClientAllowedOrigin(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClientAllowedOrigin, Provider.MySql)
            .Add(PgSqlAddIdentityClientAllowedOrigin, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityClientAllowedOrigin(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityClientAllowedOrigin(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityClientAllowedOrigin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("identity_client_allowed_origins"); // Map to the same table name as in MySQL.

            entity.HasIndex(e => e.ClientId, "idx_identity_client_allowed_origins_client_id");

            entity.Property(e => e.AllowedOrigin)
                .HasColumnType("text") // PostgreSQL equivalent of MySQL's "tinytext".
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
    }
}
