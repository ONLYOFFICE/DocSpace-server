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

using Constants = ASC.Core.Configuration.Constants;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.ActiveDirectory.ComplexOperations;
[Transient(Additional = typeof(LdapOperationExtension))]
public class LdapOperationJob(TenantManager tenantManager,
        SecurityContext securityContext,
        LdapUserManager ldapUserManager,
        NovellLdapHelper novellLdapHelper,
        NovellLdapUserImporter novellLdapUserImporter,
        LdapChangeCollection ldapChanges,
        UserFormatter userFormatter,
        SettingsManager settingsManager,
        UserPhotoManager userPhotoManager,
        WebItemSecurity webItemSecurity,
        UserManager userManager,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        NovellLdapSettingsChecker novellLdapSettingsChecker,
        ILogger<LdapOperationJob> logger)
    : DistributedTaskProgress
{
    private string _culture;

    public LdapSettings LDAPSettings { get; private set; }
    protected string Source { get; private set; }
    protected new string Status { get; set; }
    protected string Error { get; set; }
    protected string Warning { get; set; }

    private int? _tenantId;
    public int TenantId
    {
        get
        {
            return _tenantId ?? this[nameof(_tenantId)];
        }
        private set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }

    public LdapOperationType OperationType { get; private set; }
    public static LdapLocalization Resource { get; private set; }


    private readonly LdapUserImporter _ldapUserImporter = novellLdapUserImporter;

    private UserInfo _currentUser;

    public async Task InitJobAsync(
       LdapSettings settings,
       Tenant tenant,
       LdapOperationType operationType,
       LdapLocalization resource,
       string userId)
    {
        _currentUser = userId != null ? await userManager.GetUsersAsync(Guid.Parse(userId)) : null;

        TenantId = tenant.Id;
        tenantManager.SetCurrentTenant(tenant);

        OperationType = operationType;

        _culture = CultureInfo.CurrentCulture.Name;

        LDAPSettings = settings;

        Source = "";
        Percentage = 0;
        Status = "";
        Error = "";
        Warning = "";

        Resource = resource ?? new LdapLocalization();
        ldapUserManager.Init(Resource);

        InitDisturbedTask();
    }

    protected override async Task DoJob()
    {
        try
        {
            await securityContext.AuthenticateMeAsync(Constants.CoreSystem);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(_culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(_culture);

            if (LDAPSettings == null)
            {
                Error = Resource.LdapSettingsErrorCantGetLdapSettings;
                logger.ErrorSaveDefaultLdapSettings();
                return;
            }

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.SaveTest:

                    logger.InfoStartOperation(Enum.GetName(typeof(LdapOperationType), OperationType));

                    SetProgress(1, Resource.LdapSettingsStatusCheckingLdapSettings);

                    logger.DebugPrepareSettings();

                    PrepareSettings(LDAPSettings);

                    if (!string.IsNullOrEmpty(Error))
                    {
                        logger.DebugPrepareSettingsError(Error);
                        return;
                    }

                    _ldapUserImporter.Init(LDAPSettings, Resource);

                    if (LDAPSettings.EnableLdapAuthentication)
                    {
                        novellLdapSettingsChecker.Init(_ldapUserImporter);

                        SetProgress(5, Resource.LdapSettingsStatusLoadingBaseInfo);

                        var result = novellLdapSettingsChecker.CheckSettings();

                        if (result != LdapSettingsStatus.Ok)
                        {
                            if (result == LdapSettingsStatus.CertificateRequest)
                            {
                                this[LdapTaskProperty.CERT_REQUEST] = novellLdapSettingsChecker.CertificateConfirmRequest;
                            }

                            Error = GetError(result);

                            logger.DebugCheckSettingsError(Error);

                            return;
                        }
                    }

                    break;
                case LdapOperationType.Sync:
                case LdapOperationType.SyncTest:
                    logger.InfoStartOperation(Enum.GetName(typeof(LdapOperationType), OperationType));

                    _ldapUserImporter.Init(LDAPSettings, Resource);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await Do();
        }
        catch (AuthorizingException authError)
        {
            Error = Resource.ErrorAccessDenied;
            logger.ErrorAuthorizing(Error, new SecurityException(Error, authError));
        }
        catch (AggregateException ae)
        {
            ae.Flatten().Handle(e => e is TaskCanceledException or OperationCanceledException);
        }
        catch (TenantQuotaException e)
        {
            Error = Resource.LdapSettingsTenantQuotaSettled;
            logger.ErrorTenantQuota(e);
        }
        catch (FormatException e)
        {
            Error = Resource.LdapSettingsErrorCantCreateUsers;
            logger.ErrorFormatException(e);
        }
        catch (Exception e)
        {
            Error = Resource.LdapSettingsInternalServerError;
            logger.ErrorInternal(e);
        }
        finally
        {
            try
            {
                this[LdapTaskProperty.FINISHED] = true;
                PublishTaskInfo();
                securityContext.Logout();
            }
            catch (Exception ex)
            {
                logger.ErrorLdapOperationFinalizationlProblem(ex);
            }
        }
    }

    private async Task Do()
    {
        try
        {
            if (OperationType == LdapOperationType.Save)
            {
                SetProgress(10, Resource.LdapSettingsStatusSavingSettings);

                LDAPSettings.IsDefault = LDAPSettings.Equals(LDAPSettings.GetDefault());

                if (!await settingsManager.SaveAsync(LDAPSettings))
                {
                    logger.ErrorSaveLdapSettings();
                    Error = Resource.LdapSettingsErrorCantSaveLdapSettings;
                    return;
                }
            }

            if (LDAPSettings.EnableLdapAuthentication)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("SyncLDAP()");
                    sb.AppendLine(string.Format("Server: {0}:{1}", LDAPSettings.Server, LDAPSettings.PortNumber));
                    sb.AppendLine("UserDN: " + LDAPSettings.UserDN);
                    sb.AppendLine("LoginAttr: " + LDAPSettings.LoginAttribute);
                    sb.AppendLine("UserFilter: " + LDAPSettings.UserFilter);
                    sb.AppendLine("Groups: " + LDAPSettings.GroupMembership);
                    if (LDAPSettings.GroupMembership)
                    {
                        sb.AppendLine("GroupDN: " + LDAPSettings.GroupDN);
                        sb.AppendLine("UserAttr: " + LDAPSettings.UserAttribute);
                        sb.AppendLine("GroupFilter: " + LDAPSettings.GroupFilter);
                        sb.AppendLine("GroupName: " + LDAPSettings.GroupNameAttribute);
                        sb.AppendLine("GroupMember: " + LDAPSettings.GroupAttribute);
                    }

                    logger.DebugLdapSettings(sb.ToString());
                }

                await SyncLDAPAsync();

                if (!string.IsNullOrEmpty(Error))
                {
                    return;
                }
            }
            else
            {
                logger.DebugTurnOffLDAP();

                await TurnOffLDAPAsync();
                var ldapCurrentUserPhotos = (await settingsManager.LoadAsync<LdapCurrentUserPhotos>()).GetDefault();
                await settingsManager.SaveAsync(ldapCurrentUserPhotos);

                var ldapCurrentAcccessSettings = (await settingsManager.LoadAsync<LdapCurrentAcccessSettings>()).GetDefault();
                await settingsManager.SaveAsync(ldapCurrentAcccessSettings);
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
            logger.ErrorCheckSettings(
                LDAPSettings.AcceptCertificate, LDAPSettings.AcceptCertificateHash, ex);
            Error = Resource.LdapSettingsStatusCertificateVerification;

            //TaskInfo.SetProperty(CERT_REQUEST, ex.CertificateConfirmRequest);
        }
        catch (TenantQuotaException e)
        {
            logger.ErrorTenantQuota(e);
            Error = Resource.LdapSettingsTenantQuotaSettled;
        }
        catch (FormatException e)
        {
            logger.ErrorFormatException(e);
            Error = Resource.LdapSettingsErrorCantCreateUsers;
        }
        catch (Exception e)
        {
            logger.ErrorInternal(e);
            Error = Resource.LdapSettingsInternalServerError;
        }
        finally
        {
            SetProgress(99, Resource.LdapSettingsStatusDisconnecting, "");
        }

        SetProgress(100, OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest
            ? JsonSerializer.Serialize(ldapChanges)
            : "", "");
    }

    private async Task TurnOffLDAPAsync()
    {
        const double percents = 48;

        SetProgress((int)percents, Resource.LdapSettingsModifyLdapUsers);

        var existingLDAPUsers = (await userManager.GetUsersAsync(EmployeeStatus.All)).Where(u => u.Sid != null).ToList();

        var step = percents / existingLDAPUsers.Count;

        var percentage = GetProgress();

        var index = 0;
        var count = existingLDAPUsers.Count;

        foreach (var existingLDAPUser in existingLDAPUsers)
        {
            SetProgress(Convert.ToInt32(percentage),
                currentSource:
                    string.Format("({0}/{1}): {2}", ++index, count,
                        userFormatter.GetUserName(existingLDAPUser, DisplayUserNameFormat.Default)));

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    existingLDAPUser.Sid = null;
                    existingLDAPUser.ConvertExternalContactsToOrdinary();

                    logger.DebugSaveUserInfo(existingLDAPUser.GetUserInfoString());

                    await userManager.UpdateUserInfoAsync(existingLDAPUser);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    ldapChanges.SetSaveAsPortalUserChange(existingLDAPUser);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            percentage += step;
        }
    }

    private async Task SyncLDAPAsync()
    {
        var currentDomainSettings = await settingsManager.LoadAsync<LdapCurrentDomain>();

        if (string.IsNullOrEmpty(currentDomainSettings.CurrentDomain) || currentDomainSettings.CurrentDomain != _ldapUserImporter.LDAPDomain)
        {
            currentDomainSettings.CurrentDomain = _ldapUserImporter.LDAPDomain;
            await settingsManager.SaveAsync(currentDomainSettings);
        }

        if (!LDAPSettings.GroupMembership)
        {
            logger.DebugSyncLDAPUsers();

            await SyncLDAPUsersAsync();
        }
        else
        {
            logger.DebugSyncLDAPUsersInGroups();

            await SyncLDAPUsersInGroupsAsync();
        }

        await SyncLdapAvatarAsync();

        await SyncLdapAccessRights();
    }

    private async Task SyncLdapAvatarAsync()
    {
        SetProgress(90, Resource.LdapSettingsStatusUpdatingUserPhotos);

        if (!LDAPSettings.LdapMapping.ContainsKey(LdapSettings.MappingFields.AvatarAttribute))
        {
            var ph = await settingsManager.LoadAsync<LdapCurrentUserPhotos>();

            if (ph.CurrentPhotos == null || !ph.CurrentPhotos.Any())
            {
                return;
            }

            foreach (var guid in ph.CurrentPhotos.Keys)
            {
                logger.InfoSyncLdapAvatarsRemovingPhoto(guid);
                await userPhotoManager.RemovePhotoAsync(guid);
                await userPhotoManager.ResetThumbnailSettingsAsync(guid);
            }

            ph.CurrentPhotos = null;
            await settingsManager.SaveAsync(ph);
            return;
        }

        var photoSettings = await settingsManager.LoadAsync<LdapCurrentUserPhotos>();

        if (photoSettings.CurrentPhotos == null)
        {
            photoSettings.CurrentPhotos = new Dictionary<Guid, string>();
        }

        var ldapUsers = _ldapUserImporter.AllDomainUsers.Where(x => !x.IsDisabled);
        var step = 5.0 / ldapUsers.Count();
        var currentPercent = 90.0;
        foreach (var ldapUser in ldapUsers)
        {
            var image = ldapUser.GetValue(LDAPSettings.LdapMapping[LdapSettings.MappingFields.AvatarAttribute], true);

            if (image == null || image.GetType() != typeof(byte[]))
            {
                continue;
            }

            string hash;
            using (var md5 = MD5.Create())
            {
                hash = Convert.ToBase64String(md5.ComputeHash((byte[])image));
            }

            var user = await userManager.GetUserBySidAsync(ldapUser.Sid);

            logger.DebugSyncLdapAvatarsFoundPhoto(ldapUser.Sid);

            if (photoSettings.CurrentPhotos.ContainsKey(user.Id) && photoSettings.CurrentPhotos[user.Id] == hash)
            {
                logger.DebugSyncLdapAvatarsSkipping();
                continue;
            }

            try
            {
                SetProgress((int)(currentPercent += step),
                    string.Format("{0}: {1}", Resource.LdapSettingsStatusSavingUserPhoto, userFormatter.GetUserName(user, DisplayUserNameFormat.Default)));

                await userPhotoManager.SyncPhotoAsync(user.Id, (byte[])image);

                if (photoSettings.CurrentPhotos.ContainsKey(user.Id))
                {
                    photoSettings.CurrentPhotos[user.Id] = hash;
                }
                else
                {
                    photoSettings.CurrentPhotos.Add(user.Id, hash);
                }
            }
            catch
            {
                logger.DebugSyncLdapAvatarsCouldNotSavePhoto(user.Id);
                if (photoSettings.CurrentPhotos.ContainsKey(user.Id))
                {
                    photoSettings.CurrentPhotos.Remove(user.Id);
                }
            }
        }

        await settingsManager.SaveAsync(photoSettings);
    }

    private async Task SyncLdapAccessRights()
    {
        SetProgress(95, Resource.LdapSettingsStatusUpdatingAccessRights);

        var currentUserRights = new List<LdapSettings.AccessRight>();
        await TakeUsersRightsAsync(_currentUser != null ? currentUserRights : null);

        if (LDAPSettings.GroupMembership && LDAPSettings.AccessRights is { Count: > 0 })
        {
            await GiveUsersRights(LDAPSettings.AccessRights, _currentUser != null ? currentUserRights : null);
        }

        if (currentUserRights.Count > 0)
        {
            Warning = Resource.LdapSettingsErrorLostRights;
        }

        await settingsManager.SaveAsync(LDAPSettings);
    }

    private async Task TakeUsersRightsAsync(List<LdapSettings.AccessRight> currentUserRights)
    {
        var current = await settingsManager.LoadAsync<LdapCurrentAcccessSettings>();

        if (current.CurrentAccessRights == null || !current.CurrentAccessRights.Any())
        {
            logger.DebugAccessRightsIsEmpty();
            return;
        }

        SetProgress(95, Resource.LdapSettingsStatusRemovingOldRights);
        foreach (var right in current.CurrentAccessRights)
        {
            foreach (var user in right.Value)
            {
                var userId = Guid.Parse(user);
                if (_currentUser != null && _currentUser.Id == userId)
                {
                    logger.DebugAttemptingTakeAdminRights(user);
                    currentUserRights?.Add(right.Key);
                }
                else
                {
                    logger.DebugTakingAdminRights(right.Key, user);
                    await webItemSecurity.SetProductAdministrator(LdapSettings.AccessRightsGuids[right.Key], userId, false);
                }
            }
        }

        current.CurrentAccessRights = null;
        await settingsManager.SaveAsync(current);
    }

    private async Task GiveUsersRights(Dictionary<LdapSettings.AccessRight, string> accessRightsSettings, List<LdapSettings.AccessRight> currentUserRights)
    {
        var current = await settingsManager.LoadAsync<LdapCurrentAcccessSettings>();
        var currentAccessRights = new Dictionary<LdapSettings.AccessRight, List<string>>();
        var usersWithRightsFlat = current.CurrentAccessRights == null ? new List<string>() : current.CurrentAccessRights.SelectMany(x => x.Value).Distinct().ToList();

        var step = 3.0 / accessRightsSettings.Count;
        var currentPercent = 95.0;
        foreach (var access in accessRightsSettings)
        {
            currentPercent += step;
            var ldapGroups = _ldapUserImporter.FindGroupsByAttribute(LDAPSettings.GroupNameAttribute, access.Value.Split(',').Select(x => x.Trim()));

            if (!ldapGroups.Any())
            {
                logger.DebugGiveUsersRightsNoLdapGroups(access.Key);
                continue;
            }

            foreach (var ldapGr in ldapGroups)
            {
                var gr = await userManager.GetGroupInfoBySidAsync(ldapGr.Sid);

                if (gr == null)
                {
                    logger.DebugGiveUsersRightsCouldNotFindPortalGroup(ldapGr.Sid);
                    continue;
                }

                var users = await userManager.GetUsersByGroupAsync(gr.ID);

                logger.DebugGiveUsersRightsFoundUsersForGroup(users.Count(), gr.Name, gr.ID);


                foreach (var user in users)
                {
                    if (!user.Equals(Core.Users.Constants.LostUser) && !await userManager.IsUserAsync(user))
                    {
                        if (!usersWithRightsFlat.Contains(user.Id.ToString()))
                        {
                            usersWithRightsFlat.Add(user.Id.ToString());

                            var cleared = false;

                            foreach (var r in Enum.GetValues(typeof(LdapSettings.AccessRight)).Cast<LdapSettings.AccessRight>())
                            {
                                var prodId = LdapSettings.AccessRightsGuids[r];

                                if (await webItemSecurity.IsProductAdministratorAsync(prodId, user.Id))
                                {
                                    cleared = true;
                                    await webItemSecurity.SetProductAdministrator(prodId, user.Id, false);
                                }
                            }

                            if (cleared)
                            {
                                logger.DebugGiveUsersRightsClearedAndAddedRights(user.DisplayUserName(displayUserSettingsHelper));
                            }
                        }

                        if (!currentAccessRights.ContainsKey(access.Key))
                        {
                            currentAccessRights.Add(access.Key, new List<string>());
                        }
                        currentAccessRights[access.Key].Add(user.Id.ToString());

                        SetProgress((int)currentPercent,
                            string.Format(Resource.LdapSettingsStatusGivingRights, userFormatter.GetUserName(user, DisplayUserNameFormat.Default), access.Key));
                        await webItemSecurity.SetProductAdministrator(LdapSettings.AccessRightsGuids[access.Key], user.Id, true);

                        if (currentUserRights != null && currentUserRights.Contains(access.Key))
                        {
                            currentUserRights.Remove(access.Key);
                        }
                    }
                }
            }
        }

        current.CurrentAccessRights = currentAccessRights;
        await settingsManager.SaveAsync(current);
    }

    private async Task SyncLDAPUsersAsync()
    {
        SetProgress(15, Resource.LdapSettingsStatusGettingUsersFromLdap);

        var ldapUsers = await _ldapUserImporter.GetDiscoveredUsersByAttributesAsync();

        if (!ldapUsers.Any())
        {
            Error = Resource.LdapSettingsErrorUsersNotFound;
            return;
        }

        logger.DebugGetDiscoveredUsersByAttributes(_ldapUserImporter.AllDomainUsers.Count);

        SetProgress(20, Resource.LdapSettingsStatusRemovingOldUsers, "");

        ldapUsers = await RemoveOldDbUsersAsync(ldapUsers);

        SetProgress(30,
            OperationType is LdapOperationType.Save or LdapOperationType.SaveTest
                ? Resource.LdapSettingsStatusSavingUsers
                : Resource.LdapSettingsStatusSyncingUsers,
            "");

        await SyncDbUsers(ldapUsers);

        SetProgress(70, Resource.LdapSettingsStatusRemovingOldGroups, "");

        await RemoveOldDbGroupsAsync(new List<GroupInfo>()); // Remove all db groups with sid
    }

    private async Task SyncLDAPUsersInGroupsAsync()
    {
        SetProgress(15, Resource.LdapSettingsStatusGettingGroupsFromLdap);

        var ldapGroups = _ldapUserImporter.GetDiscoveredGroupsByAttributes();

        if (!ldapGroups.Any())
        {
            Error = Resource.LdapSettingsErrorGroupsNotFound;
            return;
        }

        logger.DebugGetDiscoveredGroupsByAttributes(_ldapUserImporter.AllDomainGroups.Count);

        SetProgress(20, Resource.LdapSettingsStatusGettingUsersFromLdap);

        var (ldapGroupsUsers, uniqueLdapGroupUsers) = await GetGroupsUsersAsync(ldapGroups);

        if (!uniqueLdapGroupUsers.Any())
        {
            Error = Resource.LdapSettingsErrorUsersNotFound;
            return;
        }

        logger.DebugGetGroupsUsers(_ldapUserImporter.AllDomainUsers.Count);

        SetProgress(30,
            OperationType is LdapOperationType.Save or LdapOperationType.SaveTest
                ? Resource.LdapSettingsStatusSavingUsers
                : Resource.LdapSettingsStatusSyncingUsers,
            "");

        var newUniqueLdapGroupUsers = await SyncGroupsUsers(uniqueLdapGroupUsers);

        SetProgress(60, Resource.LdapSettingsStatusSavingGroups, "");

        await SyncDbGroups(ldapGroupsUsers);

        SetProgress(80, Resource.LdapSettingsStatusRemovingOldGroups, "");

        await RemoveOldDbGroupsAsync(ldapGroups);

        SetProgress(90, Resource.LdapSettingsStatusRemovingOldUsers, "");

        await RemoveOldDbUsersAsync(newUniqueLdapGroupUsers);
    }

    private async Task SyncDbGroups(Dictionary<GroupInfo, List<UserInfo>> ldapGroupsWithUsers)
    {
        const double percents = 20;

        var step = percents / ldapGroupsWithUsers.Count;

        var percentage = GetProgress();

        if (!ldapGroupsWithUsers.Any())
        {
            return;
        }

        var gIndex = 0;
        var gCount = ldapGroupsWithUsers.Count;

        foreach (var ldapGroupWithUsers in ldapGroupsWithUsers)
        {
            var ldapGroup = ldapGroupWithUsers.Key;

            var ldapGroupUsers = ldapGroupWithUsers.Value;

            ++gIndex;

            SetProgress(Convert.ToInt32(percentage),
                currentSource:
                    string.Format("({0}/{1}): {2}", gIndex,
                        gCount, ldapGroup.Name));

            var dbLdapGroup = await userManager.GetGroupInfoBySidAsync(ldapGroup.Sid);

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
        if (!ldapGroupUsers.Any()) // Skip empty groups
        {
            if (OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest)
            {
                ldapChanges.SetSkipGroupChange(ldapGroup);
            }

            return;
        }

        var groupMembersToAdd = await ldapGroupUsers.ToAsyncEnumerable().SelectAwait(async ldapGroupUser => await SearchDbUserBySidAsync(ldapGroupUser.Sid))
                .Where(userBySid => !Equals(userBySid, Core.Users.Constants.LostUser))
                .ToListAsync();

        if (groupMembersToAdd.Any())
        {
            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    ldapGroup = await userManager.SaveGroupInfoAsync(ldapGroup);

                    var index = 0;
                    var count = groupMembersToAdd.Count;

                    foreach (var userBySid in groupMembersToAdd)
                    {
                        SetProgress(
                            currentSource:
                                string.Format("({0}/{1}): {2}, {3} ({4}/{5}): {6}", gIndex,
                                    gCount, ldapGroup.Name,
                                    Resource.LdapSettingsStatusAddingGroupUser,
                                    ++index, count,
                                    userFormatter.GetUserName(userBySid, DisplayUserNameFormat.Default)));

                        await userManager.AddUserIntoGroupAsync(userBySid.Id, ldapGroup.ID);
                    }
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    ldapChanges.SetAddGroupChange(ldapGroup);
                    ldapChanges.SetAddGroupMembersChange(ldapGroup, groupMembersToAdd);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            if (OperationType is LdapOperationType.SaveTest or LdapOperationType.SyncTest)
            {
                ldapChanges.SetSkipGroupChange(ldapGroup);
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

    private async Task UpdateDbGroupAsync(GroupInfo dbLdapGroup, GroupInfo ldapGroup, List<UserInfo> ldapGroupUsers, int gIndex,
        int gCount)
    {
        SetProgress(currentSource:
            string.Format("({0}/{1}): {2}", gIndex, gCount, ldapGroup.Name));

        var dbGroupMembers =
                    (await userManager.GetUsersByGroupAsync(dbLdapGroup.ID, EmployeeStatus.All))
                        .Where(u => u.Sid != null)
                        .ToList();

        var groupMembersToRemove =
            dbGroupMembers.Where(
                dbUser => ldapGroupUsers.FirstOrDefault(lu => dbUser.Sid.Equals(lu.Sid)) == null).ToList();

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

                    dbLdapGroup = await userManager.SaveGroupInfoAsync(dbLdapGroup);
                }

                var index = 0;
                var count = groupMembersToRemove.Count;

                foreach (var dbUser in groupMembersToRemove)
                {
                    SetProgress(
                        currentSource:
                            string.Format("({0}/{1}): {2}, {3} ({4}/{5}): {6}", gIndex, gCount,
                                dbLdapGroup.Name,
                                Resource.LdapSettingsStatusRemovingGroupUser,
                                ++index, count,
                                userFormatter.GetUserName(dbUser, DisplayUserNameFormat.Default)));

                    await userManager.RemoveUserFromGroupAsync(dbUser.Id, dbLdapGroup.ID);
                }

                index = 0;
                count = groupMembersToAdd.Count;

                foreach (var userInfo in groupMembersToAdd)
                {
                    SetProgress(
                        currentSource:
                            string.Format("({0}/{1}): {2}, {3} ({4}/{5}): {6}", gIndex, gCount,
                                ldapGroup.Name,
                                Resource.LdapSettingsStatusAddingGroupUser,
                                ++index, count,
                                userFormatter.GetUserName(userInfo, DisplayUserNameFormat.Default)));

                    await userManager.AddUserIntoGroupAsync(userInfo.Id, dbLdapGroup.ID);
                }

                if (dbGroupMembers.All(dbUser => groupMembersToRemove.Exists(u => u.Id.Equals(dbUser.Id)))
                    && !groupMembersToAdd.Any())
                {
                    SetProgress(currentSource:
                        string.Format("({0}/{1}): {2}", gIndex, gCount, dbLdapGroup.Name));

                    await userManager.DeleteGroupAsync(dbLdapGroup.ID);
                }

                break;
            case LdapOperationType.SaveTest:
            case LdapOperationType.SyncTest:
                if (NeedUpdateGroup(dbLdapGroup, ldapGroup))
                {
                    ldapChanges.SetUpdateGroupChange(ldapGroup);
                }

                if (groupMembersToRemove.Any())
                {
                    ldapChanges.SetRemoveGroupMembersChange(dbLdapGroup, groupMembersToRemove);
                }

                if (groupMembersToAdd.Any())
                {
                    ldapChanges.SetAddGroupMembersChange(dbLdapGroup, groupMembersToAdd);
                }

                if (dbGroupMembers.All(dbUser => groupMembersToRemove.Exists(u => u.Id.Equals(dbUser.Id)))
                    && !groupMembersToAdd.Any())
                {
                    ldapChanges.SetRemoveGroupChange(dbLdapGroup, logger);
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

        var foundUser = await userManager.GetUserBySidAsync(sid);

        return foundUser;
    }

    private async Task SyncDbUsers(List<UserInfo> ldapUsers)
    {
        const double percents = 35;

        var step = percents / ldapUsers.Count;

        var percentage = GetProgress();

        if (!ldapUsers.Any())
        {
            return;
        }

        var index = 0;
        var count = ldapUsers.Count;

        foreach (var userInfo in ldapUsers)
        {
            SetProgress(Convert.ToInt32(percentage),
                currentSource:
                    string.Format("({0}/{1}): {2}", ++index, count,
                        userFormatter.GetUserName(userInfo, DisplayUserNameFormat.Default)));

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    await ldapUserManager.SyncLDAPUserAsync(userInfo, ldapUsers);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    var changes = (await ldapUserManager.GetLDAPSyncUserChangeAsync(userInfo, ldapUsers)).LdapChangeCollection;
                    ldapChanges.AddRange(changes);
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
        var dbLdapUsers = (await userManager.GetUsersAsync(EmployeeStatus.All)).Where(u => u.Sid != null).ToList();

        if (!dbLdapUsers.Any())
        {
            return ldapUsers;
        }

        var removedUsers =
            dbLdapUsers.Where(u => ldapUsers.FirstOrDefault(lu => u.Sid.Equals(lu.Sid)) == null).ToList();

        if (!removedUsers.Any())
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
            SetProgress(Convert.ToInt32(percentage),
                currentSource:
                    string.Format("({0}/{1}): {2}", ++index, count,
                        userFormatter.GetUserName(removedUser, DisplayUserNameFormat.Default)));

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    removedUser.Sid = null;
                    if (!removedUser.IsOwner(await tenantManager.GetCurrentTenantAsync()) && !(_currentUser != null && _currentUser.Id == removedUser.Id && await userManager.IsDocSpaceAdminAsync(removedUser)))
                    {
                        removedUser.Status = EmployeeStatus.Terminated; // Disable user on portal
                    }
                    else
                    {
                        Warning = Resource.LdapSettingsErrorRemovedYourself;
                        logger.DebugRemoveOldDbUsersAttemptingExcludeYourself(removedUser.Id);
                    }

                    removedUser.ConvertExternalContactsToOrdinary();

                    logger.DebugSaveUserInfo(removedUser.GetUserInfoString());

                    await userManager.UpdateUserInfoAsync(removedUser);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    ldapChanges.SetSaveAsPortalUserChange(removedUser);
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
           (await userManager.GetGroupsAsync())
                .Where(g => g.Sid != null && ldapGroups.FirstOrDefault(lg => g.Sid.Equals(lg.Sid)) == null)
                .ToList();

        if (!removedDbLdapGroups.Any())
        {
            return;
        }

        const double percents = 10;

        var step = percents / removedDbLdapGroups.Count;

        var index = 0;
        var count = removedDbLdapGroups.Count;

        foreach (var groupInfo in removedDbLdapGroups)
        {
            SetProgress(Convert.ToInt32(percentage),
                currentSource: string.Format("({0}/{1}): {2}", ++index, count, groupInfo.Name));

            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    await userManager.DeleteGroupAsync(groupInfo.ID);
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    ldapChanges.SetRemoveGroupChange(groupInfo);
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

            SetProgress(Convert.ToInt32(percentage),
                currentSource:
                    string.Format("({0}/{1}): {2}", ++index, count,
                        userFormatter.GetUserName(ldapGroupUser, DisplayUserNameFormat.Default)));

            UserInfo user;
            switch (OperationType)
            {
                case LdapOperationType.Save:
                case LdapOperationType.Sync:
                    user = await ldapUserManager.SyncLDAPUserAsync(ldapGroupUser, uniqueLdapGroupUsers);
                    if (!Equals(user, Core.Users.Constants.LostUser))
                    {
                        newUniqueLdapGroupUsers.Add(user);
                    }
                    break;
                case LdapOperationType.SaveTest:
                case LdapOperationType.SyncTest:
                    var wrapper = await ldapUserManager.GetLDAPSyncUserChangeAsync(ldapGroupUser, uniqueLdapGroupUsers);
                    user = wrapper.UserInfo;
                    var changes = wrapper.LdapChangeCollection;
                    if (!Equals(user, Core.Users.Constants.LostUser))
                    {
                        newUniqueLdapGroupUsers.Add(user);
                    }
                    ldapChanges.AddRange(changes);
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
            var ldapGroupUsers = await _ldapUserImporter.GetGroupUsersAsync(ldapGroup);

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

    private void SetProgress(int? currentPercent = null, string currentStatus = null, string currentSource = null)
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
            Status = currentStatus;
        }

        if (currentSource != null)
        {
            Source = currentSource;
        }

        logger.InfoProgress(Percentage, Status, Source);

        PublishTaskInfo();
    }
    private void PublishTaskInfo()
    {
        FillDistributedTask();
        PublishChanges();
    }

    private void InitDisturbedTask()
    {
        this[LdapTaskProperty.FINISHED] = false;
        this[LdapTaskProperty.CERT_REQUEST] = null;
        FillDistributedTask();
    }

    private void FillDistributedTask()
    {
        this[LdapTaskProperty.SOURCE] = Source;
        this[LdapTaskProperty.OPERATION_TYPE] = OperationType;
        this[LdapTaskProperty.OWNER] = _tenantId;
        this[LdapTaskProperty.PROGRESS] = Percentage < 100 ? Percentage : 100;
        this[LdapTaskProperty.RESULT] = Status;
        this[LdapTaskProperty.ERROR] = Error;
        this[LdapTaskProperty.WARNING] = Warning;
        //SetProperty(PROCESSED, successProcessed);
    }

    private void PrepareSettings(LdapSettings settings)
    {
        if (settings == null)
        {
            logger.ErrorWrongLdapSettings();
            Error = Resource.LdapSettingsErrorCantGetLdapSettings;
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
            logger.ErrorServerIsNullOrEmpty();
            Error = Resource.LdapSettingsErrorCantGetLdapSettings;
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
            logger.ErrorUserDnIsNullOrEmpty();
            Error = Resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.LoginAttribute))
        {
            settings.LoginAttribute = settings.LoginAttribute.Trim();
        }
        else
        {
            logger.ErrorLoginAttributeIsNullOrEmpty();
            Error = Resource.LdapSettingsErrorCantGetLdapSettings;
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
                logger.ErrorGroupDnIsNullOrEmpty();
                Error = Resource.LdapSettingsErrorCantGetLdapSettings;
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
                logger.ErrorGroupAttributeIsNullOrEmpty();
                Error = Resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.UserAttribute))
            {
                settings.UserAttribute = settings.UserAttribute.Trim();
            }
            else
            {
                logger.ErrorUserAttributeIsNullOrEmpty();
                Error = Resource.LdapSettingsErrorCantGetLdapSettings;
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
            logger.ErrorloginIsNullOrEmpty();
            Error = Resource.LdapSettingsErrorCantGetLdapSettings;
            return;
        }

        if (settings.PasswordBytes == null || !settings.PasswordBytes.Any())
        {
            if (!string.IsNullOrEmpty(settings.Password))
            {
                settings.PasswordBytes = novellLdapHelper.GetPasswordBytes(settings.Password);

                if (settings.PasswordBytes == null)
                {
                    logger.ErrorPasswordBytesIsNullOrEmpty();
                    Error = Resource.LdapSettingsErrorCantGetLdapSettings;
                    return;
                }
            }
            else
            {
                logger.ErrorPasswordIsNullOrEmpty();
                Error = Resource.LdapSettingsErrorCantGetLdapSettings;
                return;
            }
        }

        settings.Password = string.Empty;
    }

    private static string GetError(LdapSettingsStatus result)
    {
        switch (result)
        {
            case LdapSettingsStatus.Ok:
                return string.Empty;
            case LdapSettingsStatus.WrongServerOrPort:
                return Resource.LdapSettingsErrorWrongServerOrPort;
            case LdapSettingsStatus.WrongUserDn:
                return Resource.LdapSettingsErrorWrongUserDn;
            case LdapSettingsStatus.IncorrectLDAPFilter:
                return Resource.LdapSettingsErrorIncorrectLdapFilter;
            case LdapSettingsStatus.UsersNotFound:
                return Resource.LdapSettingsErrorUsersNotFound;
            case LdapSettingsStatus.WrongLoginAttribute:
                return Resource.LdapSettingsErrorWrongLoginAttribute;
            case LdapSettingsStatus.WrongGroupDn:
                return Resource.LdapSettingsErrorWrongGroupDn;
            case LdapSettingsStatus.IncorrectGroupLDAPFilter:
                return Resource.LdapSettingsErrorWrongGroupFilter;
            case LdapSettingsStatus.GroupsNotFound:
                return Resource.LdapSettingsErrorGroupsNotFound;
            case LdapSettingsStatus.WrongGroupAttribute:
                return Resource.LdapSettingsErrorWrongGroupAttribute;
            case LdapSettingsStatus.WrongUserAttribute:
                return Resource.LdapSettingsErrorWrongUserAttribute;
            case LdapSettingsStatus.WrongGroupNameAttribute:
                return Resource.LdapSettingsErrorWrongGroupNameAttribute;
            case LdapSettingsStatus.CredentialsNotValid:
                return Resource.LdapSettingsErrorCredentialsNotValid;
            case LdapSettingsStatus.ConnectError:
                return Resource.LdapSettingsConnectError;
            case LdapSettingsStatus.StrongAuthRequired:
                return Resource.LdapSettingsStrongAuthRequired;
            case LdapSettingsStatus.WrongSidAttribute:
                return Resource.LdapSettingsWrongSidAttribute;
            case LdapSettingsStatus.TlsNotSupported:
                return Resource.LdapSettingsTlsNotSupported;
            case LdapSettingsStatus.DomainNotFound:
                return Resource.LdapSettingsErrorDomainNotFound;
            case LdapSettingsStatus.CertificateRequest:
                return Resource.LdapSettingsStatusCertificateVerification;
            default:
                return Resource.LdapSettingsErrorUnknownError;
        }
    }

    public static class LdapOperationExtension
    {
        public static void Register(DIHelper services)
        {
            services.TryAdd<NovellLdapSettingsChecker>();
            services.TryAdd<LdapChangeCollection>();
        }
    }
}
