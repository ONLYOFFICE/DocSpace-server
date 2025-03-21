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

using Constants = ASC.Core.Configuration.Constants;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.ActiveDirectory.ComplexOperations;

[Transient]
public class LdapOperationJob : DistributedTaskProgress
{
    private string _culture;

    private LdapSettings _ldapSettings;
    public string Source { get; set; }
    public string Result { get; set; }
    public string Error { get; set; }
    public string Warning { get; set; }
    public int TenantId { get; set; }
    public bool Finished { get; set; }
    public string CertRequest { get; set; }

    public LdapOperationType OperationType { get; set; }
    private LdapLocalization _resource;

    private SettingsManager _settingsManager;
    private UserManager _userManager;
    private LdapUserManager _ldapUserManager;
    private NovellLdapUserImporter _novellLdapUserImporter;

    private SecurityContext _securityContext;
    private NovellLdapHelper _novellLdapHelper;
    private LdapChangeCollection _ldapChanges;
    private UserFormatter _userFormatter;
    private UserPhotoManager _userPhotoManager;
    private WebItemSecurity _webItemSecurity;
    private DisplayUserSettingsHelper _displayUserSettingsHelper;
    private NovellLdapSettingsChecker _novellLdapSettingsChecker;
    private ILogger<LdapOperationJob> _logger;

    private UserInfo _currentUser;
    private TenantManager _tenantManager;
    private IServiceScopeFactory _serviceScopeFactory;

    public LdapOperationJob()
    {
        
    }
    
    public LdapOperationJob(LdapUserManager ldapUserManager,
        UserManager userManager,
        TenantManager tenantManager,
        IServiceScopeFactory serviceScopeFactory)
    {
        _ldapUserManager = ldapUserManager;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InitJobAsync(
        LdapSettings settings,
        Tenant tenant,
        LdapOperationType operationType,
        LdapLocalization resource,
        string userId)
    {
        _tenantManager.SetCurrentTenant(tenant);

        _currentUser = userId != null ? await _userManager.GetUsersAsync(Guid.Parse(userId)) : null;

        TenantId = tenant.Id;

        OperationType = operationType;

        _culture = CultureInfo.CurrentCulture.Name;

        _ldapSettings = settings;

        Source = "";
        Percentage = 0;
        Result = "";
        Error = "";
        Warning = "";

        _resource = resource ?? new LdapLocalization();
        _ldapUserManager.Init(_resource);

        InitDisturbedTask();
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        _settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
        _ldapUserManager = scope.ServiceProvider.GetRequiredService<LdapUserManager>();
        _novellLdapUserImporter = scope.ServiceProvider.GetRequiredService<NovellLdapUserImporter>();

        _securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
        _novellLdapHelper = scope.ServiceProvider.GetRequiredService<NovellLdapHelper>();
        _ldapChanges = scope.ServiceProvider.GetRequiredService<LdapChangeCollection>();
        _userFormatter = scope.ServiceProvider.GetRequiredService<UserFormatter>();
        _userPhotoManager = scope.ServiceProvider.GetRequiredService<UserPhotoManager>();
        _webItemSecurity = scope.ServiceProvider.GetRequiredService<WebItemSecurity>();
        _displayUserSettingsHelper = scope.ServiceProvider.GetRequiredService<DisplayUserSettingsHelper>();
        _novellLdapSettingsChecker = scope.ServiceProvider.GetRequiredService<NovellLdapSettingsChecker>();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<LdapOperationJob>>();

        _tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        _serviceScopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        try
        {
            await _tenantManager.SetCurrentTenantAsync(TenantId);

            await _securityContext.AuthenticateMeAsync(Constants.CoreSystem);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(_culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(_culture);

            if (_ldapSettings == null)
            {
                Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                _logger.ErrorSaveDefaultLdapSettings();
                return;
            }

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.SaveTest:

                    _logger.InfoStartOperation(Enum.GetName(OperationType));

                    await SetProgress(1, _resource.LdapSettingsStatusCheckingLdapSettings);

                    _logger.DebugPrepareSettings();

                    await PrepareSettingsAsync(_ldapSettings);

                    if (!string.IsNullOrEmpty(Error))
                    {
                        _logger.DebugPrepareSettingsError(Error);
                        return;
                    }

                    _novellLdapUserImporter.Init(_ldapSettings, _resource);

                    if (_ldapSettings.EnableLdapAuthentication)
                    {
                        _novellLdapSettingsChecker.Init(_novellLdapUserImporter);

                        await SetProgress(5, _resource.LdapSettingsStatusLoadingBaseInfo);

                        var result = await _novellLdapSettingsChecker.CheckSettings();

                        if (result != LdapSettingsStatus.Ok)
                        {
                            if (result == LdapSettingsStatus.CertificateRequest)
                            {
                                CertRequest = Convert.ToString(_novellLdapSettingsChecker.CertificateConfirmRequest);
                            }

                            Error = GetError(result);

                            _logger.DebugCheckSettingsError(Error);

                            return;
                        }
                    }

                    break;
                case LdapOperationType.Sync:
                case LdapOperationType.SyncTest:
                    _logger.InfoStartOperation(Enum.GetName(OperationType));

                    _novellLdapUserImporter.Init(_ldapSettings, _resource);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await Do();
        }
        catch (AuthorizingException authError)
        {
            Error = _resource.ErrorAccessDenied;
            _logger.ErrorAuthorizing(Error, new SecurityException(Error, authError));
        }
        catch (AggregateException ae)
        {
            ae.Flatten().Handle(e => e is TaskCanceledException or OperationCanceledException);
        }
        catch (TenantQuotaException e)
        {
            Error = _resource.LdapSettingsTenantQuotaSettled;
            _logger.ErrorTenantQuota(e);
        }
        catch (FormatException e)
        {
            Error = _resource.LdapSettingsErrorCantCreateUsers;
            _logger.ErrorFormatException(e);
        }
        catch (Exception e)
        {
            Error = _resource.LdapSettingsInternalServerError;
            _logger.ErrorInternal(e);
        }
        finally
        {
            try
            {
                Finished = true;
                await PublishChanges();
                _securityContext.Logout();
            }
            catch (Exception ex)
            {
                _logger.ErrorLdapOperationFinalizationlProblem(ex);
            }
        }
    }

    private async Task Do()
    {
        try
        {
            if (OperationType == LdapOperationType.Save)
            {
                await SetProgress(10, _resource.LdapSettingsStatusSavingSettings);

                _ldapSettings.IsDefault = _ldapSettings.Equals(_ldapSettings.GetDefault());

                if (!await _settingsManager.SaveAsync(_ldapSettings))
                {
                    _logger.ErrorSaveLdapSettings();
                    Error = _resource.LdapSettingsErrorCantSaveLdapSettings;
                    return;
                }
            }

            if (_ldapSettings.EnableLdapAuthentication)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("SyncLDAP()");
                    sb.AppendLine($"Server: {_ldapSettings.Server}:{_ldapSettings.PortNumber}");
                    sb.AppendLine("UserDN: " + _ldapSettings.UserDN);
                    sb.AppendLine("LoginAttr: " + _ldapSettings.LoginAttribute);
                    sb.AppendLine("UserFilter: " + _ldapSettings.UserFilter);
                    sb.AppendLine("Groups: " + _ldapSettings.GroupMembership);
                    if (_ldapSettings.GroupMembership)
                    {
                        sb.AppendLine("GroupDN: " + _ldapSettings.GroupDN);
                        sb.AppendLine("UserAttr: " + _ldapSettings.UserAttribute);
                        sb.AppendLine("GroupFilter: " + _ldapSettings.GroupFilter);
                        sb.AppendLine("GroupName: " + _ldapSettings.GroupNameAttribute);
                        sb.AppendLine("GroupMember: " + _ldapSettings.GroupAttribute);
                    }

                    _logger.DebugLdapSettings(sb.ToString());
                }

                await SyncLDAPAsync();

                if (!string.IsNullOrEmpty(Error))
                {
                    return;
                }
            }
            else
            {
                _logger.DebugTurnOffLDAP();

                await TurnOffLDAPAsync();
                var ldapCurrentUserPhotos = (await _settingsManager.LoadAsync<LdapCurrentUserPhotos>()).GetDefault();
                await _settingsManager.SaveAsync(ldapCurrentUserPhotos);

                var ldapCurrentAccessSettings = (await _settingsManager.LoadAsync<LdapCurrentAcccessSettings>()).GetDefault();
                await _settingsManager.SaveAsync(ldapCurrentAccessSettings);
                // don't remove permissions on shutdown
                //var rights = new List<LdapSettings.AccessRight>();
                //TakeUsersRights(rights);

                //if (rights.Count > 0)
                //{
                //    Warning = Resource.LdapSettingsErrorLostRights;
                //}
            }
        }
        catch (NovellLdapTlsCertificateRequestedException ex)
        {
            _logger.ErrorCheckSettings(
                _ldapSettings.AcceptCertificate, _ldapSettings.AcceptCertificateHash, ex);
            Error = _resource.LdapSettingsStatusCertificateVerification;

            //TaskInfo.SetProperty(CERT_REQUEST, ex.CertificateConfirmRequest);
        }
        catch (TenantQuotaException e)
        {
            _logger.ErrorTenantQuota(e);
            Error = _resource.LdapSettingsTenantQuotaSettled;
        }
        catch (FormatException e)
        {
            _logger.ErrorFormatException(e);
            Error = _resource.LdapSettingsErrorCantCreateUsers;
        }
        catch (Exception e)
        {
            _logger.ErrorInternal(e);
            Error = _resource.LdapSettingsInternalServerError;
        }
        finally
        {
            await SetProgress(99, _resource.LdapSettingsStatusDisconnecting, "");
        }

        await SetProgress(100, OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest
            ? JsonSerializer.Serialize(_ldapChanges)
            : "", "");
    }

    private async Task TurnOffLDAPAsync()
    {
        const double percents = 48;

        await SetProgress((int)percents, _resource.LdapSettingsModifyLdapUsers);

        var existingLDAPUsers = (await _userManager.GetUsersAsync(EmployeeStatus.All)).Where(u => u.Sid != null).ToList();

        var step = percents / existingLDAPUsers.Count;

        var percentage = GetProgress();

        var index = 0;
        var count = existingLDAPUsers.Count;

        foreach (var existingLDAPUser in existingLDAPUsers)
        {
            await SetProgress(Convert.ToInt32(percentage), currentSource: $"({++index}/{count}): {_userFormatter.GetUserName(existingLDAPUser)}");

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    existingLDAPUser.Sid = null;
                    existingLDAPUser.ConvertExternalContactsToOrdinary();

                    _logger.DebugSaveUserInfo(existingLDAPUser.GetUserInfoString());

                    await _userManager.UpdateUserInfoAsync(existingLDAPUser);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    _ldapChanges.SetSaveAsPortalUserChange(existingLDAPUser);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }
    }

    private async Task SyncLDAPAsync()
    {
        var currentDomainSettings = await _settingsManager.LoadAsync<LdapCurrentDomain>();

        if (string.IsNullOrEmpty(currentDomainSettings.CurrentDomain) || currentDomainSettings.CurrentDomain != _novellLdapUserImporter.LDAPDomain)
        {
            currentDomainSettings.CurrentDomain = _novellLdapUserImporter.LDAPDomain;
            await _settingsManager.SaveAsync(currentDomainSettings);
        }

        if (!_ldapSettings.GroupMembership)
        {
            _logger.DebugSyncLDAPUsers();

            await SyncLDAPUsersAsync();
        }
        else
        {
            _logger.DebugSyncLDAPUsersInGroups();

            await SyncLDAPUsersInGroupsAsync();
        }

        await SyncLdapAvatarAsync();

        await SyncLdapAccessRights();
    }

    private async Task SyncLdapAvatarAsync()
    {
        await SetProgress(90, _resource.LdapSettingsStatusUpdatingUserPhotos);

        if (!_ldapSettings.LdapMapping.ContainsKey(LdapSettings.MappingFields.AvatarAttribute))
        {
            var ph = await _settingsManager.LoadAsync<LdapCurrentUserPhotos>();

            if (ph.CurrentPhotos == null || ph.CurrentPhotos.Count == 0)
            {
                return;
            }

            foreach (var guid in ph.CurrentPhotos.Keys)
            {
                _logger.InfoSyncLdapAvatarsRemovingPhoto(guid);
                await _userPhotoManager.RemovePhotoAsync(guid);
                await _userPhotoManager.ResetThumbnailSettingsAsync(guid);
            }

            ph.CurrentPhotos = null;
            await _settingsManager.SaveAsync(ph);
            return;
        }

        var photoSettings = await _settingsManager.LoadAsync<LdapCurrentUserPhotos>();

        photoSettings.CurrentPhotos ??= new Dictionary<Guid, string>();

        var ldapUsers = _novellLdapUserImporter.AllDomainUsers.Where(x => !x.IsDisabled);
        var step = 5.0 / ldapUsers.Count();
        var currentPercent = 90.0;
        foreach (var ldapUser in ldapUsers)
        {
            var image = ldapUser.GetValue(_ldapSettings.LdapMapping[LdapSettings.MappingFields.AvatarAttribute], true);

            if (image == null || image.GetType() != typeof(byte[]))
            {
                continue;
            }

            var hash = Convert.ToBase64String(MD5.HashData((byte[])image));

            var user = await _userManager.GetUserBySidAsync(ldapUser.Sid);

            _logger.DebugSyncLdapAvatarsFoundPhoto(ldapUser.Sid);

            if (photoSettings.CurrentPhotos.ContainsKey(user.Id) && photoSettings.CurrentPhotos[user.Id] == hash)
            {
                _logger.DebugSyncLdapAvatarsSkipping();
                continue;
            }

            try
            {
                await SetProgress((int)(currentPercent += step), $"{_resource.LdapSettingsStatusSavingUserPhoto}: {_userFormatter.GetUserName(user)}");

                await _userPhotoManager.SyncPhotoAsync(user.Id, (byte[])image);

                photoSettings.CurrentPhotos[user.Id] = hash;
            }
            catch
            {
                _logger.DebugSyncLdapAvatarsCouldNotSavePhoto(user.Id);
                if (photoSettings.CurrentPhotos.ContainsKey(user.Id))
                {
                    photoSettings.CurrentPhotos.Remove(user.Id);
                }
            }
        }

        await _settingsManager.SaveAsync(photoSettings);
    }

    private async Task SyncLdapAccessRights()
    {
        await SetProgress(95, _resource.LdapSettingsStatusUpdatingAccessRights);

        var currentUserRights = new List<LdapSettings.AccessRight>();
        await TakeUsersRightsAsync(_currentUser != null ? currentUserRights : null);

        if (_ldapSettings.GroupMembership && _ldapSettings.AccessRights is { Count: > 0 })
        {
            await GiveUsersRights(_ldapSettings.AccessRights, _currentUser != null ? currentUserRights : null);
        }

        if (currentUserRights.Count > 0)
        {
            Warning = _resource.LdapSettingsErrorLostRights;
        }

        await _settingsManager.SaveAsync(_ldapSettings);
    }

    private async Task TakeUsersRightsAsync(List<LdapSettings.AccessRight> currentUserRights)
    {
        var current = await _settingsManager.LoadAsync<LdapCurrentAcccessSettings>();

        if (current.CurrentAccessRights == null || current.CurrentAccessRights.Count == 0)
        {
            _logger.DebugAccessRightsIsEmpty();
            return;
        }

        await SetProgress(95, _resource.LdapSettingsStatusRemovingOldRights);
        foreach (var right in current.CurrentAccessRights)
        {
            foreach (var user in right.Value)
            {
                var userId = Guid.Parse(user);
                if (_currentUser != null && _currentUser.Id == userId)
                {
                    _logger.DebugAttemptingTakeAdminRights(user);
                    currentUserRights?.Add(right.Key);
                }
                else
                {
                    _logger.DebugTakingAdminRights(right.Key, user);
                    await _webItemSecurity.SetProductAdministrator(LdapSettings.AccessRightsGuids[right.Key], userId, false);
                }
            }
        }

        current.CurrentAccessRights = null;
        await _settingsManager.SaveAsync(current);
    }

    private async Task GiveUsersRights(Dictionary<LdapSettings.AccessRight, string> accessRightsSettings, List<LdapSettings.AccessRight> currentUserRights)
    {
        var current = await _settingsManager.LoadAsync<LdapCurrentAcccessSettings>();
        var currentAccessRights = new Dictionary<LdapSettings.AccessRight, List<string>>();
        var usersWithRightsFlat = current.CurrentAccessRights == null ? [] : current.CurrentAccessRights.SelectMany(x => x.Value).Distinct().ToList();

        var step = 3.0 / accessRightsSettings.Count;
        var currentPercent = 95.0;
        foreach (var access in accessRightsSettings)
        {
            currentPercent += step;
            var ldapGroups = _novellLdapUserImporter.FindGroupsByAttribute(_ldapSettings.GroupNameAttribute, access.Value.Split(',').Select(x => x.Trim()));

            if (ldapGroups.Count == 0)
            {
                _logger.DebugGiveUsersRightsNoLdapGroups(access.Key);
                continue;
            }

            foreach (var ldapGr in ldapGroups)
            {
                var gr = await _userManager.GetGroupInfoBySidAsync(ldapGr.Sid);

                if (gr == null)
                {
                    _logger.DebugGiveUsersRightsCouldNotFindPortalGroup(ldapGr.Sid);
                    continue;
                }

                var users = await _userManager.GetUsersByGroupAsync(gr.ID);

                _logger.DebugGiveUsersRightsFoundUsersForGroup(users.Length, gr.Name, gr.ID);


                foreach (var user in users)
                {
                    if (!user.Equals(Core.Users.Constants.LostUser) && !await _userManager.IsGuestAsync(user))
                    {
                        if (!usersWithRightsFlat.Contains(user.Id.ToString()))
                        {
                            usersWithRightsFlat.Add(user.Id.ToString());

                            var cleared = false;

                            foreach (var r in Enum.GetValues<LdapSettings.AccessRight>())
                            {
                                var prodId = LdapSettings.AccessRightsGuids[r];

                                if (await _webItemSecurity.IsProductAdministratorAsync(prodId, user.Id))
                                {
                                    cleared = true;
                                    await _webItemSecurity.SetProductAdministrator(prodId, user.Id, false);
                                }
                            }

                            if (cleared)
                            {
                                _logger.DebugGiveUsersRightsClearedAndAddedRights(user.DisplayUserName(_displayUserSettingsHelper));
                            }
                        }

                        if (!currentAccessRights.ContainsKey(access.Key))
                        {
                            currentAccessRights.Add(access.Key, []);
                        }

                        currentAccessRights[access.Key].Add(user.Id.ToString());

                        await SetProgress((int)currentPercent, string.Format(_resource.LdapSettingsStatusGivingRights, _userFormatter.GetUserName(user), access.Key));
                        await _webItemSecurity.SetProductAdministrator(LdapSettings.AccessRightsGuids[access.Key], user.Id, true);

                        if (currentUserRights != null && currentUserRights.Contains(access.Key))
                        {
                            currentUserRights.Remove(access.Key);
                        }
                    }
                }
            }
        }

        current.CurrentAccessRights = currentAccessRights;
        await _settingsManager.SaveAsync(current);
    }

    private async Task SyncLDAPUsersAsync()
    {
        await SetProgress(15, _resource.LdapSettingsStatusGettingUsersFromLdap);

        var ldapUsers = await _novellLdapUserImporter.GetDiscoveredUsersByAttributesAsync();

        if (ldapUsers.Count == 0)
        {
            Error = _resource.LdapSettingsErrorUsersNotFound;
            return;
        }

        _logger.DebugGetDiscoveredUsersByAttributes(_novellLdapUserImporter.AllDomainUsers.Count);

        await SetProgress(20, _resource.LdapSettingsStatusRemovingOldUsers, "");

        ldapUsers = await RemoveOldDbUsersAsync(ldapUsers);

        await SetProgress(30,
            OperationType is LdapOperationType.Save or LdapOperationType.SaveTest
                ? _resource.LdapSettingsStatusSavingUsers
                : _resource.LdapSettingsStatusSyncingUsers,
            "");

        await SyncDbUsers(ldapUsers);

        await SetProgress(70, _resource.LdapSettingsStatusRemovingOldGroups, "");

        await RemoveOldDbGroupsAsync([]); // Remove all db groups with sid
    }

    private async Task SyncLDAPUsersInGroupsAsync()
    {
        await SetProgress(15, _resource.LdapSettingsStatusGettingGroupsFromLdap);

        var ldapGroups = _novellLdapUserImporter.GetDiscoveredGroupsByAttributes();

        if (ldapGroups.Count == 0)
        {
            Error = _resource.LdapSettingsErrorGroupsNotFound;
            return;
        }

        _logger.DebugGetDiscoveredGroupsByAttributes(_novellLdapUserImporter.AllDomainGroups.Count);

        await SetProgress(20, _resource.LdapSettingsStatusGettingUsersFromLdap);

        var (ldapGroupsUsers, uniqueLdapGroupUsers) = await GetGroupsUsersAsync(ldapGroups);

        if (uniqueLdapGroupUsers.Count == 0)
        {
            Error = _resource.LdapSettingsErrorUsersNotFound;
            return;
        }

        _logger.DebugGetGroupsUsers(_novellLdapUserImporter.AllDomainUsers.Count);

        await SetProgress(30,
            OperationType is LdapOperationType.Save or LdapOperationType.SaveTest
                ? _resource.LdapSettingsStatusSavingUsers
                : _resource.LdapSettingsStatusSyncingUsers,
            "");

        var newUniqueLdapGroupUsers = await SyncGroupsUsers(uniqueLdapGroupUsers);

        await SetProgress(60, _resource.LdapSettingsStatusSavingGroups, "");

        await SyncDbGroups(ldapGroupsUsers);

        await SetProgress(80, _resource.LdapSettingsStatusRemovingOldGroups, "");

        await RemoveOldDbGroupsAsync(ldapGroups);

        await SetProgress(90, _resource.LdapSettingsStatusRemovingOldUsers, "");

        await RemoveOldDbUsersAsync(newUniqueLdapGroupUsers);
    }

    private async Task SyncDbGroups(Dictionary<GroupInfo, List<UserInfo>> ldapGroupsWithUsers)
    {
        const double percents = 20;

        var step = percents / ldapGroupsWithUsers.Count;

        var percentage = GetProgress();

        if (ldapGroupsWithUsers.Count == 0)
        {
            return;
        }

        var gIndex = 0;
        var gCount = ldapGroupsWithUsers.Count;

        foreach (var (ldapGroup, ldapGroupUsers) in ldapGroupsWithUsers)
        {
            ++gIndex;

            await SetProgress(Convert.ToInt32(percentage), currentSource: $"({gIndex}/{gCount}): {ldapGroup.Name}");

            var dbLdapGroup = await _userManager.GetGroupInfoBySidAsync(ldapGroup.Sid);

            if (Equals(dbLdapGroup, Core.Users.Constants.LostGroupInfo))
            {
                await AddNewGroupAsync(ldapGroup, ldapGroupUsers, gIndex, gCount);
            }
            else
            {
                await UpdateDbGroupAsync(dbLdapGroup, ldapGroup, ldapGroupUsers, gIndex, gCount);
            }

            percentage += step;
        }
    }

    private async Task AddNewGroupAsync(GroupInfo ldapGroup, List<UserInfo> ldapGroupUsers, int gIndex, int gCount)
    {
        if (ldapGroupUsers.Count == 0) // Skip empty groups
        {
            if (OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest)
            {
                _ldapChanges.SetSkipGroupChange(ldapGroup);
            }

            return;
        }

        var groupMembersToAdd = await ldapGroupUsers.ToAsyncEnumerable().SelectAwait(async ldapGroupUser => await SearchDbUserBySidAsync(ldapGroupUser.Sid))
            .Where(userBySid => !Equals(userBySid, Core.Users.Constants.LostUser))
            .ToListAsync();

        if (groupMembersToAdd.Count != 0)
        {
            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    ldapGroup = await _userManager.SaveGroupInfoAsync(ldapGroup);

                    var index = 0;
                    var count = groupMembersToAdd.Count;

                    foreach (var userBySid in groupMembersToAdd)
                    {
                        await SetProgress(currentSource: $"({gIndex}/{gCount}): {ldapGroup.Name}, {_resource.LdapSettingsStatusAddingGroupUser} ({++index}/{count}): {_userFormatter.GetUserName(userBySid)}");

                        await _userManager.AddUserIntoGroupAsync(userBySid.Id, ldapGroup.ID);
                    }

                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    _ldapChanges.SetAddGroupChange(ldapGroup);
                    _ldapChanges.SetAddGroupMembersChange(ldapGroup, groupMembersToAdd);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            if (OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest)
            {
                _ldapChanges.SetSkipGroupChange(ldapGroup);
            }
        }
    }

    private static bool NeedUpdateGroup(GroupInfo portalGroup, GroupInfo ldapGroup)
    {
        var needUpdate =
            !portalGroup.Name.Equals(ldapGroup.Name, StringComparison.InvariantCultureIgnoreCase) ||
            !portalGroup.Sid.Equals(ldapGroup.Sid, StringComparison.InvariantCultureIgnoreCase);

        return needUpdate;
    }

    private async Task UpdateDbGroupAsync(GroupInfo dbLdapGroup, GroupInfo ldapGroup, List<UserInfo> ldapGroupUsers, int gIndex, int gCount)
    {
        await SetProgress(currentSource: $"({gIndex}/{gCount}): {ldapGroup.Name}");

        var dbGroupMembers =
            (await _userManager.GetUsersByGroupAsync(dbLdapGroup.ID, EmployeeStatus.All))
            .Where(u => u.Sid != null)
            .ToList();

        var groupMembersToRemove = dbGroupMembers.Where(dbUser => ldapGroupUsers.FirstOrDefault(lu => dbUser.Sid.Equals(lu.Sid)) == null).ToList();

        var groupMembersToAdd = await ldapGroupUsers.ToAsyncEnumerable().Where(q => dbGroupMembers.FirstOrDefault(u => u.Sid.Equals(q.Sid)) == null)
            .SelectAwait(async q => await SearchDbUserBySidAsync(q.Sid)).Where(q => !Equals(q, Core.Users.Constants.LostUser)).ToListAsync();


        switch (OperationType)
        {
            case LdapOperationType.Save:
            case LdapOperationType.Sync:
                if (NeedUpdateGroup(dbLdapGroup, ldapGroup))
                {
                    dbLdapGroup.Name = ldapGroup.Name;
                    dbLdapGroup.Sid = ldapGroup.Sid;

                    dbLdapGroup = await _userManager.SaveGroupInfoAsync(dbLdapGroup);
                }

                var index = 0;
                var count = groupMembersToRemove.Count;

                foreach (var dbUser in groupMembersToRemove)
                {
                    await SetProgress(currentSource: $"({gIndex}/{gCount}): {dbLdapGroup.Name}, {_resource.LdapSettingsStatusRemovingGroupUser} ({++index}/{count}): {_userFormatter.GetUserName(dbUser)}");

                    await _userManager.RemoveUserFromGroupAsync(dbUser.Id, dbLdapGroup.ID);
                }

                index = 0;
                count = groupMembersToAdd.Count;

                foreach (var userInfo in groupMembersToAdd)
                {
                    await SetProgress(currentSource: $"({gIndex}/{gCount}): {ldapGroup.Name}, {_resource.LdapSettingsStatusAddingGroupUser} ({++index}/{count}): {_userFormatter.GetUserName(userInfo)}");

                    await _userManager.AddUserIntoGroupAsync(userInfo.Id, dbLdapGroup.ID);
                }

                if (dbGroupMembers.All(dbUser => groupMembersToRemove.Exists(u => u.Id.Equals(dbUser.Id)))
                    && groupMembersToAdd.Count == 0)
                {
                    await SetProgress(currentSource: $"({gIndex}/{gCount}): {dbLdapGroup.Name}");

                    await _userManager.DeleteGroupAsync(dbLdapGroup.ID);
                }

                break;
            case LdapOperationType.SaveTest:
            case LdapOperationType.SyncTest:
                if (NeedUpdateGroup(dbLdapGroup, ldapGroup))
                {
                    _ldapChanges.SetUpdateGroupChange(ldapGroup);
                }

                if (groupMembersToRemove.Count != 0)
                {
                    _ldapChanges.SetRemoveGroupMembersChange(dbLdapGroup, groupMembersToRemove);
                }

                if (groupMembersToAdd.Count != 0)
                {
                    _ldapChanges.SetAddGroupMembersChange(dbLdapGroup, groupMembersToAdd);
                }

                if (dbGroupMembers.All(dbUser => groupMembersToRemove.Exists(u => u.Id.Equals(dbUser.Id)))
                    && groupMembersToAdd.Count == 0)
                {
                    _ldapChanges.SetRemoveGroupChange(dbLdapGroup, _logger);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<UserInfo> SearchDbUserBySidAsync(string sid)
    {
        if (string.IsNullOrEmpty(sid))
        {
            return Core.Users.Constants.LostUser;
        }

        var foundUser = await _userManager.GetUserBySidAsync(sid);

        return foundUser;
    }

    private async Task SyncDbUsers(List<UserInfo> ldapUsers)
    {
        const double percents = 35;

        var step = percents / ldapUsers.Count;

        var percentage = GetProgress();

        if (ldapUsers.Count == 0)
        {
            return;
        }

        var index = 0;
        var count = ldapUsers.Count;

        foreach (var userInfo in ldapUsers)
        {
            await SetProgress(Convert.ToInt32(percentage), currentSource: $"({++index}/{count}): {_userFormatter.GetUserName(userInfo)}");

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    await _ldapUserManager.SyncLDAPUserAsync(userInfo, ldapUsers);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    var changes = (await _ldapUserManager.GetLDAPSyncUserChangeAsync(userInfo, ldapUsers)).LdapChangeCollection;
                    _ldapChanges.AddRange(changes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }
    }

    /// <summary>
    /// Remove old LDAP users from db
    /// </summary>
    /// <param name="ldapUsers">list of actual LDAP users</param>
    /// <returns>New list of actual LDAP users</returns>
    private async Task<List<UserInfo>> RemoveOldDbUsersAsync(List<UserInfo> ldapUsers)
    {
        var dbLdapUsers = (await _userManager.GetUsersAsync(EmployeeStatus.All)).Where(u => u.Sid != null).ToList();

        if (dbLdapUsers.Count == 0)
        {
            return ldapUsers;
        }

        var removedUsers = dbLdapUsers.Where(u => ldapUsers.FirstOrDefault(lu => u.Sid.Equals(lu.Sid)) == null).ToList();

        if (removedUsers.Count == 0)
        {
            return ldapUsers;
        }

        const double percents = 8;

        var step = percents / removedUsers.Count;

        var percentage = GetProgress();

        var index = 0;
        var count = removedUsers.Count;

        foreach (var removedUser in removedUsers)
        {
            await SetProgress(Convert.ToInt32(percentage),
                currentSource:
                $"({++index}/{count}): {_userFormatter.GetUserName(removedUser)}");

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    removedUser.Sid = null;
                    if (!removedUser.IsOwner(_tenantManager.GetCurrentTenant()) && !(_currentUser != null && _currentUser.Id == removedUser.Id && await _userManager.IsDocSpaceAdminAsync(removedUser)))
                    {
                        removedUser.Status = EmployeeStatus.Terminated; // Disable user on portal
                    }
                    else
                    {
                        Warning = _resource.LdapSettingsErrorRemovedYourself;
                        _logger.DebugRemoveOldDbUsersAttemptingExcludeYourself(removedUser.Id);
                    }

                    removedUser.ConvertExternalContactsToOrdinary();

                    _logger.DebugSaveUserInfo(removedUser.GetUserInfoString());

                    await _userManager.UpdateUserInfoAsync(removedUser);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    _ldapChanges.SetSaveAsPortalUserChange(removedUser);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }

        dbLdapUsers.RemoveAll(removedUsers.Contains);

        var newLdapUsers = ldapUsers.Where(u => !removedUsers.Exists(ru => ru.Id.Equals(u.Id))).ToList();

        return newLdapUsers;
    }

    private async Task RemoveOldDbGroupsAsync(List<GroupInfo> ldapGroups)
    {
        var percentage = GetProgress();

        var removedDbLdapGroups =
            (await _userManager.GetGroupsAsync())
            .Where(g => g.Sid != null && ldapGroups.FirstOrDefault(lg => g.Sid.Equals(lg.Sid)) == null)
            .ToList();

        if (removedDbLdapGroups.Count == 0)
        {
            return;
        }

        const double percents = 10;

        var step = percents / removedDbLdapGroups.Count;

        var index = 0;
        var count = removedDbLdapGroups.Count;

        foreach (var groupInfo in removedDbLdapGroups)
        {
            await SetProgress(Convert.ToInt32(percentage),
                currentSource: $"({++index}/{count}): {groupInfo.Name}");

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    await _userManager.DeleteGroupAsync(groupInfo.ID);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    _ldapChanges.SetRemoveGroupChange(groupInfo);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }
    }

    private async Task<List<UserInfo>> SyncGroupsUsers(List<UserInfo> uniqueLdapGroupUsers)
    {
        const double percents = 30;

        var step = percents / uniqueLdapGroupUsers.Count;

        var percentage = GetProgress();

        var newUniqueLdapGroupUsers = new List<UserInfo>();

        var index = 0;
        var count = uniqueLdapGroupUsers.Count;

        int i, len;
        for (i = 0, len = uniqueLdapGroupUsers.Count; i < len; i++)
        {
            var ldapGroupUser = uniqueLdapGroupUsers[i];

            await SetProgress(Convert.ToInt32(percentage),
                currentSource:
                $"({++index}/{count}): {_userFormatter.GetUserName(ldapGroupUser)}");

            UserInfo user;
            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    user = await _ldapUserManager.SyncLDAPUserAsync(ldapGroupUser, uniqueLdapGroupUsers);
                    if (!Equals(user, Core.Users.Constants.LostUser))
                    {
                        newUniqueLdapGroupUsers.Add(user);
                    }

                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    var wrapper = await _ldapUserManager.GetLDAPSyncUserChangeAsync(ldapGroupUser, uniqueLdapGroupUsers);
                    user = wrapper.UserInfo;
                    var changes = wrapper.LdapChangeCollection;
                    if (!Equals(user, Core.Users.Constants.LostUser))
                    {
                        newUniqueLdapGroupUsers.Add(user);
                    }

                    _ldapChanges.AddRange(changes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }

        return newUniqueLdapGroupUsers;
    }

    private async Task<(Dictionary<GroupInfo, List<UserInfo>>, List<UserInfo>)> GetGroupsUsersAsync(List<GroupInfo> ldapGroups)
    {
        var uniqueLdapGroupUsers = new List<UserInfo>();

        var listGroupsUsers = new Dictionary<GroupInfo, List<UserInfo>>();

        foreach (var ldapGroup in ldapGroups)
        {
            var ldapGroupUsers = await _novellLdapUserImporter.GetGroupUsersAsync(ldapGroup);

            listGroupsUsers.Add(ldapGroup, ldapGroupUsers);

            foreach (var ldapGroupUser in ldapGroupUsers)
            {
                if (!uniqueLdapGroupUsers.Any(u => u.Sid.Equals(ldapGroupUser.Sid)))
                {
                    uniqueLdapGroupUsers.Add(ldapGroupUser);
                }
            }
        }

        return (listGroupsUsers, uniqueLdapGroupUsers);
    }

    private double GetProgress()
    {
        return Percentage;
    }

    private async Task SetProgress(int? currentPercent = null, string currentStatus = null, string currentSource = null)
    {
        if (!currentPercent.HasValue && currentStatus == null && currentSource == null)
        {
            return;
        }

        if (currentPercent.HasValue)
        {
            Percentage = currentPercent.Value;
        }

        if (currentStatus != null)
        {
            Result = currentStatus;
        }

        if (currentSource != null)
        {
            Source = currentSource;
        }

        _logger.InfoProgress(Percentage, Result, Source);

        await PublishChanges();
    }

    private void InitDisturbedTask()
    {
        Finished = false;
        CertRequest = null;
    }

    private async Task PrepareSettingsAsync(LdapSettings settings)
    {
        if (settings == null)
        {
            _logger.ErrorWrongLdapSettings();
            Error = _resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (!settings.EnableLdapAuthentication)
        {
            settings.Password = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.Server))
        {
            settings.Server = settings.Server.Trim();
        }
        else
        {
            _logger.ErrorServerIsNullOrEmpty();
            Error = _resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (!settings.Server.StartsWith("LDAP://"))
        {
            settings.Server = "LDAP://" + settings.Server.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.UserDN))
        {
            settings.UserDN = settings.UserDN.Trim();
        }
        else
        {
            _logger.ErrorUserDnIsNullOrEmpty();
            Error = _resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.LoginAttribute))
        {
            settings.LoginAttribute = settings.LoginAttribute.Trim();
        }
        else
        {
            _logger.ErrorLoginAttributeIsNullOrEmpty();
            Error = _resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.UserFilter))
        {
            settings.UserFilter = settings.UserFilter.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.FirstNameAttribute))
        {
            settings.FirstNameAttribute = settings.FirstNameAttribute.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.SecondNameAttribute))
        {
            settings.SecondNameAttribute = settings.SecondNameAttribute.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.MailAttribute))
        {
            settings.MailAttribute = settings.MailAttribute.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.TitleAttribute))
        {
            settings.TitleAttribute = settings.TitleAttribute.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.MobilePhoneAttribute))
        {
            settings.MobilePhoneAttribute = settings.MobilePhoneAttribute.Trim();
        }

        if (settings.GroupMembership)
        {
            if (!string.IsNullOrWhiteSpace(settings.GroupDN))
            {
                settings.GroupDN = settings.GroupDN.Trim();
            }
            else
            {
                _logger.ErrorGroupDnIsNullOrEmpty();
                Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.GroupFilter))
            {
                settings.GroupFilter = settings.GroupFilter.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.GroupAttribute))
            {
                settings.GroupAttribute = settings.GroupAttribute.Trim();
            }
            else
            {
                _logger.ErrorGroupAttributeIsNullOrEmpty();
                Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.UserAttribute))
            {
                settings.UserAttribute = settings.UserAttribute.Trim();
            }
            else
            {
                _logger.ErrorUserAttributeIsNullOrEmpty();
                Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }
        }

        if (!settings.Authentication)
        {
            settings.Password = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.Login))
        {
            settings.Login = settings.Login.Trim();
        }
        else
        {
            _logger.ErrorloginIsNullOrEmpty();
            Error = _resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (settings.PasswordBytes == null || settings.PasswordBytes.Length == 0)
        {
            if (!string.IsNullOrEmpty(settings.Password))
            {
                settings.PasswordBytes = await _novellLdapHelper.GetPasswordBytesAsync(settings.Password);

                if (settings.PasswordBytes == null)
                {
                    _logger.ErrorPasswordBytesIsNullOrEmpty();
                    Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                    return;
                }
            }
            else
            {
                _logger.ErrorPasswordIsNullOrEmpty();
                Error = _resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }
        }

        settings.Password = string.Empty;
    }

    private string GetError(LdapSettingsStatus result)
    {
        return result switch
        {
            LdapSettingsStatus.Ok => string.Empty,
            LdapSettingsStatus.WrongServerOrPort => _resource.LdapSettingsErrorWrongServerOrPort,
            LdapSettingsStatus.WrongUserDn => _resource.LdapSettingsErrorWrongUserDn,
            LdapSettingsStatus.IncorrectLDAPFilter => _resource.LdapSettingsErrorIncorrectLdapFilter,
            LdapSettingsStatus.UsersNotFound => _resource.LdapSettingsErrorUsersNotFound,
            LdapSettingsStatus.WrongLoginAttribute => _resource.LdapSettingsErrorWrongLoginAttribute,
            LdapSettingsStatus.WrongGroupDn => _resource.LdapSettingsErrorWrongGroupDn,
            LdapSettingsStatus.IncorrectGroupLDAPFilter => _resource.LdapSettingsErrorWrongGroupFilter,
            LdapSettingsStatus.GroupsNotFound => _resource.LdapSettingsErrorGroupsNotFound,
            LdapSettingsStatus.WrongGroupAttribute => _resource.LdapSettingsErrorWrongGroupAttribute,
            LdapSettingsStatus.WrongUserAttribute => _resource.LdapSettingsErrorWrongUserAttribute,
            LdapSettingsStatus.WrongGroupNameAttribute => _resource.LdapSettingsErrorWrongGroupNameAttribute,
            LdapSettingsStatus.CredentialsNotValid => _resource.LdapSettingsErrorCredentialsNotValid,
            LdapSettingsStatus.ConnectError => _resource.LdapSettingsConnectError,
            LdapSettingsStatus.StrongAuthRequired => _resource.LdapSettingsStrongAuthRequired,
            LdapSettingsStatus.WrongSidAttribute => _resource.LdapSettingsWrongSidAttribute,
            LdapSettingsStatus.TlsNotSupported => _resource.LdapSettingsTlsNotSupported,
            LdapSettingsStatus.DomainNotFound => _resource.LdapSettingsErrorDomainNotFound,
            LdapSettingsStatus.CertificateRequest => _resource.LdapSettingsStatusCertificateVerification,
            _ => _resource.LdapSettingsErrorUnknownError
        };
    }
}