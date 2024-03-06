// (c) Copyright Ascensio System SIA 2010-2022
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

using DotNetOpenAuth.Messaging;

namespace ASC.Migration.Core.Models;

public abstract class MigrationInfo<TUser, TFiles, TGroup> : IMigrationInfo
    where TUser : MigratingUser<TFiles>
    where TFiles : MigratingFiles
    where TGroup : MigratingGroup
{
    public Dictionary<string, TUser> Users = new Dictionary<string, TUser>();
    public Dictionary<string, TUser> WithoutEmailUsers = new Dictionary<string, TUser>();
    public Dictionary<string, TUser> ExistUsers = new Dictionary<string, TUser>();
    public string Path { get; set; }
    public string MigratorName { get; set; }
    public string Operation { get; set; }
    public List<string> Files { get; set; }
    public List<string> FailedArchives = new List<string>();
    public List<TGroup> Groups = new List<TGroup>();

    public int SuccessedUsers { get; set; }
    public int FailedUsers { get; set; }

    public virtual MigrationApiInfo ToApiInfo()
    {
        return new MigrationApiInfo()
        {
            Users = Users.Values.Select(u => u.ToApiInfo()).ToList(),
            ExistUsers = ExistUsers.Values.Select(u => u.ToApiInfo()).ToList(),
            WithoutEmailUsers = WithoutEmailUsers.Values.Select(u => u.ToApiInfo()).ToList(),
            MigratorName = MigratorName,
            FailedArchives = FailedArchives,
            SuccessedUsers = SuccessedUsers,
            FailedUsers = FailedUsers,
            Groups = Groups.Select(g => g.ToApiInfo()).ToList(),
            Operation = Operation,
            Files = Files,
        };
    }

    public virtual void Merge(MigrationApiInfo apiInfo)
    {
        Users.AddRange(ExistUsers);
        Users.AddRange(WithoutEmailUsers);
        foreach (var apiUser in apiInfo.Users)
        {
            if (!Users.ContainsKey(apiUser.Key))
            {
                continue;
            }
            var user = Users[apiUser.Key];
            user.ShouldImport = apiUser.ShouldImport;
            user.UserType = apiUser.UserType;
            user.MigratingFiles.ShouldImport = apiUser.ShouldImport && apiInfo.ImportPersonalFiles;
            user.MigratingFiles.ShouldImportSharedFiles = apiInfo.ImportSharedFiles;
            user.MigratingFiles.ShouldImportSharedFolders = apiInfo.ImportSharedFolders;
        }

        foreach (var group in Groups)
        {
            group.ShouldImport = apiInfo.ImportGroups;
        }
    }
}
