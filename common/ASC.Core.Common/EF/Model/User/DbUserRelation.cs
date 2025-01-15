// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Core.Common.EF;

public class DbUserRelation : BaseEntity
{
    public int TenantId { get; set; }
    public Guid SourceUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public DbTenant Tenant { get; set; }
    
    public override object[] GetKeys()
    {
        return [TenantId, SourceUserId, TargetUserId];
    }
}

public static class UserRelationExtension
{
    public static ModelBuilderWrapper AddUserRelation(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbUserRelation>()
            .Navigation(e => e.Tenant)
            .AutoInclude(false);

        modelBuilder
            .Add(MySqlAddUserRelation, Provider.MySql)
            .Add(PgSqlAddUserRelation, Provider.PostgreSql);

        return modelBuilder;
    }
    
    private static void MySqlAddUserRelation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUserRelation>(builder =>
        {
            builder.ToTable("core_user_relations");
            
            builder.HasKey(e => new { e.TenantId, e.SourceUserId, e.TargetUserId })
                .HasName("PRIMARY");

            builder.Property(e => e.TenantId)
                .HasColumnName("tenant");
            
            builder.Property(e => e.SourceUserId)
                .HasColumnName("source_user_id")
                .HasColumnType("varchar(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            builder.Property(e => e.TargetUserId)
                .HasColumnName("target_user_id")
                .HasColumnType("varchar(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
        });
    }

    private static void PgSqlAddUserRelation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUserRelation>(builder =>
        {
            builder.ToTable("core_user_relations");

            builder.HasKey(e => new { e.TenantId, e.SourceUserId, e.TargetUserId })
                .HasName("PK_CoreUserRelations");

            builder.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            builder.Property(e => e.SourceUserId)
                .HasColumnName("source_user_id")
                .HasColumnType("uuid");

            builder.Property(e => e.TargetUserId)
                .HasColumnName("target_user_id")
                .HasColumnType("uuid");
        });
    }
}