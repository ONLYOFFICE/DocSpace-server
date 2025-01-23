namespace ASC.Migrations.Core.Identity;

public class IdentityShedlock
{
    public string Name { get; set; } = null!;

    public DateTime LockUntil { get; set; }

    public DateTime LockedAt { get; set; }

    public string LockedBy { get; set; } = null!;
}

public static class IdentityShedlockExtension
{
    public static ModelBuilderWrapper AddIdentityShedlock(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityShedlock, Provider.MySql)
            .Add(PgSqlAddIdentityShedlock, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    public static void MySqlAddIdentityShedlock(this ModelBuilder modelBuilder)
    {
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

    public static void PgSqlAddIdentityShedlock(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityShedlock>(entity =>
        {
            // Setting primary key with "name" column
            entity.HasKey(e => e.Name).HasName("identity_shedlock_pkey");

            // Mapping this entity to the PostgreSQL table
            entity.ToTable("identity_shedlock");

            // Configuring the properties
            entity.Property(e => e.Name)
                .HasMaxLength(64) // Restricting the length
                .HasColumnName("name");

            entity.Property(e => e.LockUntil)
                .HasColumnType("timestamptz") // PostgreSQL specific timestamp type
                .HasColumnName("lock_until");

            entity.Property(e => e.LockedAt)
                .HasColumnType("timestamptz") // PostgreSQL specific timestamp type
                .HasColumnName("locked_at");

            entity.Property(e => e.LockedBy)
                .HasMaxLength(255) // Restricting the length
                .HasColumnName("locked_by")
                .IsRequired(); // Configuring the column as NOT NULL
        });
    }
}