// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Common.EF.Model;

/// <summary>
/// The database tenant parameters.
/// </summary>
public class DbTenant
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    [MaxLength(255)]
    public string Name { get; set; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    [MaxLength(100)]
    public string Alias { get; set; }

    /// <summary>
    /// Mapped domain
    /// </summary>
    [MaxLength(100)]
    public string MappedDomain { get; set; }

    /// <summary>
    /// The tenant version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The Version_changed field.
    /// </summary>
    public DateTime? Version_Changed { get; set; }

    /// <summary>
    /// The date and time when the version was changed.
    /// </summary>
    public DateTime VersionChanged
    {
        get => Version_Changed ?? DateTime.MinValue;
        set => Version_Changed = value;
    }

    /// <summary>
    /// The tenant language.
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; }

    /// <summary>
    /// The tenant time zone.
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; }

    /// <summary>
    /// The tenant trusted domains raw.
    /// </summary>
    [MaxLength(1024)]
    public string TrustedDomainsRaw { get; set; }

    /// <summary>
    /// The type of the tenant trusted domains.
    /// </summary>
    public TenantTrustedDomainsType TrustedDomainsEnabled { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    public TenantStatus Status { get; set; }

    /// <summary>
    /// The date and time when the tenant status was changed.
    /// </summary>
    public DateTime? StatusChanged { get; set; }
    //hack for DateTime?

    /// <summary>
    /// The hacked date and time when the tenant status was changed.
    /// </summary>
    public DateTime StatusChangedHack
    {
        get => StatusChanged ?? DateTime.MinValue;
        set { StatusChanged = value; }
    }

    /// <summary>
    /// The tenant creation date.
    /// </summary>
    public DateTime CreationDateTime { get; set; }

    /// <summary>
    /// The tenant owner ID.
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// The tenant payment ID.
    /// </summary>
    [MaxLength(38)]
    public string PaymentId { get; set; }

    /// <summary>
    /// The tenant industry.
    /// </summary>
    public TenantIndustry Industry { get; set; }

    /// <summary>
    /// The date and time when the tenant was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Specifies if the calls are available for the current tenant or not.
    /// </summary>
    public bool Calls { get; set; }

    /// <summary>
    /// The database tenant partner parameters.
    /// </summary>
    public DbTenantPartner Partner { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class DbTenantMapper
{
    [MapPropertyFromSource(nameof(Tenant.TrustedDomainsRaw), Use = nameof(GetTrustedDomains))]
    [MapValue(nameof(Tenant.LastModified), Use = nameof(MapLastModified))]
    [MapProperty(nameof(Tenant.TrustedDomainsType), nameof(DbTenant.TrustedDomainsEnabled))]
    [MapProperty(nameof(Tenant.Alias), nameof(DbTenant.Alias), Use = nameof(MapAlias))]
    [MapProperty(nameof(Tenant.Name), nameof(DbTenant.Name), Use = nameof(MapName))]
    [MapProperty(nameof(Tenant.MappedDomain), nameof(DbTenant.MappedDomain), Use = nameof(MapMappedDomain))]
    [MapProperty(nameof(Tenant.Id), [nameof(DbTenant.Partner), nameof(DbTenantPartner.TenantId)])]
    [MapProperty(nameof(Tenant.AffiliateId), [nameof(DbTenant.Partner), nameof(DbTenantPartner.AffiliateId)])]
    [MapProperty(nameof(Tenant.PartnerId), [nameof(DbTenant.Partner), nameof(DbTenantPartner.PartnerId)])]
    [MapProperty(nameof(Tenant.Campaign), [nameof(DbTenant.Partner), nameof(DbTenantPartner.Campaign)])]
    public static partial DbTenant Map(this Tenant source);

    [UserMapping(Default = false)]
    public static string MapAlias(string alias)
    {
        return alias.ToLower();
    }

    public static DateTime MapLastModified()
    {
        return DateTime.UtcNow;
    }

    [UserMapping(Default = false)]
    public static string GetTrustedDomains(Tenant source)
    {
        return source.GetTrustedDomains();
    }

    [UserMapping(Default = false)]
    public static string MapName(string name)
    {
        return name ?? "";
    }

    [UserMapping(Default = false)]
    public static string MapMappedDomain(string mappedDomain)
    {
        return !string.IsNullOrEmpty(mappedDomain) ? mappedDomain.ToLowerInvariant() : null;
    }
}

public static class DbTenantExtension
{
    public static ModelBuilderWrapper AddDbTenant(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbTenant>().Navigation(e => e.Partner).AutoInclude();

        modelBuilder
            .AddDbTenantPartner()
            .Add(MySqlAddDbTenant, Provider.MySql)
            .Add(PgSqlAddDbTenant, Provider.PostgreSql)
            .HasData(
            new DbTenant
            {
                Id = 1,
                Alias = "localhost",
                Name = "Web Office",
                CreationDateTime = new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317),
                OwnerId = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef"),
                LastModified = new DateTime(2022, 7, 8, 0, 0, 0, DateTimeKind.Utc)
            }
            )
            .HasData(
            new DbTenant
            {
                Id = -1,
                Alias = "settings",
                Name = "Web Office",
                CreationDateTime = new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317),
                OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                LastModified = new DateTime(2022, 7, 8, 0, 0, 0, DateTimeKind.Utc),
                Status = TenantStatus.Suspended
            });

        return modelBuilder;
    }

    public static void MySqlAddDbTenant(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTenant>()
            .HasOne(r => r.Partner)
            .WithOne(r => r.Tenant)
            .HasPrincipalKey<DbTenant>(r => new { r.Id });

        modelBuilder.Entity<DbTenant>(entity =>
        {
            entity.ToTable("tenants_tenants")
                .HasCharSet("utf8");

            entity.HasIndex(e => e.LastModified)
                .HasDatabaseName("last_modified");

            entity.HasIndex(e => e.MappedDomain)
                .HasDatabaseName("mappeddomain");

            entity.HasIndex(e => e.Version)
                .HasDatabaseName("version");

            entity.HasIndex(e => e.Alias)
                .HasDatabaseName("alias")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Alias)
                .IsRequired()
                .HasColumnName("alias")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Calls)
                .HasColumnName("calls")
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(1)");

            entity.Property(e => e.CreationDateTime)
                .HasColumnName("creationdatetime")
                .HasColumnType("datetime");

            entity.Property(e => e.Industry)
                .HasColumnName("industry")
                .IsRequired()
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.Language)
                .IsRequired()
                .HasColumnName("language")
                .HasColumnType("char")
                .HasDefaultValueSql("'en-US'")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified")
                .HasColumnType("timestamp");

            entity.Property(e => e.MappedDomain)
                .HasColumnName("mappeddomain")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.OwnerId)
                .HasColumnName("owner_id")
                .HasColumnType("varchar(38)")
                .IsRequired(false)
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PaymentId)
                .HasColumnName("payment_id")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.StatusChanged)
                .HasColumnName("statuschanged")
                .HasColumnType("datetime");

            entity.Property(e => e.TimeZone)
                .HasColumnName("timezone")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.TrustedDomainsRaw)
                .HasColumnName("trusteddomains")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.TrustedDomainsEnabled)
                .HasColumnName("trusteddomainsenabled")
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasDefaultValueSql("'2'");

            entity.Property(e => e.Version_Changed)
                .HasColumnName("version_changed")
                .HasColumnType("datetime");

            entity.Ignore(c => c.StatusChangedHack);
            entity.Ignore(c => c.VersionChanged);
        });
    }

    public static void PgSqlAddDbTenant(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTenant>(entity =>
        {
            entity.ToTable("tenants_tenants");

            entity.HasIndex(e => e.LastModified)
                .HasDatabaseName("IX_tenants_tenants_last_modified");

            entity.HasIndex(e => e.MappedDomain)
                .HasDatabaseName("mappeddomain");

            entity.HasIndex(e => e.Version)
                .HasDatabaseName("version");

            entity.HasIndex(e => e.Alias)
                .HasDatabaseName("alias")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Alias)
                .IsRequired()
                .HasColumnName("alias")
                .HasColumnType("varchar");

            entity.Property(e => e.Calls)
                .HasColumnName("calls")
                .HasDefaultValueSql("true")
                .HasColumnType("boolean");

            entity.Property(e => e.CreationDateTime)
                .HasColumnName("creationdatetime")
                .HasColumnType("timestamptz");

            entity.Property(e => e.Industry)
                .HasColumnName("industry")
                .IsRequired()
                .HasDefaultValueSql("0");

            entity.Property(e => e.Language)
                .IsRequired()
                .HasColumnName("language")
                .HasColumnType("char")
                .HasDefaultValueSql("'en-US'");

            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified")
                .HasColumnType("timestamptz");

            entity.Property(e => e.MappedDomain)
                .HasColumnName("mappeddomain")
                .HasColumnType("varchar");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar");

            entity.Property(e => e.OwnerId)
                .HasColumnName("owner_id")
                .HasColumnType("uuid")
                .IsRequired(false);

            entity.Property(e => e.PaymentId)
                .HasColumnName("payment_id")
                .HasColumnType("varchar");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValueSql("0");

            entity.Property(e => e.StatusChanged)
                .HasColumnName("statuschanged")
                .HasColumnType("timestamptz");

            entity.Property(e => e.TimeZone)
                .HasColumnName("timezone")
                .HasColumnType("varchar");

            entity.Property(e => e.TrustedDomainsRaw)
                .HasColumnName("trusteddomains")
                .HasColumnType("varchar");

            entity.Property(e => e.TrustedDomainsEnabled)
                .HasColumnName("trusteddomainsenabled")
                .HasDefaultValueSql("0");

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasDefaultValueSql("2");

            entity.Property(e => e.Version_Changed)
                .HasColumnName("version_changed")
                .HasColumnType("timestamptz");

            entity.Ignore(c => c.StatusChangedHack);
            entity.Ignore(c => c.VersionChanged);

            entity.HasOne(r => r.Partner)
                .WithOne(r => r.Tenant)
                .HasPrincipalKey<DbTenant>(r => new { r.Id });
        });
    }
}