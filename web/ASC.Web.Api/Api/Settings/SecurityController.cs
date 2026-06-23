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

[DefaultRoute("security")]
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
    /// Returns the security settings for the modules specified in the request.
    /// </remarks>
    /// <summary>
    /// Get the security settings
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Security settings", typeof(IAsyncEnumerable<SecurityDto>))]
    [HttpGet("")]
    public async IAsyncEnumerable<SecurityDto> GetWebItemSettingsSecurityInfo(SecuritySettingsRequestDto inDto)
    {
        if (inDto.Ids == null || !inDto.Ids.Any())
        {
            inDto.Ids = WebItemManager.GetItemsAll().Select(i => i.ID.ToString());
        }

        var subItemList = WebItemManager.GetItemsAll().Where(item => item.IsSubItem()).Select(i => i.ID.ToString()).ToList();

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
                s.Users.Add(await employeeWrapperHelper.GetAsync(e));
            }

            yield return s;
        }
    }

    /// <remarks>
    /// Returns the availability of the module with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Get the module availability
    /// </summary>
    /// <path>api/2.0/settings/security/{id}</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Boolean value: true - module is enabled, false - module is disabled", typeof(bool))]
    [HttpGet("{id:guid}")]
    public async Task<bool> GetWebItemSecurityInfo(IdRequestDto<Guid> inDto)
    {
        var module = WebItemManager[inDto.Id];

        return module != null && !await module.IsDisabledAsync(webItemSecurity, authContext);
    }

    /// <remarks>
    /// Returns a list of all the enabled modules.
    /// </remarks>
    /// <summary>
    /// Get the enabled modules
    /// </summary>
    /// <path>api/2.0/settings/security/modules</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "List of enabled modules", typeof(object))]
    [HttpGet("modules")]
    public async Task<object> GetEnabledModules()
    {
        var enabledModules = (await webItemManagerSecurity.GetItemsAsync(WebZoneType.All))
                                    .Where(item => !item.IsSubItem() && item.Visible)
            .Select(item => new { id = item.ProductClassName.HtmlEncode(), title = item.Name.HtmlEncode() });

        return enabledModules;
    }

    /// <remarks>
    /// Returns the portal password settings.
    /// </remarks>
    /// <summary>
    /// Get the password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Password settings", typeof(PasswordSettingsDto))]
    [HttpGet("password")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Authenticated")]
    public async Task<PasswordSettingsDto> GetPasswordSettings()
    {
        var settings = await settingsManager.LoadAsync<PasswordSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : passwordSettingsConverter.Convert(settings);
    }

    /// <remarks>
    /// Sets the portal password settings.
    /// </remarks>
    /// <summary>
    /// Set the password settings
    /// </summary>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Password settings", typeof(PasswordSettingsDto))]
    [SwaggerResponse(400, "MinLength")]
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
    /// Sets the security settings to the module with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Set the module security settings
    /// </summary>
    /// <path>api/2.0/settings/security</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Security settings", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(403, "Security settings are disabled for an open portal")]
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
    /// Sets the security settings to the modules with the IDs specified in the request.
    /// </remarks>
    /// <summary>
    /// Set the security settings to modules
    /// </summary>
    /// <path>api/2.0/settings/security/access</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Security settings", typeof(IEnumerable<SecurityDto>))]
    [SwaggerResponse(403, "Security settings are disabled for an open portal")]
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
    /// Returns a list of all the administrators of a product with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Get the product administrators
    /// </summary>
    /// <path>api/2.0/settings/security/administrator/{productid}</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "List of product administrators with the following parameters", typeof(IAsyncEnumerable<EmployeeDto>))]
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
    /// Checks if the selected user is an administrator of a product with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Check a product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Object with the user security information: product ID, user ID, administrator or not", typeof(ProductAdministratorDto))]
    [HttpGet("administrator")]
    public async Task<ProductAdministratorDto> GetIsProductAdministrator(UserProductIdsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var result = await webItemSecurity.IsProductAdministratorAsync(inDto.ProductId, inDto.UserId);
        return new ProductAdministratorDto { ProductId = inDto.ProductId, UserId = inDto.UserId, Administrator = result };
    }

    /// <remarks>
    /// Sets the selected user as an administrator of a product with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Set a product administrator
    /// </summary>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Object with the user security information: product ID, user ID, administrator or not", typeof(ProductAdministratorDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "Only portal owner can set user as administrator")]
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
    /// Updates the login settings with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Update the login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Updated login settings", typeof(LoginSettingsDto))]
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
    /// Returns the portal login settings.
    /// </remarks>
    /// <summary>
    /// Get the login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Login settings", typeof(LoginSettingsDto))]
    [HttpGet("loginSettings")]
    public async Task<LoginSettingsDto> GetLoginSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<LoginSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings.Map();
    }

    /// <remarks>
    /// Resets the portal login settings to default.
    /// </remarks>
    /// <summary>
    /// Reset the login settings
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Login settings", typeof(LoginSettingsDto))]
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
