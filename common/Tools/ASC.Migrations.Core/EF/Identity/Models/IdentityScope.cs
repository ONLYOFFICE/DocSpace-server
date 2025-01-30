namespace ASC.Migrations.Core.Identity;

public class IdentityScope
{
    public string Name { get; set; } = null!;

    public string Group { get; set; } = null!;

    public string Type { get; set; } = null!;

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}

public static class IdentityScopeExtension
{
    public static ModelBuilderWrapper AddIdentityScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityScope, Provider.MySql)
            .Add(PgSqlAddIdentityScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityScope(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityScope(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityScope>(entity =>
        {
            // Define primary key
            entity.HasKey(e => e.Name).HasName("pk_identity_scopes");

            // Set the table name
            entity.ToTable("identity_scopes");

            // Map columns
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Group)
                .HasMaxLength(255)
                .HasColumnName("group")
                .IsRequired();
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type")
                .IsRequired();

            // Add seed data for PostgreSQL
            entity.HasData(new IdentityScope { Name = "accounts:read", Group = "accounts", Type = "read" });

            entity.HasData(new IdentityScope { Name = "accounts:write", Group = "accounts", Type = "write" });

            entity.HasData(new IdentityScope { Name = "accounts.self:read", Group = "profiles", Type = "read" });

            entity.HasData(new IdentityScope { Name = "accounts.self:write", Group = "profiles", Type = "write" });

            entity.HasData(new IdentityScope { Name = "files:read", Group = "files", Type = "read" });

            entity.HasData(new IdentityScope { Name = "files:write", Group = "files", Type = "write" });

            entity.HasData(new IdentityScope { Name = "openid", Group = "openid", Type = "openid" });

            entity.HasData(new IdentityScope { Name = "rooms:read", Group = "rooms", Type = "read" });

            entity.HasData(new IdentityScope { Name = "rooms:write", Group = "rooms", Type = "write" });
        });
    }
}