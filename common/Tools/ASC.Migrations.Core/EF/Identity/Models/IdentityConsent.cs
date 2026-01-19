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