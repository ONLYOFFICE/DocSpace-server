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
    /// Returns the access rules of portal modules, one entry per GUID in `ids`. Entries follow the order of `ids`. Each
    /// entry says whether the module is enabled and which groups and users may open it. Without `ids`, the call reports
    /// every module the portal registers. DocSpace registers none, so the list is then empty. Any signed-in user can call
    /// it. The call is read-only. If an `ids` value is not a GUID, the call returns 400. A module that nobody configured
    /// has `enabled` set to `false`. The `users` list leaves out users the caller cannot see. So two callers can get
    /// different lists for one module. The `isSubItem` flag is `true` for a module nested under another module. To change
    /// the rules, use `PUT api/2.0/settings/security`.
    /// </remarks>
    /// <summary>
    /// Get module access settings
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The access configuration of every module identifier asked about: the enabled flag, the allowed groups, the allowed users the caller may see, and the sub-module flag", typeof(IAsyncEnumerable<SecurityDto>))]
    [SwaggerResponse(400, "An `ids` value is not a GUID")]
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
    /// Returns whether the calling user can open the portal module with the GUID `id`. The answer is a single boolean.
    /// Any signed-in user can call it. The call is read-only, and the answer depends on the caller. The value `true`
    /// means the module is registered and visible, and the caller may read it. The value `false` covers all other cases:
    /// an unknown module, a hidden module, or a caller outside the allow-list. So `false` does not tell these cases
    /// apart, and an unknown `id` is not an error. A value that is not a GUID does not match the route. To read the
    /// allow-list, use `GET api/2.0/settings/security`. To list the modules the caller can open, use
    /// `GET api/2.0/settings/security/modules`. To change access, use `PUT api/2.0/settings/security`.
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
    /// Returns the portal modules that the calling user can open now. Each entry has the product class name in `id` and
    /// the display name in `title`. Both values are HTML-encoded. Any signed-in user can call it. The call is read-only
    /// and takes no parameters. The list depends on the caller. It leaves out hidden modules, modules whose access rules
    /// exclude the caller, and nested sub-modules. Entries follow the portal's own module order, not the alphabet. An
    /// empty list means the installation registers no such modules. This is the normal case on DocSpace, not an error.
    /// The `id` values are class names, not module GUIDs. Do not pass them to `GET api/2.0/settings/security/{id}`.
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
    /// Returns the password policy of the portal. It has the minimum length and the flags that require an uppercase
    /// letter, a digit, and a special symbol. It also has regular expressions to check a password before sending it. Any
    /// signed-in user can call it. A request signed with the confirmation token from an emailed link also works. So an
    /// invited user or a user who resets a password can check the new password. The call works even if the portal payment
    /// is overdue. The call is read-only. It supports `If-Modified-Since` with the `Last-Modified` value of an earlier
    /// answer. If the policy has not changed since then, the call returns 304 with an empty body. A portal that nobody
    /// configured requires 8 characters with all three flags off. The portal also refuses passwords over 30 characters,
    /// and the answer does not show this limit. To change the policy, use `PUT api/2.0/settings/security/password`.
    /// </remarks>
    /// <summary>
    /// Get password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The portal password policy: the minimum length, the uppercase, digit and special-symbol requirements, and the regular expressions a client can validate against", typeof(PasswordSettingsDto))]
    [SwaggerResponse(304, "The password policy has not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty")]
    [HttpGet("password")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Authenticated")]
    public async Task<PasswordSettingsDto> GetPasswordSettings()
    {
        var settings = await settingsManager.LoadAsync<PasswordSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : passwordSettingsConverter.Convert(settings);
    }

    /// <remarks>
    /// Updates the password policy of the portal with `minLength`, `upperCase`, `digits`, and `specSymbols`. The call
    /// replaces all four values, and a flag left out of the body is stored as `false`. So first read the policy with
    /// `GET api/2.0/settings/security/password`. Then send it back with your change. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call changes data, and you
    /// can repeat it with the same body. The new policy applies only to passwords set after this call. Existing passwords
    /// keep working, and nobody is asked to change them. The call returns 400 if the body cannot be read, `minLength` is
    /// missing, or `minLength` is out of range. It returns the stored policy with its regular expressions.
    /// </remarks>
    /// <summary>
    /// Update password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The password policy as it was stored, including the regular expressions a client can validate against", typeof(PasswordSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `minLength`, or the minimum length is outside the range the installation allows")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Replaces the access rule of one portal module. The rule says whether the module may be opened, and for which users
    /// and groups. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. The call also returns 403 on an open portal, where everyone is admitted. The call changes data,
    /// and you can repeat it with the same body. It deletes the stored subjects instead of adding to them. So send the
    /// full list of subjects every time. If `subjects` is left out, `enabled` applies to everyone. An empty `subjects`
    /// array gives access to everyone, whatever `enabled` says. The call returns 400 if the body cannot be read, has no
    /// `id`, or `id` is not a GUID. It returns the resulting rule as a list with one entry, in the shape of
    /// `GET api/2.0/settings/security`. To switch several modules in one call, use
    /// `PUT api/2.0/settings/security/access`.
    /// </remarks>
    /// <summary>
    /// Set module access
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The resulting access configuration of the module, as a single-entry list", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`, or the `id` is not a GUID")]
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
    /// Turns several portal modules on or off in one call. Each `items` entry has a module GUID in `key` and the enabled
    /// flag in `value`. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call also returns 403 on an open portal, where everyone is admitted. The call changes
    /// data, and you can repeat it with the same body. If a module is listed twice, the call applies only its first
    /// entry. The body has no list of users and groups. Turning a product module on restores its last list of users and
    /// groups. In all other cases, the call allows or denies the module for everyone. To change that list, use
    /// `PUT api/2.0/settings/security`. If the body cannot be read or a `key` is not a GUID, the call returns 400. It
    /// returns the resulting rules of the listed modules in the shape of `GET api/2.0/settings/security`.
    /// </remarks>
    /// <summary>
    /// Set access to modules in bulk
    /// </summary>
    /// <path>api/2.0/settings/security/access</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The resulting access configuration of every module listed in the request", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(400, "The request body cannot be read, or a `key` in `items` is not a GUID")]
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
    /// Returns the users who administer the portal module with the GUID `productId`. The all-zero GUID means the whole
    /// portal. Then the list covers the DocSpace admin group and every product group, and it includes the portal owner.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call is read-only. If `productId` names no group, the call returns an empty list. Entries come in
    /// group order, with the DocSpace admin group first. A user who is in several of these groups appears once per group.
    /// The list has no paging and no filter. To check one user against one module, use
    /// `GET api/2.0/settings/security/administrator`. To add or remove an administrator, use
    /// `PUT api/2.0/settings/security/administrator`.
    /// </remarks>
    /// <summary>
    /// Get product administrators
    /// </summary>
    /// <path>api/2.0/settings/security/administrator/{productId}</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The users who administer the module asked about, or the portal-wide administrators when the all-zero identifier is used", typeof(IAsyncEnumerable<EmployeeDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("administrator/{productId:guid}")]
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
    /// Returns whether one user administers one portal module. The answer repeats `productId` and `userId` and adds the
    /// `administrator` flag. The all-zero `productId` asks about the whole portal. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The
    /// flag is `true` if the user is in the DocSpace admin group or in the module group. So a DocSpace admin is an
    /// administrator of every module. Unknown user or module IDs give `false`, not an error. So `false` does not prove
    /// that the user exists. The flag comes only from group membership and does not show whether the module is available.
    /// To check that, use `GET api/2.0/settings/security/{id}`. To list all administrators of a module, use
    /// `GET api/2.0/settings/security/administrator/{productId}`. To change the role, use
    /// `PUT api/2.0/settings/security/administrator`.
    /// </remarks>
    /// <summary>
    /// Check product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The module and the user asked about together with the flag that says whether that user administers the module", typeof(ProductAdministratorDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("administrator")]
    public async Task<ProductAdministratorDto> GetIsProductAdministrator(UserProductIdsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var result = await webItemSecurity.IsProductAdministratorAsync(inDto.ProductId, inDto.UserId);
        return new ProductAdministratorDto { ProductId = inDto.ProductId, UserId = inDto.UserId, Administrator = result };
    }

    /// <remarks>
    /// Adds a portal member to the administrators of one module, or removes the member, by the `administrator` flag. The
    /// all-zero `productId` means the DocSpace admin role for the whole portal. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. Only the portal owner can
    /// grant the DocSpace admin role or take any role away from a DocSpace admin. Other callers get 403 for these
    /// changes. On a free cloud plan, the call returns 402. Promoting a guest or a user takes a paid seat. If no paid
    /// seat is left, the call returns 402. Removing the DocSpace admin role also removes the member from every product
    /// group. The call changes data immediately, and you can repeat it with the same body. It never creates a user, so
    /// invite the member first. If the body cannot be read or has no `productId` or `userId`, the call returns 400. The
    /// call returns the identifiers and the flag as sent. To check membership, use
    /// `GET api/2.0/settings/security/administrator`.
    /// </remarks>
    /// <summary>
    /// Set product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "The module, the user and the administrator flag as they were stored", typeof(ProductAdministratorDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `productId` or `userId`")]
    [SwaggerResponse(402, "The portal plan does not offer product administrators, or no paid seat is left for the member being promoted")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or is not the portal owner and grants the portal-wide administrator role or takes a role away from a DocSpace administrator")]
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
    /// Updates the brute-force protection of the portal sign-in. After `attemptCount` failed attempts within
    /// `checkPeriod` seconds, the portal blocks the sign-in for `blockTime` seconds. The call replaces all three values
    /// together. So to change one value, first read the others with `GET api/2.0/settings/security/loginsettings`.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get
    /// 403. The call changes data, and you can repeat it with the same body. The new values apply to attempts made
    /// after this call. Running counters and blocks stay as they are. The portal counts failed attempts per user name
    /// and IP address. So one blocked member does not block the rest of the portal. A blocked pair cannot sign in even
    /// with the correct password, unless it passes the CAPTCHA. The call returns 400 if the body cannot be read, or a
    /// value is missing or out of range. It returns the stored settings and whether they match the defaults.
    /// </remarks>
    /// <summary>
    /// Update login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings as they were stored, with the flag that says whether they match the shipped defaults", typeof(LoginSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read, or `attemptCount`, `checkPeriod` or `blockTime` is missing or outside 1-9999")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Returns the brute-force protection of the portal sign-in. It has the tolerated failed attempts, the counting
    /// window, and the block time. The window and the block time are in seconds. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. It
    /// supports `If-Modified-Since` with the `Last-Modified` value of an earlier answer. If the settings have not changed
    /// since then, the call returns 304 with an empty body. A portal that nobody configured allows 5 failed attempts in
    /// 60 seconds and blocks for 60 seconds. Then `isDefault` is `true`, and it becomes `false` when any of the three
    /// values differs. The answer does not show which accounts or addresses are blocked now. To read past lockouts in the
    /// login history, use `GET api/2.0/security/audit/login/filter`. To change the values, use
    /// `PUT api/2.0/settings/security/loginsettings`. To restore the defaults, use
    /// `DELETE api/2.0/settings/security/loginsettings`.
    /// </remarks>
    /// <summary>
    /// Get login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings of the portal: the tolerated attempts, the counting window and the block in seconds, and whether they match the shipped defaults", typeof(LoginSettingsDto))]
    [SwaggerResponse(304, "The brute-force protection settings have not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("loginSettings")]
    public async Task<LoginSettingsDto> GetLoginSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<LoginSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings.Map();
    }

    /// <remarks>
    /// Resets the brute-force protection of the portal sign-in to the defaults. The defaults are 5 failed attempts, a
    /// window of 60 seconds, and a block of 60 seconds. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get 403. The call changes data and takes no parameters. It
    /// overwrites the current values without a confirmation. To keep them, first read them with
    /// `GET api/2.0/settings/security/loginsettings`. You can repeat the call. The reset applies to attempts made after
    /// this call. Sign-ins that are already blocked stay blocked until their block ends. Running attempt counters stay as
    /// they are. The sign-in form asks for a captcha one attempt before the block. The call returns the restored settings
    /// with `isDefault` set to `true`. To set your own values, use `PUT api/2.0/settings/security/loginsettings`.
    /// </remarks>
    /// <summary>
    /// Reset login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "The brute-force protection settings restored to the shipped defaults", typeof(LoginSettingsDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
