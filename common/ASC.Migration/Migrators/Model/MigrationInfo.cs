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

namespace ASC.Migration.Core.Migrators.Model;
public class MigrationInfo
{
    public readonly Dictionary<string, MigrationUser> Users = new();
    public readonly Dictionary<string, MigrationUser> WithoutEmailUsers = new();
    public readonly Dictionary<string, MigrationUser> ExistUsers = new();
    public string Name { get; set; }
    public OperationType Operation { get; set; }
    public List<string> Files { get; set; }
    public List<string> FailedArchives = new();
    public readonly Dictionary<string, MigrationGroup> Groups = new();

    public MigrationStorage CommonStorage { get; set; }
    public MigrationStorage ProjectStorage { get; set; }

    public int SuccessedUsers { get; set; }
    public int FailedUsers { get; set; }
    public List<string> Errors { get; set; } = new();

    public virtual MigrationApiInfo ToApiInfo()
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
            Operation = Operation.ToString().ToLower(),
            Files = Files
        };
    }

    public virtual void Merge(MigrationApiInfo apiInfo)
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

        if (CommonStorage != null)
        {
            CommonStorage.ShouldImport = apiInfo.ImportSharedFiles;
        }
        if (ProjectStorage != null)
        { 
            ProjectStorage.ShouldImport = apiInfo.ImportSharedFolders;
            ProjectStorage.ShouldImportSharedFiles = true;
            ProjectStorage.ShouldImportSharedFolders = true;
        }

        foreach (var group in Groups)
        {
            group.Value.ShouldImport = apiInfo.ImportGroups;
        }
    }
}
