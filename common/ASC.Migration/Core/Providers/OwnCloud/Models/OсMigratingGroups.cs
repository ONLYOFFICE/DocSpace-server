// (c) Copyright Ascensio System SIA 2010-2023
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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Migration.OwnCloud.Models;

[Transient]
public class OсMigratingGroups(UserManager userManager) : MigratingGroup
{
    private string _groupName;
    private List<string> _userUidList;
    private OсGroup _group;
    private GroupInfo _groupInfo;
    public Dictionary<string, Guid> UsersGuidList;
    public override List<string> UserGuidList => _userUidList;
    public override string GroupName => _groupName;

    public void Init(OсGroup group, Action<string, Exception> log)
    {
        _group = group;
        Log = log;
    }

    public override void Parse()
    {
        _groupName = _group.GroupGid;
        _groupInfo = new GroupInfo()
        {
            Name = _group.GroupGid
        };
        _userUidList = _group.UsersUid;
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }
        var existingGroups = (await userManager.GetGroupsAsync()).ToList();
        var oldGroup = existingGroups.Find(g => g.Name == _groupInfo.Name);
        if (oldGroup != null)
        {
            _groupInfo = oldGroup;
        }
        else
        {
            _groupInfo = await userManager.SaveGroupInfoAsync(_groupInfo);
        }
        foreach (var userGuid in UsersGuidList)
        {
            try
            {
                var user = await userManager.GetUsersAsync(userGuid.Value);
                if (user.Equals(Constants.LostUser))
                {
                    throw new ArgumentNullException();
                }
                if (!await userManager.IsUserInGroupAsync(user.Id, _groupInfo.ID))
                {
                    await userManager.AddUserIntoGroupAsync(user.Id, _groupInfo.ID);
                }
            }
            catch (Exception ex)
            {
                //Think about the text of the error
                Log($"Couldn't to add user {userGuid.Key} to group {_groupName} ", ex);
            }
        }
    }
}
