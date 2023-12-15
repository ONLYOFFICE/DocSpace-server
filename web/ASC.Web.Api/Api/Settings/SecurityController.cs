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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("security")]
public class SecurityController(TenantManager tenantManager,
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
        EmployeeDtoHelper employeeWraperHelper,
        MessageTarget messageTarget,
        IMemoryCache memoryCache,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the security settings for the modules specified in the request.
    /// </summary>
    /// <short>
    /// Get the security settings
    /// </short>
    /// <category>Security</category>
    /// <param type="System.Collections.Generic.IEnumerable{System.String}, System.Collections.Generic" name="ids">List of module IDs</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.SecurityDto, ASC.Web.Api">Security settings</returns>
    /// <path>api/2.0/settings/security</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("")]
    public async IAsyncEnumerable<SecurityDto> GetWebItemSecurityInfo([FromQuery] IEnumerable<string> ids)
    {
        if (ids == null || !ids.Any())
        {
            ids = WebItemManager.GetItemsAll().Select(i => i.ID.ToString());
        }

        var subItemList = WebItemManager.GetItemsAll().Where(item => item.IsSubItem()).Select(i => i.ID.ToString());

        foreach (var r in ids)
        {
            var i = await webItemSecurity.GetSecurityInfoAsync(r);

            var s = new SecurityDto
            {
                WebItemId = i.WebItemId,
                Enabled = i.Enabled,
                Groups = i.Groups.Select(g => new GroupSummaryDto(g, userManager)),
                IsSubItem = subItemList.Contains(i.WebItemId)
            };

            s.Users = new List<EmployeeDto>();

            foreach (var e in i.Users)
            {
                s.Users.Add(await employeeWraperHelper.GetAsync(e));
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
    /// <category>Security</category>
    /// <param type="System.Guid, System" method="url" name="id">Module ID</param>
    /// <returns type="System.Boolean, System">Boolean value: true - module is enabled, false - module is disabled</returns>
    /// <path>api/2.0/settings/security/{id}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{id:guid}")]
    public async Task<bool> GetWebItemSecurityInfoAsync(Guid id)
    {
        var module = WebItemManager[id];

        return module != null && !await module.IsDisabledAsync(webItemSecurity, authContext);
    }

    /// <summary>
    /// Returns a list of all the enabled modules.
    /// </summary>
    /// <short>
    /// Get the enabled modules
    /// </short>
    /// <category>Security</category>
    /// <returns type="System.Object, System">List of enabled modules</returns>
    /// <path>api/2.0/settings/security/modules</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("modules")]
    public object GetEnabledModules()
    {
        var EnabledModules = webItemManagerSecurity.GetItems(WebZoneType.All, ItemAvailableState.Normal)
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
    /// <category>Security</category>
    /// <returns type="ASC.Web.Core.Utility.PasswordSettings, ASC.Web.Core">Password settings</returns>
    /// <path>api/2.0/settings/security/password</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("password")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Everyone")]
    public async Task<PasswordSettings> GetPasswordSettingsAsync()
    {
        return await settingsManager.LoadAsync<PasswordSettings>();
    }

    /// <summary>
    /// Sets the portal password settings.
    /// </summary>
    /// <short>
    /// Set the password settings
    /// </short>
    /// <category>Security</category>
    /// <param type="ASC.Web.Api.Models.PasswordSettingsRequestsDto, ASC.Web.Api" name="inDto">Password settings</param>
    /// <returns type="ASC.Web.Core.Utility.PasswordSettings, ASC.Web.Core">Password settings</returns>
    /// <path>api/2.0/settings/security/password</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("password")]
    public async Task<PasswordSettings> UpdatePasswordSettingsAsync(PasswordSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var userPasswordSettings = await settingsManager.LoadAsync<PasswordSettings>();

        userPasswordSettings.MinLength = inDto.MinLength;
        userPasswordSettings.UpperCase = inDto.UpperCase;
        userPasswordSettings.Digits = inDto.Digits;
        userPasswordSettings.SpecSymbols = inDto.SpecSymbols;

        await settingsManager.SaveAsync(userPasswordSettings);

        await messageService.SendAsync(MessageAction.PasswordStrengthSettingsUpdated);

        return userPasswordSettings;
    }

    /// <summary>
    /// Sets the security settings to the module with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Set the module security settings
    /// </short>
    /// <category>Security</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WebItemSecurityRequestsDto, ASC.Web.Api" name="inDto">Module request parameters</param>
    /// <path>api/2.0/settings/security</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.SecurityDto, ASC.Web.Api">Security settings</returns>
    /// <collection>list</collection>
    [HttpPut("")]
    public async Task<IEnumerable<SecurityDto>> SetWebItemSecurity(WebItemSecurityRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await webItemSecurity.SetSecurityAsync(inDto.Id, inDto.Enabled, inDto.Subjects?.ToArray());
        var securityInfo = await GetWebItemSecurityInfo(new List<string> { inDto.Id }).ToListAsync();

        if (inDto.Subjects == null)
        {
            return securityInfo;
        }

        var productName = GetProductName(new Guid(inDto.Id));

        if (!inDto.Subjects.Any())
        {
            await messageService.SendAsync(MessageAction.ProductAccessOpened, productName);
        }
        else
        {
            foreach (var info in securityInfo)
            {
                if (info.Groups.Any())
                {
                    await messageService.SendAsync(MessageAction.GroupsOpenedProductAccess, productName,
                        info.Groups.Select(x => x.Name));
                }

                if (info.Users.Any())
                {
                    await messageService.SendAsync(MessageAction.UsersOpenedProductAccess, productName,
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
    /// <category>Security</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WebItemSecurityRequestsDto, ASC.Web.Api" name="inDto">Module request parameters</param>
    /// <path>api/2.0/settings/security/access</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.SecurityDto, ASC.Web.Api">Security settings</returns>
    /// <collection>list</collection>
    [HttpPut("access")]
    public async Task<IEnumerable<SecurityDto>> SetAccessToWebItems(WebItemSecurityRequestsDto inDto)
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

        await messageService.SendAsync(MessageAction.ProductsListUpdated);

        return await GetWebItemSecurityInfo(itemList.Keys.ToList()).ToListAsync();
    }

    /// <summary>
    /// Returns a list of all the product administrators with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get the product administrators
    /// </short>
    /// <category>Security</category>
    /// <param type="System.Guid, System" method="url" name="productid">Product ID</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core">List of product administrators with the following parameters</returns>
    /// <path>api/2.0/settings/security/administrator/{productid}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("administrator/{productid:guid}")]
    public async IAsyncEnumerable<EmployeeDto> GetProductAdministrators(Guid productid)
    {
        var admins = await webItemSecurity.GetProductAdministratorsAsync(productid);

        foreach (var a in admins)
        {
            yield return await employeeWraperHelper.GetAsync(a);
        }
    }

    /// <summary>
    /// Checks if the selected user is a product administrator with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Check a product administrator
    /// </short>
    /// <category>Security</category>
    /// <param type="System.Guid, System" name="productid">Product ID</param>
    /// <param type="System.Guid, System" name="userid">User ID</param>
    /// <returns type="System.Object, System">Object with the user security information: product ID, user ID, administrator or not</returns>
    /// <path>api/2.0/settings/security/administrator</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("administrator")]
    public async Task<object> IsProductAdministratorAsync(Guid productid, Guid userid)
    {
        var result = await webItemSecurity.IsProductAdministratorAsync(productid, userid);
        return new { ProductId = productid, UserId = userid, Administrator = result };
    }

    /// <summary>
    /// Sets the selected user as a product administrator with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Set a product administrator
    /// </short>
    /// <category>Security</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.SecurityRequestsDto, ASC.Web.Api" name="inDto">Security request parameters</param>
    /// <returns type="System.Object, System">Object with the user security information: product ID, user ID, administrator or not</returns>
    /// <path>api/2.0/settings/security/administrator</path>
    /// <httpMethod>PUT</httpMethod>
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
            await messageService.SendAsync(messageAction, messageTarget.Create(admin.Id),
                admin.DisplayUserName(false, displayUserSettingsHelper));
        }
        else
        {
            var messageAction = inDto.Administrator
                ? MessageAction.ProductAddedAdministrator
                : MessageAction.ProductDeletedAdministrator;
            await messageService.SendAsync(messageAction, messageTarget.Create(admin.Id),
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
    /// <category>Login settings</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.LoginSettingsRequestDto, ASC.Web.Api" name="inDto">Login settings request parameters</param>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.LoginSettingsDto, ASC.Web.Api">Updated login settings</returns>
    /// <path>api/2.0/settings/security/loginsettings</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("loginSettings")]
    public async Task<LoginSettingsDto> UpdateLoginSettingsAsync(LoginSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var attemptsCount = inDto.AttemptCount;
        var checkPeriod = inDto.CheckPeriod;
        var blockTime = inDto.BlockTime;

        if (attemptsCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptsCount));
        }

        if (checkPeriod < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(checkPeriod));
        }

        if (blockTime < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(blockTime));
        }

        var settings = new LoginSettings
        {
            AttemptCount = attemptsCount, CheckPeriod = checkPeriod, BlockTime = blockTime
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
    /// <category>Login settings</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.LoginSettingsDto, ASC.Web.Api">Login settings</returns>
    /// <path>api/2.0/settings/security/loginsettings</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("loginSettings")]
    public async Task<LoginSettingsDto> GetLoginSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<LoginSettings>();

        return mapper.Map<LoginSettings, LoginSettingsDto>(settings);
    }
}