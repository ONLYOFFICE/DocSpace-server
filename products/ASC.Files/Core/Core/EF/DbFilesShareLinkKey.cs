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

namespace ASC.Files.Core.EF;

/// <summary>
/// The key material of an external link of an item in a private room: the public half of the link's key pair,
/// the private half sealed under the link's secret, and the hash of the proof a visitor shows. The server can open
/// none of it. A wrap of a file key made for the link is a <see cref="DbFileKeys"/> row with the link id in
/// <see cref="DbFileKeys.UserId"/>.
/// </summary>
public class DbFilesShareLinkKey : BaseEntity
{
    public int TenantId { get; set; }

    public Guid LinkId { get; set; }

    /// <summary>The public X25519 key of the link, 32 bytes in base64.</summary>
    public string PublicKey { get; set; }

    /// <summary>The private key of the link sealed by the creator's browser, as the client sent it.</summary>
    public string PrivateKeyEnc { get; set; }

    /// <summary>SHA-256 of the proof value, 32 bytes in base64.</summary>
    public string ProofHash { get; set; }

    public DateTime CreateOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, LinkId];
    }
}

/// <summary>
/// The secret of an external link of a private item, wrapped for one user who manages the room's links.
/// </summary>
public class DbFilesShareLinkSecretWrap : BaseEntity
{
    public int TenantId { get; set; }

    public Guid LinkId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The id of the user's public key the secret was wrapped for.</summary>
    public Guid PublicKeyId { get; set; }

    public string Wrap { get; set; }

    public DateTime CreateOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, LinkId, UserId];
    }
}

public static class DbFilesShareLinkKeyExtension
{
    public static ModelBuilderWrapper AddDbFilesShareLinkKeys(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesShareLinkKey>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbFilesShareLinkSecretWrap>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesShareLinkKeys, Provider.MySql)
            .Add(PgSqlAddDbFilesShareLinkKeys, Provider.PostgreSql);

        return modelBuilder;
    }

    private static void MySqlAddDbFilesShareLinkKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesShareLinkKey>(entity =>
        {
            entity.ToTable("files_share_link_keys")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.LinkId })
                .HasName("PRIMARY");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.LinkId)
                .HasColumnName("link_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PublicKey)
                .IsRequired()
                .HasColumnName("public_key")
                .HasColumnType("varchar(64)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PrivateKeyEnc)
                .IsRequired()
                .HasColumnName("private_key_enc")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ProofHash)
                .IsRequired()
                .HasColumnName("proof_hash")
                .HasColumnType("varchar(64)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("datetime");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbFilesShareLinkSecretWrap>(entity =>
        {
            entity.ToTable("files_share_link_secret_wraps")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.LinkId, e.UserId })
                .HasName("PRIMARY");

            entity.HasIndex(e => new { e.TenantId, e.UserId })
                .HasDatabaseName("tenant_id_user_id");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.LinkId)
                .HasColumnName("link_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PublicKeyId)
                .HasColumnName("public_key_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Wrap)
                .IsRequired()
                .HasColumnName("wrap")
                .HasColumnType("text")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("datetime");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void PgSqlAddDbFilesShareLinkKeys(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesShareLinkKey>(entity =>
        {
            entity.ToTable("files_share_link_keys");

            entity.HasKey(e => new { e.TenantId, e.LinkId })
                .HasName("files_share_link_keys_pkey");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.LinkId)
                .HasColumnName("link_id")
                .HasMaxLength(36);

            entity.Property(e => e.PublicKey)
                .IsRequired()
                .HasColumnName("public_key")
                .HasMaxLength(64);

            entity.Property(e => e.PrivateKeyEnc)
                .IsRequired()
                .HasColumnName("private_key_enc");

            entity.Property(e => e.ProofHash)
                .IsRequired()
                .HasColumnName("proof_hash")
                .HasMaxLength(64);

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbFilesShareLinkSecretWrap>(entity =>
        {
            entity.ToTable("files_share_link_secret_wraps");

            entity.HasKey(e => new { e.TenantId, e.LinkId, e.UserId })
                .HasName("files_share_link_secret_wraps_pkey");

            entity.HasIndex(e => new { e.TenantId, e.UserId })
                .HasDatabaseName("files_share_link_secret_wraps_tenant_id_user_id");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.LinkId)
                .HasColumnName("link_id")
                .HasMaxLength(36);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(36);

            entity.Property(e => e.PublicKeyId)
                .HasColumnName("public_key_id")
                .HasMaxLength(36);

            entity.Property(e => e.Wrap)
                .IsRequired()
                .HasColumnName("wrap");

            entity.Property(e => e.CreateOn)
                .HasColumnName("create_on")
                .HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
