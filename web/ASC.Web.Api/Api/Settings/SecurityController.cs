﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("security")]
public class SecurityController(
    GroupSummaryDtoHelper groupSummaryDtoHelper,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    CoreBaseSettings coreBaseSettings,
    MessageService messageService,
    ApiContext apiContext,
    UserManager userManager,
    AuthContext authContext,
    WebItemSecurity webItemSecurity,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    WebItemManagerSecurity webItemManagerSecurity,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    IMemoryCache memoryCache,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    PasswordSettingsConverter passwordSettingsConverter,
    PasswordSettingsManager passwordSettingsManager)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the security settings for the modules specified in the request.
    /// </summary>
    /// <short>
    /// Get the security settings
    /// </short>
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

        var subItemList = WebItemManager.GetItemsAll().Where(item => item.IsSubItem()).Select(i => i.ID.ToString());

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

    /// <summary>
    /// Returns the availability of the module with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get the module availability
    /// </short>
    /// <path>api/2.0/settings/security/{id}</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Boolean value: true - module is enabled, false - module is disabled", typeof(bool))]
    [HttpGet("{id:guid}")]
    public async Task<bool> GetWebItemSecurityInfoAsync(IdRequestDto<Guid> inDto)
    {
        var module = WebItemManager[inDto.Id];

        return module != null && !await module.IsDisabledAsync(webItemSecurity, authContext);
    }

    /// <summary>
    /// Returns a list of all the enabled modules.
    /// </summary>
    /// <short>
    /// Get the enabled modules
    /// </short>
    /// <path>api/2.0/settings/security/modules</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "List of enabled modules", typeof(object))]
    [HttpGet("modules")]
    public object GetEnabledModules()
    {
        var EnabledModules = webItemManagerSecurity.GetItems(WebZoneType.All)
                                    .Where(item => !item.IsSubItem() && item.Visible)
            .Select(item => new { id = item.ProductClassName.HtmlEncode(), title = item.Name.HtmlEncode() });

        return EnabledModules;
    }

    /// <summary>
    /// Returns the portal password settings.
    /// </summary>
    /// <short>
    /// Get the password settings
    /// </short>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Password settings", typeof(PasswordSettingsDto))]
    [HttpGet("password")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Everyone")]
    public async Task<PasswordSettingsDto> GetPasswordSettingsAsync()
    {
        var settings = await settingsManager.LoadAsync<PasswordSettings>();
        return passwordSettingsConverter.Convert(settings);
    }

    /// <summary>
    /// Sets the portal password settings.
    /// </summary>
    /// <short>
    /// Set the password settings
    /// </short>
    /// <path>api/2.0/settings/security/password</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Password settings", typeof(PasswordSettingsDto))]
    [SwaggerResponse(400, "MinLength")]
    [HttpPut("password")]
    public async Task<PasswordSettingsDto> UpdatePasswordSettingsAsync(PasswordSettingsRequestsDto inDto)
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

    /// <summary>
    /// Sets the security settings to the module with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Set the module security settings
    /// </short>
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

    /// <summary>
    /// Sets the access settings to the products with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Set the access settings to products
    /// </short>
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

        var defaultPageSettings = await settingsManager.LoadAsync<StudioDefaultPageSettings>();

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
            else if (productId == defaultPageSettings.DefaultProductID)
            {
                await settingsManager.SaveAsync(settingsManager.GetDefault<StudioDefaultPageSettings>());
            }

            await webItemSecurity.SetSecurityAsync(item.Key, item.Value, subjects);
        }

        messageService.Send(MessageAction.ProductsListUpdated);

        return await GetWebItemSettingsSecurityInfo(new SecuritySettingsRequestDto { Ids = itemList.Keys.ToList() }).ToListAsync();
    }

    /// <summary>
    /// Returns a list of all the product administrators with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get the product administrators
    /// </short>
    /// <path>api/2.0/settings/security/administrator/{productid}</path>
    /// <collection>list</collection>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "List of product administrators with the following parameters", typeof(IAsyncEnumerable<EmployeeDto>))]
    [HttpGet("administrator/{productid:guid}")]
    public async IAsyncEnumerable<EmployeeDto> GetProductAdministrators(ProductIdRequestDto inDto)
    {
        var admins = await webItemSecurity.GetProductAdministratorsAsync(inDto.ProductId);

        foreach (var a in admins)
        {
            yield return await employeeWrapperHelper.GetAsync(a);
        }
    }

    /// <summary>
    /// Checks if the selected user is a product administrator with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Check a product administrator
    /// </short>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Object with the user security information: product ID, user ID, administrator or not", typeof(object))]
    [HttpGet("administrator")]
    public async Task<object> IsProductAdministratorAsync(UserProductIdsRequestDto inDto)
    {
        var result = await webItemSecurity.IsProductAdministratorAsync(inDto.ProductId, inDto.UserId);
        return new { inDto.ProductId, inDto.UserId, Administrator = result };
    }

    /// <summary>
    /// Sets the selected user as a product administrator with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Set a product administrator
    /// </short>
    /// <path>api/2.0/settings/security/administrator</path>
    [Tags("Settings / Security")]
    [SwaggerResponse(200, "Object with the user security information: product ID, user ID, administrator or not", typeof(object))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPut("administrator")]
    public async Task<object> SetProductAdministrator(SecurityRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var isStartup = !coreBaseSettings.CustomMode && tenantExtra.Saas &&
                        (await tenantManager.GetCurrentTenantQuotaAsync()).Free;
        if (isStartup)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Administrator");
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

        return new { inDto.ProductId, inDto.UserId, inDto.Administrator };
    }

    /// <summary>
    /// Updates the login settings with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update login settings
    /// </short>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Updated login settings", typeof(LoginSettingsDto))]
    [HttpPut("loginSettings")]
    public async Task<LoginSettingsDto> UpdateLoginSettingsAsync(LoginSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new LoginSettings
        {
            AttemptCount = inDto.AttemptCount, 
            CheckPeriod = inDto.CheckPeriod, 
            BlockTime = inDto.BlockTime
        };

        await settingsManager.SaveAsync(settings);

        return mapper.Map<LoginSettings, LoginSettingsDto>(settings);
    }

    /// <summary>
    /// Returns the portal login settings.
    /// </summary>
    /// <short>
    /// Get login settings
    /// </short>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Login settings", typeof(LoginSettingsDto))]
    [HttpGet("loginSettings")]
    public async Task<LoginSettingsDto> GetLoginSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<LoginSettings>();

        return mapper.Map<LoginSettings, LoginSettingsDto>(settings);
    }

    /// <summary>
    ///  Returns the portal login settings.
    /// </summary>
    /// <path>api/2.0/settings/security/loginsettings</path>
    [Tags("Settings / Login settings")]
    [SwaggerResponse(200, "Login settings", typeof(LoginSettingsDto))]
    [HttpDelete("loginSettings")]
    public async Task<LoginSettingsDto> SetDefaultLoginSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var defaultSettings = new LoginSettings().GetDefault();
        
        await settingsManager.SaveAsync(defaultSettings);

        return mapper.Map<LoginSettings, LoginSettingsDto>(defaultSettings);
    }
}