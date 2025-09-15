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

namespace ASC.Files.Core.EF;

public class DbFilesGroup : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    [MaxLength(128)]
    public string Name { get; set; }
    [MaxLength(50)]
    public string Icon { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbFilesGroupExtension
{
    public static ModelBuilderWrapper AddDbFilesGroup(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesGroup>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesGroup, Provider.MySql)
            .Add(PgSqlAddDbFilesGroup, Provider.PostgreSql);

        return modelBuilder;
    }

    private static void MySqlAddDbFilesGroup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesGroup>(entity =>
        {
            entity.ToTable("files_group")
                .HasCharSet("utf8");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Icon)
                .HasColumnName("icon")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
        });
    }

    private static void PgSqlAddDbFilesGroup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesGroup>(entity =>
        {
            entity.ToTable("files_group");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("integer");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant")
                .HasColumnType("integer");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar(128)");

            entity.Property(e => e.Icon)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar(50)");

        });
    }
}