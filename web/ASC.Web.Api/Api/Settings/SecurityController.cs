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

namespace ASC.Web.Api.Controllers.Settings;

[ApiEndpoint(Template = "security")]
public class SecurityController(
    GroupSummaryDtoHelper groupSummaryDtoHelper,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    CoreBaseSettings coreBaseSettings,
    MessageService messageService,
    UserManager userManager,
    AuthContext authContext,
    WebItemSecurity webItemSecurity,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    WebItemManagerSecurity webItemManagerSecurity,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    IFusionCache fusionCache,
    PasswordSettingsConverter passwordSettingsConverter,
    PasswordSettingsManager passwordSettingsManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Reports how access to the portal's own modules is configured: for every module identifier sent in `ids`,
    /// whether access is restricted at all and which users and groups are allowed to open the module. Send the
    /// identifiers as repeated `ids` query values; each one has to be a GUID, and anything else is rejected as an
    /// invalid request. Omitting `ids` asks about every module registered in the portal, which on a DocSpace
    /// installation is none, so the answer is then an empty list rather than a failure. Any signed-in member may call
    /// this; anonymous callers are not admitted. The operation is read-only and answers one entry per identifier, in
    /// the order the identifiers were sent. `enabled` is `false` for a module nobody has ever configured, `groups`
    /// and `users` name the subjects the rule was stored for, and `isSubItem` marks a module that hangs under another
    /// one. Users the caller is not allowed to see are left out of `users`, so the same module can come back with
    /// different lists for different callers. Change any of this with `PUT api/2.0/settings/security`.
    /// </remarks>
    /// <summary>
    /// Get module access settings
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The access configuration of every module identifier asked about: the enabled flag, the allowed groups, the allowed users the caller may see, and the sub-module flag", typeof(IAsyncEnumerable<SecurityDto>))]
    [HttpGet("")]
    public async IAsyncEnumerable<SecurityDto> GetWebItemSettingsSecurityInfo(SecuritySettingsRequestDto inDto)
    {
        if (inDto.Ids == null || !inDto.Ids.Any())
        {
            inDto.Ids = WebItemManager.GetItemsAll().Select(i => i.ID.ToString());
        }

        var subItemList = WebItemManager.GetItemsAll().Where(item => item.IsSubItem()).Select(i => i.ID.ToString()).ToList();

        // The same subject shows up under many modules, and without ids that is every module the
        // portal has — the visibility check is asked once per subject instead of once per pair.
        var visible = new Dictionary<Guid, bool>();

        foreach (var r in inDto.Ids)
        {
            var i = await webItemSecurity.GetSecurityInfoAsync(r);

            var s = new SecurityDto
            {
                WebItemId = i.WebItemId,
                Enabled = i.Enabled,
                Groups = [],
                IsSubItem = subItemList.Contains(i.WebItemId),
                Users = []
            };

            foreach (var e in i.Groups)
            {
                s.Groups.Add(await groupSummaryDtoHelper.GetAsync(e));
            }

            foreach (var e in i.Users)
            {
                if (!visible.TryGetValue(e.Id, out var canView))
                {
                    canView = await userManager.CanUserViewAnotherUserAsync(authContext.CurrentAccount.ID, e.Id);
                    visible[e.Id] = canView;
                }

                if (!canView)
                {
                    continue;
                }

                s.Users.Add(await employeeWrapperHelper.GetAsync(e));
            }

            yield return s;
        }
    }

    /// <remarks>
    /// Answers whether the module with the given identifier is available to the calling user right now, as a single
    /// boolean. `id` is the module GUID and travels in the path; a value that is not a GUID does not match the route
    /// at all. Any signed-in member may call this; anonymous callers are not admitted. The operation is read-only and
    /// its answer is specific to the caller: `true` means a module with that identifier is registered in this portal,
    /// is visible, and the caller is allowed to read it, while `false` covers every other case - the module is not
    /// registered here, it is hidden for this portal, or the caller is outside the users and groups allowed to open
    /// it. A `false` therefore does not tell those apart, and an unknown identifier is reported as unavailable
    /// instead of failing. Read the allow-list behind the decision with `GET api/2.0/settings/security`, list the
    /// modules the caller can actually open with `GET api/2.0/settings/security/modules`, and change access with
    /// `PUT api/2.0/settings/security`.
    /// </remarks>
    /// <summary>
    /// Check module availability
    /// </summary>
    /// <path>api/2.0/settings/security/{id}</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Whether the module is registered, visible and readable by the calling user - false covers a module that is not registered here as well as one the caller may not open", typeof(bool))]
    [HttpGet("{id:guid}")]
    public async Task<bool> GetWebItemSecurityInfo(IdRequestDto<Guid> inDto)
    {
        var module = WebItemManager[inDto.Id];

        return module != null && !await module.IsDisabledAsync(webItemSecurity, authContext);
    }

    /// <remarks>
    /// Lists the portal modules the calling user can currently open, each as an `id` holding the module's product
    /// class name and a `title` holding its display name, both HTML-encoded. Any signed-in member may call this;
    /// anonymous callers are not admitted. The operation is read-only and takes no parameters, and the list is
    /// specific to the caller: modules hidden for this portal, and modules whose access rules exclude the caller, are
    /// left out, and sub-modules nested under another module are never listed. Entries follow the portal's own module
    /// order rather than an alphabetical one. An empty list means the installation registers no such modules at all -
    /// the case on DocSpace, where the classic modules do not exist - and is not a failure. The identifiers here are
    /// display-oriented class names, not the GUIDs the access-settings operations work with, so do not feed them to
    /// `GET api/2.0/settings/security/{id}`, which expects a module GUID.
    /// </remarks>
    /// <summary>
    /// Get enabled modules
    /// </summary>
    /// <path>api/2.0/settings/security/modules</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The portal modules the calling user can open, each with its product class name and its display name, in the portal's own module order", typeof(IEnumerable<EnabledModuleDto>))]
    [HttpGet("modules")]
    public async Task<IEnumerable<EnabledModuleDto>> GetEnabledModules()
    {
        var enabledModules = (await webItemManagerSecurity.GetItemsAsync(WebZoneType.All))
                                    .Where(item => !item.IsSubItem() && item.Visible)
            .Select(item => new EnabledModuleDto { Id = item.ProductClassName.HtmlEncode(), Title = item.Name.HtmlEncode() });

        return enabledModules;
    }

    /// <remarks>
    /// Returns the password policy of the current portal: the minimum length together with the flags that demand an
    /// uppercase letter, a digit and a special symbol, plus the regular expressions a client can check a password
    /// against before sending it anywhere. Any signed-in member may read it, and it is also reachable with the
    /// parameters of a confirmation link, so an invited user or one resetting a password can validate the new
    /// password before having a session; a portal whose payment has lapsed still answers. The operation is read-only
    /// and honours `If-Modified-Since`: send back the `Last-Modified` value of an earlier answer and an unchanged
    /// policy comes back as an empty not-modified response rather than a body. A portal nobody has configured
    /// requires 8 characters with all three flags off. Whatever the policy says, the portal refuses a password longer
    /// than 30 characters, a ceiling this answer does not carry. Change the policy with
    /// `PUT api/2.0/settings/security/password`.
    /// </remarks>
    /// <summary>
    /// Get password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The portal password policy: the minimum length, the uppercase, digit and special-symbol requirements, and the regular expressions a client can validate against", typeof(PasswordSettingsDto))]
    [HttpGet("password")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Authenticated")]
    public async Task<PasswordSettingsDto> GetPasswordSettings()
    {
        var settings = await settingsManager.LoadAsync<PasswordSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : passwordSettingsConverter.Convert(settings);
    }

    /// <remarks>
    /// Replaces the password policy of the whole portal with the four values sent: `minLength` and the three flags
    /// that demand an uppercase letter, a digit and a special symbol. There is no partial update - a flag left out of
    /// the body is stored as `false` - so read the current policy with `GET api/2.0/settings/security/password` and
    /// send it back with your change applied. The caller needs the portal-settings right of a DocSpace administrator,
    /// otherwise the call is refused. `minLength` has to sit between the floor the installation is configured with, 8
    /// characters unless it was changed, and the ceiling of 30; anything outside is rejected as an invalid request.
    /// The new policy applies to passwords set from now on: existing passwords keep working until their owners change
    /// them, and nobody is asked to renew. The change is portal-wide, recorded in the audit trail, and sending the
    /// same body twice changes nothing further. The answer is the stored policy with its regular expressions.
    /// </remarks>
    /// <summary>
    /// Update password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The password policy as it was stored, including the regular expressions a client can validate against", typeof(PasswordSettingsDto))]
    [SwaggerResponse(400, "The requested minimum length is outside the range the installation allows")]
    [HttpPut("password")]
    public async Task<PasswordSettingsDto> UpdatePasswordSettings(PasswordSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var userPasswordSettings = await settingsManager.LoadAsync<PasswordSettings>();

        if (!passwordSettingsManager.CheckLengthInRange(inDto.MinLength))
        {
            throw new ArgumentException(nameof(inDto.MinLength));
        }

        userPasswordSettings.MinLength = inDto.MinLength;
        userPasswordSettings.UpperCase = inDto.UpperCase;
        userPasswordSettings.Digits = inDto.Digits;
        userPasswordSettings.SpecSymbols = inDto.SpecSymbols;

        await settingsManager.SaveAsync(userPasswordSettings);

        messageService.Send(MessageAction.PasswordStrengthSettingsUpdated);

        return passwordSettingsConverter.Convert(userPasswordSettings);
    }

    /// <remarks>
    /// Replaces the access rules of one portal module: `id` names the module, `enabled` says whether it may be
    /// opened, and `subjects` lists the users and groups the rule is stored for. The caller needs the portal-settings
    /// right of a DocSpace administrator, and the call is answered with 403 on an open portal, where everyone is
    /// admitted and per-module rules would mean nothing. `id` has to be a GUID; anything else is rejected as an
    /// invalid request. The rules stored before are dropped rather than extended, so send the full list of subjects
    /// every time. Watch the empty cases: leaving `subjects` out applies `enabled` to everyone, while an empty
    /// `subjects` array is stored as access for everyone whatever `enabled` says. The change is recorded in the audit
    /// trail unless `subjects` was left out entirely. The answer is the module's resulting configuration as a
    /// single-entry list, in the shape `GET api/2.0/settings/security` returns. To switch several modules at once use
    /// `PUT api/2.0/settings/security/access`.
    /// </remarks>
    /// <summary>
    /// Set module access
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The resulting access configuration of the module, as a single-entry list", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(403, "Per-module access cannot be configured on an open portal, or the caller lacks the portal-settings right of a DocSpace administrator")]
    [HttpPut("")]
    public async Task<IEnumerable<SecurityDto>> SetWebItemSecurity(WebItemSecurityRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await webItemSecurity.SetSecurityAsync(inDto.Id, inDto.Enabled, inDto.Subjects?.ToArray());
        var securityInfo = await GetWebItemSettingsSecurityInfo(new SecuritySettingsRequestDto { Ids = new List<string> { inDto.Id } }).ToListAsync();

        if (inDto.Subjects == null)
        {
            return securityInfo;
        }

        var productName = GetProductName(new Guid(inDto.Id));

        if (!inDto.Subjects.Any())
        {
            messageService.Send(MessageAction.ProductAccessOpened, productName);
        }
        else
        {
            foreach (var info in securityInfo)
            {
                if (info.Groups.Count != 0)
                {
                    messageService.Send(MessageAction.GroupsOpenedProductAccess, productName,
                        info.Groups.Select(x => x.Name));
                }

                if (info.Users.Count != 0)
                {
                    messageService.Send(MessageAction.UsersOpenedProductAccess, productName,
                        info.Users.Select(x => HttpUtility.HtmlDecode(x.DisplayName)));
                }
            }
        }

        return securityInfo;
    }

    /// <remarks>
    /// Switches several portal modules on or off in one call: `items` carries an entry per module, its `key` the
    /// module GUID and its `value` the new enabled flag. The caller needs the portal-settings right of a DocSpace
    /// administrator, and the call is answered with 403 on an open portal, where everyone is admitted and per-module
    /// rules would mean nothing. Every key has to be a GUID; anything else is rejected as an invalid request, and a
    /// module listed twice is applied once, from its first entry. This operation carries no subject list of its own:
    /// switching a product module on restores the users and groups it was last restricted to, while every other case
    /// is stored as a plain allow or deny for everyone, so use `PUT api/2.0/settings/security` when the allow-list
    /// itself has to change. The batch is recorded in the audit trail as one list update rather than module by
    /// module. The answer is the resulting configuration of every module listed, in the shape
    /// `GET api/2.0/settings/security` returns.
    /// </remarks>
    /// <summary>
    /// Set access to modules in bulk
    /// </summary>
    /// <path>api/2.0/settings/security/access</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The resulting access configuration of every module listed in the request", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(403, "Per-module access cannot be configured on an open portal, or the caller lacks the portal-settings right of a DocSpace administrator")]
    [HttpPut("access")]
    public async Task<IEnumerable<SecurityDto>> SetAccessToWebItems(WebItemsSecurityRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var itemList = new ItemDictionary<string, bool>();

        foreach (var item in inDto.Items)
        {
            itemList.TryAdd(item.Key, item.Value);
        }

        foreach (var item in itemList)
        {
            Guid[] subjects = null;
            var productId = new Guid(item.Key);

            if (item.Value)
            {
                if (WebItemManager[productId] is IProduct || productId == WebItemManager.MailProductID)
                {
                    var productInfo = await webItemSecurity.GetSecurityInfoAsync(item.Key);
                    var selectedGroups = productInfo.Groups.Select(group => group.ID).ToList();
                    var selectedUsers = productInfo.Users.Select(user => user.Id).ToList();
                    selectedUsers.AddRange(selectedGroups);
                    if (selectedUsers.Count > 0)
                    {
                        subjects = selectedUsers.ToArray();
                    }
                }
            }

            await webItemSecurity.SetSecurityAsync(item.Key, item.Value, subjects);
        }

        messageService.Send(MessageAction.ProductsListUpdated);

        return await GetWebItemSettingsSecurityInfo(new SecuritySettingsRequestDto { Ids = itemList.Keys.ToList() }).ToListAsync();
    }

    /// <remarks>
    /// Lists the users who administer the portal module identified by `productid` in the path. The all-zero GUID
    /// stands for the portal itself: the answer then covers the DocSpace administrator group together with every
    /// product group, and includes the portal owner, who administers everything by default. The caller needs the
    /// portal-settings right of a DocSpace administrator, otherwise the call is refused. `productid` has to be a
    /// GUID, and one that names no group is answered with an empty list rather than a failure. The operation is
    /// read-only and returns whole user profiles, a heavier answer than a membership check, and a user who belongs to
    /// more than one of the groups asked about is listed once per group. Entries arrive in group order, the DocSpace
    /// administrator group first, the list is neither paged nor filterable, and a promotion made through the sibling
    /// `PUT` shows up here at once. Use `GET api/2.0/settings/security/administrator` to test a single user against a
    /// single module, and `PUT api/2.0/settings/security/administrator` to promote or demote somebody.
    /// </remarks>
    /// <summary>
    /// Get product administrators
    /// </summary>
    /// <path>api/2.0/settings/security/administrator/{productid}</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The users who administer the module asked about, or the portal-wide administrators when the all-zero identifier is used", typeof(IAsyncEnumerable<EmployeeDto>))]
    [HttpGet("administrator/{productid:guid}")]
    public async IAsyncEnumerable<EmployeeDto> GetProductAdministrators(ProductIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var admins = await webItemSecurity.GetProductAdministratorsAsync(inDto.ProductId);

        foreach (var a in admins)
        {
            yield return await employeeWrapperHelper.GetAsync(a);
        }
    }

    /// <remarks>
    /// Reports whether one user administers one portal module, as the identifiers asked about plus an `administrator`
    /// flag. Both `productid` and `userid` are query parameters and both are required; the all-zero product GUID asks
    /// about the portal itself rather than about a single module. The caller needs the portal-settings right of a
    /// DocSpace administrator, otherwise the call is refused. The operation is read-only. The flag is `true` when the
    /// user belongs to the DocSpace administrator group or to the module's own group, so a portal-wide administrator
    /// is reported as an administrator of every module, whatever the module identifier says. Identifiers that name no
    /// user and no group are answered with `false` instead of a failure, so a `false` does not prove the user exists.
    /// The verdict is read out of group membership alone and says nothing about whether the module is enabled for
    /// this portal, which `GET api/2.0/settings/security/{id}` reports. Use
    /// `GET api/2.0/settings/security/administrator/{productid}` to list everyone who administers a module, and
    /// `PUT api/2.0/settings/security/administrator` to change the membership.
    /// </remarks>
    /// <summary>
    /// Check product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The module and the user asked about together with the flag that says whether that user administers the module", typeof(ProductAdministratorDto))]
    [HttpGet("administrator")]
    public async Task<ProductAdministratorDto> GetIsProductAdministrator(UserProductIdsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var result = await webItemSecurity.IsProductAdministratorAsync(inDto.ProductId, inDto.UserId);
        return new ProductAdministratorDto { ProductId = inDto.ProductId, UserId = inDto.UserId, Administrator = result };
    }

    /// <remarks>
    /// Promotes a portal member to administrator of one module, or takes that role away, according to the
    /// `administrator` flag; the all-zero product GUID targets the DocSpace administrator role, which covers the
    /// whole portal. The caller needs the portal-settings right of a DocSpace administrator, and granting the
    /// portal-wide role additionally requires being the portal owner - anyone else is refused with 403. A free cloud
    /// plan does not offer the option at all and answers 402, as does a promotion for which no paid seat is left,
    /// since promoting a guest or a plain member turns them into a paid one. Taking the portal-wide role away also
    /// removes the member from every product group. The change is immediate, portal-wide, recorded in the audit
    /// trail, and sending the same body twice changes nothing further; it never creates a user, so invite the member
    /// first. The answer echoes the identifiers and the flag as stored - re-read membership with
    /// `GET api/2.0/settings/security/administrator`.
    /// </remarks>
    /// <summary>
    /// Set product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The module, the user and the administrator flag as they were stored", typeof(ProductAdministratorDto))]
    [SwaggerResponse(402, "The portal plan does not offer product administrators, or no paid seat is left for the member being promoted")]
    [SwaggerResponse(403, "Only the portal owner can grant or revoke the portal-wide administrator role")]
    [HttpPut("administrator")]
    public async Task<ProductAdministratorDto> SetProductAdministrator(SecurityRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var isStartup = !coreBaseSettings.CustomMode && tenantExtra.Saas &&
                        (await tenantManager.GetCurrentTenantQuotaAsync()).Free;
        if (isStartup)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        await webItemSecurity.SetProductAdministrator(inDto.ProductId, inDto.UserId, inDto.Administrator);

        var admin = await userManager.GetUsersAsync(inDto.UserId);

        if (inDto.ProductId == Guid.Empty)
        {
            var messageAction = inDto.Administrator
                ? MessageAction.AdministratorOpenedFullAccess
                : MessageAction.AdministratorDeleted;
            messageService.Send(messageAction, MessageTarget.Create(admin.Id),
                admin.DisplayUserName(false, displayUserSettingsHelper));
        }
        else
        {
            var messageAction = inDto.Administrator
                ? MessageAction.ProductAddedAdministrator
                : MessageAction.ProductDeletedAdministrator;
            messageService.Send(messageAction, MessageTarget.Create(admin.Id),
                GetProductName(inDto.ProductId), admin.DisplayUserName(false, displayUserSettingsHelper));
        }

        return new ProductAdministratorDto { ProductId = inDto.ProductId, UserId = inDto.UserId, Administrator = inDto.Administrator };
    }

    /// <remarks>
    /// Replaces the brute-force protection of the sign-in form for the whole portal: `attemptCount` failed attempts
    /// inside a rolling window of `checkPeriod` seconds, after which the offender is blocked for `blockTime` seconds.
    /// All three values are replaced together and each has to be between 1 and 9999, so read the current ones with
    /// `GET api/2.0/settings/security/loginsettings` before changing only one of them; a value outside the range is
    /// rejected as an invalid request. The caller needs the portal-settings right of a DocSpace administrator,
    /// otherwise the call is refused. Failed attempts are counted per user name and client address, so one member's
    /// lockout leaves the rest of the portal signing in normally, and a blocked pair is refused even once the
    /// password is finally correct. The new numbers apply to attempts made from now on and leave counters and blocks
    /// already running as they are. The change is recorded in the audit trail, and the answer is the stored settings
    /// with the flag that says whether they still match the shipped defaults.
    /// </remarks>
    /// <summary>
    /// Update login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings as they were stored, with the flag that says whether they match the shipped defaults", typeof(LoginSettingsDto))]
    [HttpPut("loginSettings")]
    public async Task<LoginSettingsDto> UpdateLoginSettings(LoginSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new LoginSettings
        {
            AttemptCount = inDto.AttemptCount,
            CheckPeriod = inDto.CheckPeriod,
            BlockTime = inDto.BlockTime
        };

        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.LoginSettingsUpdated);

        return settings.Map();
    }

    /// <remarks>
    /// Returns the brute-force protection of the sign-in form for the current portal: how many failed attempts are
    /// tolerated, how long the window they are counted in lasts, and how long an offender stays blocked. The caller
    /// needs the portal-settings right of a DocSpace administrator; members without it are refused, and anonymous
    /// callers are not admitted. The operation is read-only and honours `If-Modified-Since`: send back the
    /// `Last-Modified` value of an earlier answer and unchanged settings come back as an empty not-modified response
    /// rather than a body. `checkPeriod` and `blockTime` are counted in seconds. A portal nobody has configured
    /// tolerates 5 failed attempts inside a window of 60 seconds and blocks for 60 seconds, and reports `isDefault`
    /// true; the flag turns false as soon as any of the three values differs from that. The answer describes the
    /// portal-wide policy only: it does not say which accounts or addresses are blocked at the moment, while a
    /// lockout that has already happened is recorded in the login history and can be read with
    /// `GET api/2.0/security/audit/login/filter`. Change the numbers with
    /// `PUT api/2.0/settings/security/loginsettings`, or put them back with
    /// `DELETE api/2.0/settings/security/loginsettings`.
    /// </remarks>
    /// <summary>
    /// Get login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings of the portal: the tolerated attempts, the counting window and the block in seconds, and whether they match the shipped defaults", typeof(LoginSettingsDto))]
    [HttpGet("loginSettings")]
    public async Task<LoginSettingsDto> GetLoginSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<LoginSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings.Map();
    }

    /// <remarks>
    /// Puts the brute-force protection of the sign-in form back to what the portal shipped with: 5 tolerated failed
    /// attempts, a counting window of 60 seconds and a block of 60 seconds. The caller needs the portal-settings
    /// right of a DocSpace administrator, otherwise the call is refused. The operation takes no parameters and
    /// overwrites whatever was configured before without asking, so read the current numbers with
    /// `GET api/2.0/settings/security/loginsettings` first if they are worth keeping. Only the setting is reset:
    /// sign-ins already blocked stay blocked until the block they were given runs out, and the attempt counters
    /// running for other users are left alone. The reset is portal-wide, applies to attempts made from now on, is
    /// recorded in the audit trail, and calling it twice changes nothing further. The restored numbers also decide
    /// when the sign-in form starts asking for a captcha, which it does one attempt before the block. The answer is
    /// the restored settings, with `isDefault` true. Store numbers of your own with
    /// `PUT api/2.0/settings/security/loginsettings`.
    /// </remarks>
    /// <summary>
    /// Reset login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings restored to the shipped defaults", typeof(LoginSettingsDto))]
    [HttpDelete("loginSettings")]
    public async Task<LoginSettingsDto> SetDefaultLoginSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var defaultSettings = new LoginSettings().GetDefault();

        await settingsManager.SaveAsync(defaultSettings);

        messageService.Send(MessageAction.LoginSettingsUpdated);

        return defaultSettings.Map();
    }
}
