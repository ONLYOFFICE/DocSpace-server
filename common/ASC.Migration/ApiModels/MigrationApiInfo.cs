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
    /// <summary>
    /// Migrator name
    /// </summary>
    [ProtoMember(1)]
    public string MigratorName { get; set; }

    /// <summary>
    /// Operation
    /// </summary>
    [ProtoMember(2)]
    public string Operation { get; set; }

    /// <summary>
    /// Failed archives
    /// </summary>
    [ProtoMember(3)]
    public List<string> FailedArchives { get; set; } = new List<string>();

    /// <summary>
    /// Users
    /// </summary>
    [ProtoMember(4)]
    public List<MigratingApiUser> Users { get; set; } = new List<MigratingApiUser>();

    /// <summary>
    /// Without email users
    /// </summary>
    [ProtoMember(5)]
    public List<MigratingApiUser> WithoutEmailUsers { get; set; } = new List<MigratingApiUser>();

    /// <summary>
    /// Exist users
    /// </summary>
    [ProtoMember(6)]
    public List<MigratingApiUser> ExistUsers { get; set; } = new List<MigratingApiUser>();

    /// <summary>
    /// Groups
    /// </summary>
    [ProtoMember(7)]
    public List<MigratingApiGroup> Groups { get; set; } = new List<MigratingApiGroup>();

    /// <summary>
    /// Import personal files
    /// </summary>
    [ProtoMember(8)]
    public bool ImportPersonalFiles { get; set; }

    /// <summary>
    /// Import shared files
    /// </summary>
    [ProtoMember(9)]
    public bool ImportSharedFiles { get; set; }

    /// <summary>
    /// Import shared folders
    /// </summary>
    [ProtoMember(10)]
    public bool ImportSharedFolders { get; set; }

    /// <summary>
    /// Import common files
    /// </summary>
    [ProtoMember(11)]
    public bool ImportCommonFiles { get; set; }

    /// <summary>
    /// Import project files
    /// </summary>
    [ProtoMember(12)]
    public bool ImportProjectFiles { get; set; }

    /// <summary>
    /// Import groups
    /// </summary>
    [ProtoMember(13)]
    public bool ImportGroups { get; set; }

    /// <summary>
    /// Successed users
    /// </summary>
    [ProtoMember(14)]
    public int SuccessedUsers { get; set; }

    /// <summary>
    /// Failed users
    /// </summary>
    [ProtoMember(15)]
    public int FailedUsers { get; set; }

    /// <summary>
    /// Files
    /// </summary>
    [ProtoMember(16)]
    public List<string> Files { get; set; }

    /// <summary>
    /// Errors
    /// </summary>
    [ProtoMember(17)]
    public List<string> Errors { get; set; }
}
