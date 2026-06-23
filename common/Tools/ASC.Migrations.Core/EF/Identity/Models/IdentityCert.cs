// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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