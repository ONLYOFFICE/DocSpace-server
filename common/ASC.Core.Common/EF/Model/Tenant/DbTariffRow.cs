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

namespace ASC.Core.Common.EF;

public class DbTariffRow : BaseEntity
{
    public int TariffId { get; set; }
    public int Quota { get; set; }
    public int Quantity { get; set; }
    public int TenantId { get; set; }
    public DateTime? DueDate { get; set; }
    public int? NextQuantity { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, TariffId, Quota];
    }
}
public static class DbTariffRowExtension
{
    public static ModelBuilderWrapper AddDbTariffRow(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbTariffRow>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbTariffRow, Provider.MySql)
            .Add(PgSqlAddDbTariffRow, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbTariffRow()
        {
            modelBuilder.Entity<DbTariffRow>(entity =>
            {
                entity.ToTable("tenants_tariffrow");

                entity.HasKey(e => new { e.TenantId, e.TariffId, e.Quota })
                    .HasName("PRIMARY");

                entity.Property(e => e.TariffId)
                    .HasColumnName("tariff_id")
                    .HasColumnType("int");

                entity.Property(e => e.Quota)
                    .HasColumnName("quota")
                    .HasColumnType("int");

                entity.Property(e => e.Quantity)
                    .HasColumnName("quantity")
                    .HasColumnType("int");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant")
                    .HasColumnType("int");

                entity.Property(e => e.DueDate)
                    .HasColumnName("due_date")
                    .HasColumnType("datetime")
                    .IsRequired(false)
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.NextQuantity)
                    .HasColumnName("next_quantity")
                    .HasColumnType("int")
                    .IsRequired(false)
                    .HasDefaultValueSql("NULL");
            });
        }

        public void PgSqlAddDbTariffRow()
        {
            modelBuilder.Entity<DbTariffRow>(entity =>
            {
                entity.ToTable("tenants_tariffrow");

                entity.HasKey(e => new { e.TenantId, e.TariffId, e.Quota })
                    .HasName("tenants_tariffrow_pkey");

                entity.Property(e => e.TariffId)
                    .HasColumnName("tariff_id")
                    .HasColumnType("integer");

                entity.Property(e => e.Quota)
                    .HasColumnName("quota")
                    .HasColumnType("integer");

                entity.Property(e => e.Quantity)
                    .HasColumnName("quantity")
                    .HasColumnType("integer");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant")
                    .HasColumnType("integer");

                entity.Property(e => e.DueDate)
                    .HasColumnName("due_date")
                    .HasColumnType("timestamptz")
                    .IsRequired(false)
                    .HasDefaultValue(null);

                entity.Property(e => e.NextQuantity)
                    .HasColumnName("next_quantity")
                    .HasColumnType("integer")
                    .IsRequired(false)
                    .HasDefaultValue(null);
            });
        }
    }
}