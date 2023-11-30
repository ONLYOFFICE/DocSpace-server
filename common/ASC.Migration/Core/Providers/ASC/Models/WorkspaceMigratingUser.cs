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

namespace ASC.Migration.Core.Core.Providers.Models;

[Transient]
public class WorkspaceMigratingUser : MigratingUser<WorkspaceMigratingFiles>
{
    public override string Email => _user.Info.Email;
    public override string DisplayName => $"{_user.Info.FirstName} {_user.Info.LastName}";
    public Guid Guid => _user.Info.Id;

    private WorkspaceUser _user;
    private bool _hasPhoto;
    private string _pathToPhoto;
    private string _rootFolder;
    private IDataReadOperator _dataReader;
    private readonly UserManager _userManager;
    private readonly TenantQuotaFeatureStatHelper _tenantQuotaFeatureStatHelper;
    private readonly QuotaSocketManager _quotaSocketManager;
    private readonly IServiceProvider _serviceProvider;

    public WorkspaceMigratingUser(IServiceProvider serviceProvider, UserManager userManager, TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper, QuotaSocketManager quotaSocketManager)
    {
        _serviceProvider = serviceProvider;
        _userManager = userManager;
        _tenantQuotaFeatureStatHelper = tenantQuotaFeatureStatHelper;
        _quotaSocketManager = quotaSocketManager;
    }

    public void Init(string key, WorkspaceUser user, string rootFolder, IDataReadOperator dataReader, Action<string, Exception> log)
    {
        Key = key;
        _dataReader = dataReader;
        _rootFolder = rootFolder;
        _user = user;
        Log = log;
    }

    public override void Parse()
    {
        var drivePath = Directory.Exists(Path.Combine(_rootFolder, _dataReader.GetFolder(), "storage", "userPhotos")) ?
            Path.Combine(_rootFolder, _dataReader.GetFolder(), "storage", "userPhotos") : null;
        if (drivePath == null)
        {
            _hasPhoto = false;
        }
        else
        {
            _pathToPhoto = Directory.GetFiles(drivePath).FirstOrDefault(p => Path.GetFileName(p).StartsWith(Key + "_orig_"));
            _hasPhoto = _pathToPhoto != null ? true : false;
        }

        MigratingFiles = _serviceProvider.GetService<WorkspaceMigratingFiles>();
        _user.Storage = new WorkspaceStorage()
        {
            Files = new List<WorkspaceFile>(),
            Folders = new List<WorkspaceFolder>()
        };
        MigratingFiles.Init(Key, this, _dataReader, _user.Storage, Log);
        MigratingFiles.Parse();
    }

    public override async Task MigrateAsync()
    {
        var saved = await _userManager.GetUserByEmailAsync(_user.Info.Email);
        if (saved == ASC.Core.Users.Constants.LostUser)
        {
            saved = await _userManager.SaveUserInfo(_user.Info, UserType);
            var groupId = UserType switch
            {
                EmployeeType.User => ASC.Core.Users.Constants.GroupUser.ID,
                EmployeeType.DocSpaceAdmin => ASC.Core.Users.Constants.GroupAdmin.ID,
                EmployeeType.Collaborator => ASC.Core.Users.Constants.GroupCollaborator.ID,
                _ => Guid.Empty,
            };

            if (groupId != Guid.Empty)
            {
                await _userManager.AddUserIntoGroupAsync(saved.Id, groupId, true);
            }
            else if (UserType == EmployeeType.RoomAdmin)
            {
                var (name, value) = await _tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
                _ = _quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            }

            _user.Info = saved;
            if (_hasPhoto)
            {
                using (var ms = new MemoryStream())
                {
                    using (var fs = File.OpenRead(_pathToPhoto))
                    {
                        fs.CopyTo(ms);
                    }
                    await _userManager.SaveUserPhotoAsync(saved.Id, ms.ToArray());
                }
            }
        }
    }
}
