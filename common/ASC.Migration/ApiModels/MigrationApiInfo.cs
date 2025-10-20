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

namespace ASC.Migration.Core.Models.Api;

/// <summary>
/// The migration API information.
/// </summary>
[ProtoContract]
public class MigrationApiInfo
{
    /// <summary>
    /// The migrator name.
    /// </summary>
    [ProtoMember(1)]
    public string MigratorName { get; set; }

    /// <summary>
    /// The migration operation.
    /// </summary>
    [ProtoMember(2)]
    public string Operation { get; set; }

    /// <summary>
    /// The list of failed archives.
    /// </summary>
    [ProtoMember(3)]
    public List<string> FailedArchives { get; set; } = [];

    /// <summary>
    /// The list of migrating users.
    /// </summary>
    [ProtoMember(4)]
    public List<MigratingApiUser> Users { get; set; } = [];

    /// <summary>
    /// The list of migrating users without email.
    /// </summary>
    [ProtoMember(5)]
    public List<MigratingApiUser> WithoutEmailUsers { get; set; } = [];

    /// <summary>
    /// The list of existing migrating users.
    /// </summary>
    [ProtoMember(6)]
    public List<MigratingApiUser> ExistUsers { get; set; } = [];

    /// <summary>
    /// The list of migrating groups.
    /// </summary>
    [ProtoMember(7)]
    public List<MigratingApiGroup> Groups { get; set; } = [];

    /// <summary>
    /// Specifies whether to import personal files or not.
    /// </summary>
    [ProtoMember(8)]
    public bool ImportPersonalFiles { get; set; }

    /// <summary>
    /// Specifies whether to import shared files or not.
    /// </summary>
    [ProtoMember(9)]
    public bool ImportSharedFiles { get; set; }

    /// <summary>
    /// Specifies whether to import shared folders or not.
    /// </summary>
    [ProtoMember(10)]
    public bool ImportSharedFolders { get; set; }

    /// <summary>
    /// Specifies whether to import common files or not.
    /// </summary>
    [ProtoMember(11)]
    public bool ImportCommonFiles { get; set; }

    /// <summary>
    /// Specifies whether to import project files or not.
    /// </summary>
    [ProtoMember(12)]
    public bool ImportProjectFiles { get; set; }

    /// <summary>
    /// Specifies whether to import groups or not.
    /// </summary>
    [ProtoMember(13)]
    public bool ImportGroups { get; set; }

    /// <summary>
    /// The number of successfully migrated users.
    /// </summary>
    [ProtoMember(14)]
    public int SuccessedUsers { get; set; }

    /// <summary>
    /// The number of unsuccessfully migrated users.
    /// </summary>
    [ProtoMember(15)]
    public int FailedUsers { get; set; }

    /// <summary>
    /// The list of migrated files.
    /// </summary>
    [ProtoMember(16)]
    public List<string> Files { get; set; }

    /// <summary>
    /// The list of migration errors.
    /// </summary>
    [ProtoMember(17)]
    public List<string> Errors { get; set; }
}