// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityShedlock()
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

        public void PgSqlAddIdentityShedlock()
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
}