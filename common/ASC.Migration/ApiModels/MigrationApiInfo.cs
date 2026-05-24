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
    /// <example>Nextcloud</example>
    [ProtoMember(1)]
    public string MigratorName { get; set; }

    /// <summary>
    /// The migration operation.
    /// </summary>
    /// <example>parse</example>
    [ProtoMember(2)]
    public string Operation { get; set; }

    /// <summary>
    /// The list of failed archives.
    /// </summary>
    /// <example>["archive1.zip", "archive2.zip"]</example>
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
    /// <example>true</example>
    [ProtoMember(8)]
    public bool ImportPersonalFiles { get; set; }

    /// <summary>
    /// Specifies whether to import shared files or not.
    /// </summary>
    /// <example>true</example>
    [ProtoMember(9)]
    public bool ImportSharedFiles { get; set; }

    /// <summary>
    /// Specifies whether to import shared folders or not.
    /// </summary>
    /// <example>true</example>
    [ProtoMember(10)]
    public bool ImportSharedFolders { get; set; }

    /// <summary>
    /// Specifies whether to import common files or not.
    /// </summary>
    /// <example>true</example>
    [ProtoMember(11)]
    public bool ImportCommonFiles { get; set; }

    /// <summary>
    /// Specifies whether to import project files or not.
    /// </summary>
    /// <example>false</example>
    [ProtoMember(12)]
    public bool ImportProjectFiles { get; set; }

    /// <summary>
    /// Specifies whether to import groups or not.
    /// </summary>
    /// <example>true</example>
    [ProtoMember(13)]
    public bool ImportGroups { get; set; }

    /// <summary>
    /// The number of successfully migrated users.
    /// </summary>
    /// <example>50</example>
    [ProtoMember(14)]
    public int SuccessedUsers { get; set; }

    /// <summary>
    /// The number of unsuccessfully migrated users.
    /// </summary>
    /// <example>2</example>
    [ProtoMember(15)]
    public int FailedUsers { get; set; }

    /// <summary>
    /// The list of migrated files.
    /// </summary>
    /// <example>["document.docx", "spreadsheet.xlsx"]</example>
    [ProtoMember(16)]
    public List<string> Files { get; set; }

    /// <summary>
    /// The list of migration errors.
    /// </summary>
    /// <example>["User not found", "File access denied"]</example>
    [ProtoMember(17)]
    public List<string> Errors { get; set; }
}