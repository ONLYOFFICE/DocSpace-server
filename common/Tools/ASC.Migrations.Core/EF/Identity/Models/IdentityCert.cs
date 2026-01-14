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

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityCert()
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

        public void PgSqlAddIdentityCert()
        {
            modelBuilder.Entity<IdentityCert>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_identity_certs");

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
}