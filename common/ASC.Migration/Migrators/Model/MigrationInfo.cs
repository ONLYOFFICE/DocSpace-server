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

namespace ASC.Migration.Core.Migrators.Model;
public class MigrationInfo
{
    public readonly Dictionary<string, MigrationUser> Users = new();
    public readonly Dictionary<string, MigrationUser> WithoutEmailUsers = new();
    public readonly Dictionary<string, MigrationUser> ExistUsers = new();
    public string Name { get; set; }
    public OperationType Operation { get; set; }
    public List<string> Files { get; set; }
    public List<string> FailedArchives = [];
    public readonly Dictionary<string, MigrationGroup> Groups = new();

    public MigrationStorage CommonStorage { get; set; }
    public MigrationStorage ProjectStorage { get; set; }

    public int SuccessedUsers { get; set; }
    public int FailedUsers { get; set; }
    public List<string> Errors { get; set; } = [];

    public MigrationApiInfo ToApiInfo()
    {
        return new MigrationApiInfo
        {
            Users = Users.Select(u => u.Value.ToApiInfo(u.Key)).ToList(),
            ExistUsers = ExistUsers.Select(u => u.Value.ToApiInfo(u.Key)).ToList(),
            WithoutEmailUsers = WithoutEmailUsers.Select(u => u.Value.ToApiInfo(u.Key)).ToList(),
            MigratorName = Name,
            FailedArchives = FailedArchives,
            SuccessedUsers = SuccessedUsers,
            FailedUsers = FailedUsers,
            Errors = Errors,
            Groups = Groups.Select(g => g.Value.ToApiInfo()).ToList(),
            Operation = Operation.ToStringLowerFast(),
            Files = Files
        };
    }

    public void Merge(MigrationApiInfo apiInfo)
    {
        Users.AddRange(ExistUsers);
        Users.AddRange(WithoutEmailUsers);
        foreach (var apiUser in apiInfo.Users)
        {
            if (!Users.TryGetValue(apiUser.Key, out var user))
            {
                continue;
            }

            user.ShouldImport = apiUser.ShouldImport;
            if (string.IsNullOrEmpty(user.Info.Email))
            {
                user.Info.Email = apiUser.Email;
            }
            user.UserType = apiUser.UserType;
            user.Storage.ShouldImport = apiUser.ShouldImport && apiInfo.ImportPersonalFiles;
            user.Storage.ShouldImportSharedFiles = apiInfo.ImportSharedFiles;
            user.Storage.ShouldImportSharedFolders = apiInfo.ImportSharedFolders;
        }

        CommonStorage?.ShouldImport = apiInfo.ImportCommonFiles;

        if (ProjectStorage != null)
        {
            ProjectStorage.ShouldImport = apiInfo.ImportProjectFiles;
            ProjectStorage.ShouldImportSharedFiles = true;
            ProjectStorage.ShouldImportSharedFolders = true;
        }

        foreach (var group in Groups)
        {
            group.Value.ShouldImport = apiInfo.ImportGroups;
        }
    }
}