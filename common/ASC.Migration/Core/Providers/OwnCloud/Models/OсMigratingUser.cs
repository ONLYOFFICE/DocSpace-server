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

namespace ASC.Migration.OwnCloud.Models;

[Transient]
public class OсMigratingUser(
    UserManager userManager,
    IServiceProvider serviceProvider,
    QuotaSocketManager quotaSocketManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper)
    : MigratingUser<OсMigratingFiles>
{
    public override string Email => _userInfo.Email;

    public override string DisplayName => _userInfo.ToString();

    public Guid Guid => _userInfo.Id;

    private string _rootFolder;
    private bool _hasPhoto;
    private string _pathToPhoto;
    private UserInfo _userInfo;
    private OсUser _user;
    private readonly Regex _emailRegex = new Regex(@"(\S*@\S*\.\S*)");

    public void Init(OсUser user, string rootFolder, Action<string, Exception> log)
    {
        _user = user;
        Key = user.Uid;
        _rootFolder = rootFolder;
        Log = log;
    }

    public override void Parse()
    {
        _userInfo = new UserInfo()
        {
            Id = Guid.NewGuid()
        };
        var drivePath = Directory.Exists(Path.Combine(_rootFolder, "data", Key, "cache")) ?
            Path.Combine(_rootFolder, "data", Key, "cache") : null;
        if (drivePath == null)
        {
            _hasPhoto = false;
        }
        else
        {
            _pathToPhoto = File.Exists(Path.Combine(drivePath, "avatar_upload")) ? Directory.GetFiles(drivePath, "avatar_upload")[0] : null;
            _hasPhoto = _pathToPhoto != null;
        }

        var userName = _user.Data.DisplayName.Split(' ');
        _userInfo.FirstName = userName[0];
        if (userName.Length > 1)
        {
            _userInfo.LastName = userName[1];
        }
        if (!string.IsNullOrEmpty(_user.Data.Email) && _user.Data.Email != "NULL")
        {
            var email = _emailRegex.Match(_user.Data.Email);
            if (email.Success)
            {
                _userInfo.Email = email.Groups[1].Value;
            }
            _userInfo.UserName = _userInfo.Email.Split('@').First();
        }
        _userInfo.ActivationStatus = EmployeeActivationStatus.Activated;
        Action<string, Exception> log = (m, e) => { Log($"{DisplayName} ({Email}): {m}", e); };

        MigratingFiles = serviceProvider.GetService<OсMigratingFiles>();
        MigratingFiles.Init(this, _user.Storages, _rootFolder, log);
        MigratingFiles.Parse();
    }

    public void DataСhange(MigratingApiUser frontUser)
    {
        if (_userInfo.Email == null)
        {
            _userInfo.Email = frontUser.Email;
            _userInfo.UserName ??= _userInfo.Email.Split('@').First();
        }

        _userInfo.LastName ??= "NOTPROVIDED";
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }
        var saved = await userManager.GetUserByEmailAsync(_userInfo.Email);
        if (saved.Equals(ASC.Core.Users.Constants.LostUser) || saved.Removed)
        {
            if (string.IsNullOrWhiteSpace(_userInfo.FirstName))
            {
                _userInfo.FirstName = FilesCommonResource.UnknownFirstName;
            }
            if (string.IsNullOrWhiteSpace(_userInfo.LastName))
            {
                _userInfo.LastName = FilesCommonResource.UnknownLastName;
            }
            _userInfo.Id = Guid.Empty;
            saved = await userManager.SaveUserInfo(_userInfo, UserType);
            var groupId = UserType switch
            {
                EmployeeType.Collaborator => ASC.Core.Users.Constants.GroupCollaborator.ID,
                EmployeeType.DocSpaceAdmin => ASC.Core.Users.Constants.GroupAdmin.ID,
                EmployeeType.RoomAdmin => ASC.Core.Users.Constants.GroupManager.ID,
                _ => Guid.Empty,
            };

            if (groupId != Guid.Empty)
            {
                await userManager.AddUserIntoGroupAsync(saved.Id, groupId, true);
            }
            else if (UserType == EmployeeType.RoomAdmin)
            {
                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            }

            if (_hasPhoto)
            {
                using var ms = new MemoryStream();
                await using (var fs = File.OpenRead(_pathToPhoto))
                {
                    await fs.CopyToAsync(ms);
                }
                await userManager.SaveUserPhotoAsync(saved.Id, ms.ToArray());
            }
        }
        _userInfo = saved;
    }
}
