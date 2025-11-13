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

public class InvitationLink : BaseEntity
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }

    [EnumDataType(typeof(EmployeeType))]
    public EmployeeType EmployeeType { get; set; }

    public DateTime Expiration { get; set; }

    public int MaxUseCount { get; set; }

    public int CurrentUseCount { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class InvitationLinkExtension
{
    public static ModelBuilderWrapper AddInvitationLink(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<InvitationLink>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder
            .Add(MySqlAddInvitationLink, Provider.MySql)
            .Add(PgSqlAddInvitationLink, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddInvitationLink(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvitationLink>(entity =>
        {
            entity.ToTable("invitation_link")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("tenant_id");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasColumnName("tenant_id")
                .HasColumnType("int(10)");

            entity.Property(e => e.EmployeeType)
                .IsRequired()
                .HasColumnName("employee_type")
                .HasColumnType("int(10)");

            entity.Property(e => e.Expiration)
                .IsRequired()
                .HasColumnName("expiration")
                .HasColumnType("datetime");

            entity.Property(e => e.MaxUseCount)
                .IsRequired()
                .HasColumnName("max_use_count")
                .HasColumnType("int(10)");

            entity.Property(e => e.CurrentUseCount)
                .IsRequired()
                .HasColumnName("current_use_count")
                .HasColumnType("int(10)");
        });
    }

    public static void PgSqlAddInvitationLink(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvitationLink>(entity =>
        {
            entity.ToTable("invitation_link");

            entity.HasKey(e => e.Id)
                .HasName("PK_invitation_link");

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_invitation_link_tenant_id");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.EmployeeType)
                .IsRequired()
                .HasColumnName("employee_type")
                .HasColumnType("integer");

            entity.Property(e => e.Expiration)
                .IsRequired()
                .HasColumnName("expiration")
                .HasColumnType("timestamptz");

            entity.Property(e => e.MaxUseCount)
                .IsRequired()
                .HasColumnName("max_use_count")
                .HasColumnType("integer");

            entity.Property(e => e.CurrentUseCount)
                .IsRequired()
                .HasColumnName("current_use_count")
                .HasColumnType("integer");
        });
    }
}