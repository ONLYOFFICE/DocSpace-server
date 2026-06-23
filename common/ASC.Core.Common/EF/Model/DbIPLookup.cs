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

namespace ASC.Core.Common.EF.Model;

public class DbIPLookup
{
    public string AddrType { get; set; } //ipv4, ipv6
    public byte[] IPStart { get; set; }
    public byte[] IPEnd { get; set; }
    [MaxLength(2)]
    public string Continent { get; set; }
    [MaxLength(2)]
    public string Country { get; set; }
    [MaxLength(15)]
    public string StateProvCode { get; set; }
    [MaxLength(80)]
    public string StateProv { get; set; }
    [MaxLength(80)]
    public string District { get; set; }
    [MaxLength(80)]
    public string City { get; set; }
    [MaxLength(20)]
    public string ZipCode { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public int? GeonameId { get; set; }
    public float TimezoneOffset { get; set; }
    [MaxLength(64)]
    public string TimezoneName { get; set; }
    [MaxLength(10)]
    public string WeatherCode { get; set; }

}

public static class DbIPLookupExtension
{
    public static ModelBuilderWrapper AddDbIPLookup(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddDbIPLookup, Provider.MySql)
            .Add(PgSqlAddDbIPLookup, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbIPLookup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbIPLookup>(entity =>
        {
            entity.ToTable("dbip_lookup")
                 .HasCharSet("utf8mb4");

            entity.HasKey(nameof(DbIPLookup.AddrType), nameof(DbIPLookup.IPStart));

            entity.Property(e => e.AddrType)
                .IsRequired()
                .HasColumnName("addr_type")
                .HasColumnType("enum('ipv4','ipv6')");

            entity.Property(e => e.IPStart)
                .IsRequired()
                .HasColumnName("ip_start")
                .HasColumnType("varbinary(16)");

            entity.Property(e => e.IPEnd)
                .IsRequired()
                .HasColumnName("ip_end")
                .HasColumnType("varbinary(16)");

            entity.Property(e => e.Continent)
                .IsRequired()
                .HasColumnName("continent")
                .HasColumnType("char");

            entity.Property(e => e.Country)
                .IsRequired()
                .HasColumnName("country")
                .HasColumnType("char");

            entity.Property(e => e.StateProvCode)
                .HasColumnName("stateprov_code")
                .HasColumnType("varchar");

            entity.Property(e => e.StateProv)
                .IsRequired()
                .HasColumnName("stateprov")
                .HasColumnType("varchar");

            entity.Property(e => e.District)
                .IsRequired()
                .HasColumnName("district")
                .HasColumnType("varchar");


            entity.Property(e => e.City)
                .IsRequired()
                .HasColumnName("city")
                .HasColumnType("varchar");

            entity.Property(e => e.ZipCode)
                .HasColumnName("zipcode")
                .HasColumnType("varchar");

            entity.Property(e => e.Latitude)
                .IsRequired()
                .HasColumnName("latitude")
                .HasColumnType("float");

            entity.Property(e => e.Longitude)
                .IsRequired()
                .HasColumnName("longitude")
                .HasColumnType("float");

            entity.Property(e => e.GeonameId)
               .IsRequired(false)
               .HasColumnName("geoname_id")
               .HasColumnType("int(10)");

            entity.Property(e => e.TimezoneOffset)
                .IsRequired()
                .HasColumnType("float")
                .HasColumnName("timezone_offset");

            entity.Property(e => e.TimezoneName)
                .IsRequired()
                .HasColumnName("timezone_name")
                .HasColumnType("varchar");

            entity.Property(e => e.WeatherCode)
                .IsRequired()
                .HasColumnName("weather_code")
                .HasColumnType("varchar");
        });

    }

    public static void PgSqlAddDbIPLookup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbIPLookup>(entity =>
        {
            entity.ToTable("dbip_lookup");

            entity.HasKey(e => new { e.AddrType, e.IPStart });

            entity.Property(e => e.AddrType)
                  .IsRequired()
                  .HasColumnName("addr_type")
                  .HasColumnType("text");

            entity.Property(e => e.IPStart)
                  .IsRequired()
                  .HasColumnName("ip_start")
                  .HasColumnType("bytea");

            entity.Property(e => e.IPEnd)
                  .IsRequired()
                  .HasColumnName("ip_end")
                  .HasColumnType("bytea");

            entity.Property(e => e.Continent)
                  .IsRequired()
                  .HasColumnName("continent")
                  .HasColumnType("char(2)");

            entity.Property(e => e.Country)
                  .IsRequired()
                  .HasColumnName("country")
                  .HasColumnType("char(2)");

            entity.Property(e => e.StateProvCode)
                  .HasColumnName("stateprov_code")
                  .HasColumnType("varchar(15)");

            entity.Property(e => e.StateProv)
                  .IsRequired()
                  .HasColumnName("stateprov")
                  .HasColumnType("varchar(80)");

            entity.Property(e => e.District)
                  .IsRequired()
                  .HasColumnName("district")
                  .HasColumnType("varchar(80)");

            entity.Property(e => e.City)
                  .IsRequired()
                  .HasColumnName("city")
                  .HasColumnType("varchar(80)");

            entity.Property(e => e.ZipCode)
                  .HasColumnName("zipcode")
                  .HasColumnType("varchar(20)");

            entity.Property(e => e.Latitude)
                  .IsRequired()
                  .HasColumnName("latitude")
                  .HasColumnType("real");

            entity.Property(e => e.Longitude)
                  .IsRequired()
                  .HasColumnName("longitude")
                  .HasColumnType("real");

            entity.Property(e => e.GeonameId)
                  .IsRequired(false)
                  .HasColumnName("geoname_id")
                  .HasColumnType("integer");

            entity.Property(e => e.TimezoneOffset)
                  .IsRequired()
                  .HasColumnName("timezone_offset")
                  .HasColumnType("real");

            entity.Property(e => e.TimezoneName)
                  .IsRequired()
                  .HasColumnName("timezone_name")
                  .HasColumnType("varchar(64)");

            entity.Property(e => e.WeatherCode)
                  .IsRequired()
                  .HasColumnName("weather_code")
                  .HasColumnType("varchar(10)");
        });

    }
}