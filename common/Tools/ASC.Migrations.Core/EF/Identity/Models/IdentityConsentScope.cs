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

public class IdentityConsentScope
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public string Scopes { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;

    public virtual IdentityConsent Consent { get; set; } = null!;
}

public static class IdentityConsentScopeExtension
{
    public static ModelBuilderWrapper AddIdentityConsentScope(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityConsentScope, Provider.MySql)
            .Add(PgSqlAddIdentityConsentScope, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityConsentScope()
        {
            modelBuilder.Entity<IdentityConsentScope>(entity =>
            {
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId, e.Scopes }).HasName("PRIMARY");

                entity.ToTable("identity_consent_scopes");

                entity.HasIndex(e => e.RegisteredClientId, "idx_identity_consent_scopes_registered_client_id");

                entity.HasIndex(e => e.PrincipalId, "idx_identity_consent_scopes_principal_id");

                entity.HasIndex(e => e.Scopes, "idx_identity_consent_scopes_scopes");


                entity.Property(e => e.PrincipalId)
                    .HasColumnName("principal_id")
                    .HasMaxLength(255);

                entity.Property(e => e.RegisteredClientId)
                    .HasMaxLength(36)
                    .HasColumnName("registered_client_id");

                entity.Property(e => e.Scopes)
                    .HasColumnName("scopes");

                entity.HasOne(d => d.Consent)
                    .WithMany(p => p.IdentityConsentScopes)
                    .HasForeignKey(d => new { d.PrincipalId, d.RegisteredClientId })
                    .HasConstraintName("identity_consent_scopes_ibfk_1");


                entity.HasOne(d => d.ScopeNameNavigation)
                    .WithMany(p => p.IdentityConsentScopes)
                    .HasForeignKey(d => d.Scopes)
                    .HasConstraintName("identity_consent_scopes_ibfk_2");
            });
        }

        public void PgSqlAddIdentityConsentScope()
        {
            modelBuilder.Entity<IdentityConsentScope>(entity =>
            {
                // Define composite primary key
                entity.HasKey(e => new { e.PrincipalId, e.RegisteredClientId, e.Scopes }).HasName("pk_identity_consent_scopes");

                entity.ToTable("identity_consent_scopes"); // Map to table

                // Define indexes
                entity.HasIndex(e => e.PrincipalId, "ix_identity_consent_scopes_principal_id");
                entity.HasIndex(e => e.RegisteredClientId, "ix_identity_consent_scopes_registered_client_id");
                entity.HasIndex(e => e.Scopes, "ix_identity_consent_scopes_scopes");

                // Define columns
                entity.Property(e => e.PrincipalId)
                    .HasColumnName("principal_id")
                    .HasMaxLength(255);

                entity.Property(e => e.RegisteredClientId)
                    .HasMaxLength(36)
                    .HasColumnName("registered_client_id");

                entity.Property(e => e.Scopes)
                    .HasColumnName("scopes");

                // Define foreign key relations
                entity.HasOne(d => d.Consent)
                    .WithMany(p => p.IdentityConsentScopes)
                    .HasForeignKey(d => new { d.PrincipalId, d.RegisteredClientId })
                    .HasConstraintName("fk_identity_consent_scopes_principal_client");

                entity.HasOne(d => d.ScopeNameNavigation)
                    .WithMany(p => p.IdentityConsentScopes)
                    .HasForeignKey(d => d.Scopes)
                    .HasConstraintName("fk_identity_consent_scopes_scopes");
            });
        }
    }
}