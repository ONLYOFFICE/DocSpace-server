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

public class IdentityConsent
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public bool? IsInvalidated { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}

public static class IdentityConsentExtension
{
    public static ModelBuilderWrapper AddIdentityConsent(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityConsent, Provider.MySql)
            .Add(PgSqlAddIdentityConsent, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityConsent()
        {
            modelBuilder.Entity<IdentityConsent>(entity =>
            {
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId }).HasName("PRIMARY");

                entity.ToTable("identity_consents");

                entity.Property(e => e.PrincipalId)
                    .HasMaxLength(255)
                    .HasColumnName("principal_id");
                entity.Property(e => e.RegisteredClientId)
                    .HasMaxLength(36)
                    .HasColumnName("registered_client_id");
                entity.Property(e => e.IsInvalidated)
                    .HasDefaultValueSql("'0'")
                    .HasColumnName("is_invalidated");
                entity.Property(e => e.ModifiedAt)
                    .HasMaxLength(6)
                    .HasColumnName("modified_at");
            });
        }

        public void PgSqlAddIdentityConsent()
        {
            modelBuilder.Entity<IdentityConsent>(entity =>
            {
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId })
                    .HasName("pk_identity_consents"); // PostgreSQL prefers prefix "pk" for primary keys instead of "PRIMARY"

                entity.ToTable("identity_consents");

                entity.Property(e => e.PrincipalId)
                    .HasMaxLength(255)
                    .HasColumnName("principal_id");

                entity.Property(e => e.RegisteredClientId)
                    .HasMaxLength(36)
                    .HasColumnName("registered_client_id");

                entity.Property(e => e.IsInvalidated)
                    .HasDefaultValueSql("false") // PostgreSQL uses "false" for boolean false
                    .HasColumnName("is_invalidated");

                entity.Property(e => e.ModifiedAt)
                    .HasColumnType("timestamp with time zone") // PostgreSQL equivalent for a timestamp column
                    .HasColumnName("modified_at");
            });
        }
    }
}