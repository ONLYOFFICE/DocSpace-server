namespace ASC.Migrations.Core.Identity;

public class IdentityCert
{
    public string Id { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public sbyte PairType { get; set; }

    public string PrivateKey { get; set; } = null!;

    public string PublicKey { get; set; } = null!;
}

public static class IdentityCertExtension
{
    public static ModelBuilderWrapper AddIdentityCert(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityCert, Provider.MySql)
            .Add(PgSqlAddIdentityCert, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityCert(this ModelBuilder modelBuilder)
    {
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
    }

    public static void PgSqlAddIdentityCert(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityCert>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("identity_certs");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(36);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");
            
            entity.Property(e => e.PairType)
                .HasColumnName("pair_type");
            
            entity.Property(e => e.PrivateKey)
                .HasColumnType("text")
                .HasColumnName("private_key")
                .IsRequired();
            
            entity.Property(e => e.PublicKey)
                .HasColumnType("text")
                .HasColumnName("public_key")
                .IsRequired();
        });
    }
}