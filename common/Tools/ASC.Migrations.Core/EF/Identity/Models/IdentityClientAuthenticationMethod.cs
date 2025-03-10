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

    public static void MySqlAddIdentityClientAuthenticationMethod(this ModelBuilder modelBuilder)
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

    public static void PgSqlAddIdentityClientAuthenticationMethod(this ModelBuilder modelBuilder)
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
