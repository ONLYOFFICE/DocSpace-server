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

namespace ASC.Migration.Core.Models.Api;

[ProtoContract]
public class MigrationApiInfo
{
    [SwaggerSchemaCustomString("Migrator name")]
    [ProtoMember(1)]
    public string MigratorName { get; set; }

    [SwaggerSchemaCustomString("Operation")]
    [ProtoMember(2)]
    public string Operation { get; set; }

    [SwaggerSchemaCustomString("Failed archives")]
    [ProtoMember(3)]
    public List<string> FailedArchives { get; set; } = new List<string>();

    [SwaggerSchemaCustom<List<MigratingApiUser>>("Users")]
    [ProtoMember(4)]
    public List<MigratingApiUser> Users { get; set; } = new List<MigratingApiUser>();

    [SwaggerSchemaCustom<List<MigratingApiUser>>("Without email users")]
    [ProtoMember(5)]
    public List<MigratingApiUser> WithoutEmailUsers { get; set; } = new List<MigratingApiUser>();

    [SwaggerSchemaCustom<List<MigratingApiUser>>("Exist users")]
    [ProtoMember(6)]
    public List<MigratingApiUser> ExistUsers { get; set; } = new List<MigratingApiUser>();

    [SwaggerSchemaCustom<List<MigratingApiUser>>("Groups")]
    [ProtoMember(7)]
    public List<MigratingApiGroup> Groups { get; set; } = new List<MigratingApiGroup>();

    [SwaggerSchemaCustomBoolean("Import personal files")]
    [ProtoMember(8)]
    public bool ImportPersonalFiles { get; set; }

    [SwaggerSchemaCustomBoolean("Import shared files")]
    [ProtoMember(9)]
    public bool ImportSharedFiles { get; set; }

    [SwaggerSchemaCustomBoolean("Import shared folders")]
    [ProtoMember(10)]
    public bool ImportSharedFolders { get; set; }

    [SwaggerSchemaCustomBoolean("Import common files")]
    [ProtoMember(11)]
    public bool ImportCommonFiles { get; set; }

    [SwaggerSchemaCustomBoolean("Import project files")]
    [ProtoMember(12)]
    public bool ImportProjectFiles { get; set; }

    [SwaggerSchemaCustomBoolean("Import groups")]
    [ProtoMember(13)]
    public bool ImportGroups { get; set; }

    [SwaggerSchemaCustomInt("Successed users", Format = "int32")]
    [ProtoMember(14)]
    public int SuccessedUsers { get; set; }

    [SwaggerSchemaCustomInt("Failed users", Format = "int32")]
    [ProtoMember(15)]
    public int FailedUsers { get; set; }

    [SwaggerSchemaCustomString("Files")]
    [ProtoMember(16)]
    public List<string> Files { get; set; }

    [SwaggerSchemaCustomString("Errors")]
    [ProtoMember(17)]
    public List<string> Errors { get; set; }
}
