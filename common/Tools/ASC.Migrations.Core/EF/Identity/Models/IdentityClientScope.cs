namespace ASC.Migrations.Core.Identity;

public class IdentityClientScope
{
    public string ClientId { get; set; } = null!;

    public string ScopeName { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;
}

public static class IdentityClientScopeExtension
{
    public static ModelBuilderWrapper AddIdentityClientScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityClientScope, Provider.MySql)
            .Add(PgSqlAddIdentityClientScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityClientScope(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityClientScope(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityClientScope>(entity =>
        {
            // Define the table for PostgreSQL
            entity
                .HasNoKey() // Indicates no primary key, similar to MySQL variant
                .ToTable("identity_client_scopes");

            // Index for ClientId
            entity.HasIndex(e => e.ClientId).HasDatabaseName("idx_identity_client_scopes_client_id");

            // Index for ScopeName
            entity.HasIndex(e => e.ScopeName).HasDatabaseName("idx_identity_client_scopes_scope_name");

            // Configure ClientId property
            entity.Property(e => e.ClientId)
                .HasMaxLength(36) // Specifies max length of 36
                .HasColumnName("client_id") // Maps the property to the column named "client_id"
                .IsRequired(); // This field is required

            // Configure ScopeName property
            entity.Property(e => e.ScopeName)
                .HasColumnName("scope_name") // Maps the property to the column named "scope_name"
                .IsRequired(); // This field is also required

            // Configure relationships
            // Link 'ClientId' to the related 'IdentityClient' entity
            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("identity_client_scopes_client_id_fk");

            // Link 'ScopeName' to the related 'IdentityScope' entity
            entity.HasOne(d => d.ScopeNameNavigation).WithMany()
                .HasForeignKey(d => d.ScopeName)
                .HasConstraintName("identity_client_scopes_scope_name_fk");
        });
    }
}