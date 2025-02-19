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

using ASC.Api.Core.Extensions;

namespace ASC.Migration.Core.Models.Api;

[ProtoContract]
public class MigrationApiInfo
{
    /// <summary>
    /// Migrator name
    /// </summary>
    [ProtoMember(1)]
    [OpenApiDescription("Migrator name")]
    public string MigratorName { get; set; }

    /// <summary>
    /// Operation
    /// </summary>
    [ProtoMember(2)]
    [OpenApiDescription("Operation")]
    public string Operation { get; set; }

    /// <summary>
    /// Failed archives
    /// </summary>
    [ProtoMember(3)]
    [OpenApiDescription("Failed archives")]
    public List<string> FailedArchives { get; set; } = [];

    /// <summary>
    /// Users
    /// </summary>
    [ProtoMember(4)]
    [OpenApiDescription("Users")]
    public List<MigratingApiUser> Users { get; set; } = [];

    /// <summary>
    /// Without email users
    /// </summary>
    [ProtoMember(5)]
    [OpenApiDescription("Without email users")]
    public List<MigratingApiUser> WithoutEmailUsers { get; set; } = [];

    /// <summary>
    /// Exist users
    /// </summary>
    [ProtoMember(6)]
    [OpenApiDescription("Exist users")]
    public List<MigratingApiUser> ExistUsers { get; set; } = [];

    /// <summary>
    /// Groups
    /// </summary>
    [ProtoMember(7)]
    [OpenApiDescription("Groups")]
    public List<MigratingApiGroup> Groups { get; set; } = [];

    /// <summary>
    /// Import personal files
    /// </summary>
    [ProtoMember(8)]
    [OpenApiDescription("Import personal files")]
    public bool ImportPersonalFiles { get; set; }

    /// <summary>
    /// Import shared files
    /// </summary>
    [ProtoMember(9)]
    [OpenApiDescription("Import shared files")]
    public bool ImportSharedFiles { get; set; }

    /// <summary>
    /// Import shared folders
    /// </summary>
    [ProtoMember(10)]
    [OpenApiDescription("Import shared folders")]
    public bool ImportSharedFolders { get; set; }

    /// <summary>
    /// Import common files
    /// </summary>
    [ProtoMember(11)]
    [OpenApiDescription("Import common files")]
    public bool ImportCommonFiles { get; set; }

    /// <summary>
    /// Import project files
    /// </summary>
    [ProtoMember(12)]
    [OpenApiDescription("Import project files")]
    public bool ImportProjectFiles { get; set; }

    /// <summary>
    /// Import groups
    /// </summary>
    [ProtoMember(13)]
    [OpenApiDescription("Import groups")]
    public bool ImportGroups { get; set; }

    /// <summary>
    /// Successed users
    /// </summary>
    [ProtoMember(14)]
    [OpenApiDescription("Successed users")]
    public int SuccessedUsers { get; set; }

    /// <summary>
    /// Failed users
    /// </summary>
    [ProtoMember(15)]
    [OpenApiDescription("Failed users")]
    public int FailedUsers { get; set; }

    /// <summary>
    /// Files
    /// </summary>
    [ProtoMember(16)]
    [OpenApiDescription("Files")]
    public List<string> Files { get; set; }

    /// <summary>
    /// Errors
    /// </summary>
    [ProtoMember(17)]
    [OpenApiDescription("Errors")]
    public List<string> Errors { get; set; }
}
