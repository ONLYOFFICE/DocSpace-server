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

namespace ASC.Web.Api.Controllers.Settings;

public class WhitelabelController : BaseSettingsController
{
    private readonly PermissionContext _permissionContext;
    private readonly SettingsManager _settingsManager;
    private readonly TenantInfoSettingsHelper _tenantInfoSettingsHelper;
    private readonly TenantWhiteLabelSettingsHelper _tenantWhiteLabelSettingsHelper;
    private readonly TenantLogoManager _tenantLogoManager;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly IMapper _mapper;
    private readonly CompanyWhiteLabelSettingsHelper _companyWhiteLabelSettingsHelper;
    private readonly TenantManager _tenantManager;
    private readonly TenantExtra _tenantExtra;
    private readonly StorageFactory _storageFactory;

    public WhitelabelController(
        ApiContext apiContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        TenantInfoSettingsHelper tenantInfoSettingsHelper,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
        TenantLogoManager tenantLogoManager,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        CompanyWhiteLabelSettingsHelper companyWhiteLabelSettingsHelper,
        TenantManager tenantManager,
        TenantExtra tenantExtra,
        StorageFactory storageFactory)
        : base(apiContext, memoryCache, webItemManager, httpContextAccessor)
    {
        _permissionContext = permissionContext;
        _settingsManager = settingsManager;
        _tenantInfoSettingsHelper = tenantInfoSettingsHelper;
        _tenantWhiteLabelSettingsHelper = tenantWhiteLabelSettingsHelper;
        _tenantLogoManager = tenantLogoManager;
        _coreBaseSettings = coreBaseSettings;
        _commonLinkUtility = commonLinkUtility;
        _mapper = mapper;
        _companyWhiteLabelSettingsHelper = companyWhiteLabelSettingsHelper;
        _tenantManager = tenantManager;
        _tenantExtra = tenantExtra;
        _storageFactory = storageFactory;
    }

    /// <summary>
    /// Saves the white label settings specified in the request.
    /// </summary>
    /// <short>
    /// Save the white label settings
    /// </short>
    /// <category>Rebranding</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelRequestsDto, ASC.Web.Api" name="inDto">Request parameters for white label settings</param>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelQueryRequestsDto, ASC.Web.Api" name="inQueryDto">White label request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is sucessful</returns>
    /// <path>api/2.0/settings/whitelabel/save</path>
    /// <httpMethod>POST</httpMethod>
    ///<visible>false</visible>
    [HttpPost("whitelabel/save")]
    public async Task<bool> SaveWhiteLabelSettingsAsync(WhiteLabelRequestsDto inDto, [FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inQueryDto != null && inQueryDto.IsDefault.HasValue && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            await SaveWhiteLabelSettingsForDefaultTenantAsync(inDto);
        }
        else
        {
            await DemandWhiteLabelPermissionAsync();

            await SaveWhiteLabelSettingsForCurrentTenantAsync(inDto);
        }

        return true;
    }

    private async Task SaveWhiteLabelSettingsForCurrentTenantAsync(WhiteLabelRequestsDto inDto)
    {
        var settings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        await SaveWhiteLabelSettingsForTenantAsync(settings, null, tenant.Id, inDto);
    }

    private async Task SaveWhiteLabelSettingsForDefaultTenantAsync(WhiteLabelRequestsDto inDto)
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        var storage = await _storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await SaveWhiteLabelSettingsForTenantAsync(settings, storage, Tenant.DefaultTenant, inDto);
    }

    private async Task SaveWhiteLabelSettingsForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId, WhiteLabelRequestsDto inDto)
    {
        if (inDto.Logo != null)
        {
            var logoDict = new Dictionary<int, KeyValuePair<string, string>>();

            foreach (var l in inDto.Logo)
            {
                var key = Int32.Parse(l.Key);

                logoDict.Add(key, new KeyValuePair<string, string>(l.Value.Light, l.Value.Dark));
            }

            await _tenantWhiteLabelSettingsHelper.SetLogo(settings, logoDict, storage);
        }

        settings.SetLogoText(inDto.LogoText);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        await _tenantWhiteLabelSettingsHelper.SaveAsync(settings, tenant.Id, _tenantLogoManager);

        await _tenantWhiteLabelSettingsHelper.SaveAsync(settings, tenantId, _tenantLogoManager);
    }

    /// <summary>
    /// Saves the white label settings from files.
    /// </summary>
    /// <short>
    /// Save the white label settings from files
    /// </short>
    /// <category>Rebranding</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelQueryRequestsDto, ASC.Web.Api" name="inQueryDto">White label request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/whitelabel/savefromfiles</path>
    /// <httpMethod>POST</httpMethod>
    ///<visible>false</visible>
    [HttpPost("whitelabel/savefromfiles")]
    public async Task<bool> SaveWhiteLabelSettingsFromFilesAsync([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new InvalidOperationException("No input files");
        }

        if (inQueryDto != null && inQueryDto.IsDefault.HasValue && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            await SaveWhiteLabelSettingsFromFilesForDefaultTenantAsync();
        }
        else
        {
            await DemandWhiteLabelPermissionAsync();

            await SaveWhiteLabelSettingsFromFilesForCurrentTenantAsync();
        }

        return true;
    }

    private async Task SaveWhiteLabelSettingsFromFilesForCurrentTenantAsync()
    {
        var settings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        await SaveWhiteLabelSettingsFromFilesForTenantAsync(settings, null, tenant.Id);
    }

    private async Task SaveWhiteLabelSettingsFromFilesForDefaultTenantAsync()
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        var storage = await _storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await SaveWhiteLabelSettingsFromFilesForTenantAsync(settings, storage, Tenant.DefaultTenant);
    }

    private async Task SaveWhiteLabelSettingsFromFilesForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId)
    {
        foreach (var f in HttpContext.Request.Form.Files)
        {
            if (f.FileName.Contains("dark"))
            {
                GetParts(f.FileName, out var logoType, out var fileExt);

                await _tenantWhiteLabelSettingsHelper.SetLogoFromStream(settings, logoType, fileExt, f.OpenReadStream(), true, storage);
            }
            else
            {
                GetParts(f.FileName, out var logoType, out var fileExt);

                await _tenantWhiteLabelSettingsHelper.SetLogoFromStream(settings, logoType, fileExt, f.OpenReadStream(), false, storage);
            }
        }

        await _settingsManager.SaveAsync(settings, tenantId);
    }

    private void GetParts(string fileName, out WhiteLabelLogoType logoType, out string fileExt)
    {
        var parts = fileName.Split('.');
        logoType = (WhiteLabelLogoType)Convert.ToInt32(parts[0]);
        fileExt = parts[^1];
    }

    /// <summary>
    /// Returns the white label logos.
    /// </summary>
    /// <short>
    /// Get the white label logos
    /// </short>
    /// <category>Rebranding</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelQueryRequestsDto, ASC.Web.Api" name="inQueryDto">White label request parameters</param>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WhiteLabelItemDto, ASC.Web.Api">White label logos</returns>
    /// <path>api/2.0/settings/whitelabel/logos</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [AllowNotPayment, AllowAnonymous, AllowSuspended]
    [HttpGet("whitelabel/logos")]
    public async IAsyncEnumerable<WhiteLabelItemDto> GetWhiteLabelLogosAsync([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        var isDefault = inQueryDto != null && inQueryDto.IsDefault.HasValue && inQueryDto.IsDefault.Value;

        var tenantWhiteLabelSettings = isDefault ? null : await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        foreach (var logoType in (WhiteLabelLogoType[])Enum.GetValues(typeof(WhiteLabelLogoType)))
        {
            if (logoType == WhiteLabelLogoType.Notification)
            {
                continue;
            }

            var result = new WhiteLabelItemDto
            {
                Name = logoType.ToString(),
                Size = TenantWhiteLabelSettings.GetSize(logoType)
            };

            if (inQueryDto != null && inQueryDto.IsDark.HasValue)
            {
                var path = _commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await _tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, inQueryDto.IsDark.Value)
                    : await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, inQueryDto.IsDark.Value));

                if (inQueryDto.IsDark.Value)
                {
                    result.Path = new WhiteLabelItemPathDto
                    {
                        Dark = path
                    };
                }
                else
                {
                    result.Path = new WhiteLabelItemPathDto
                    {
                        Light = path
                    };
                }
            }
            else
            {
                var lightPath = _commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await _tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, false)
                    : await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, false));

                var darkPath = _commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await _tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, true)
                    : await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, true));

                if (lightPath == darkPath)
                {
                    darkPath = null;
                }

                result.Path = new WhiteLabelItemPathDto
                {
                    Light = lightPath,
                    Dark = darkPath
                };
            }

            yield return result;
        }
    }

    /// <summary>
    /// Specifies if the white label logos are default or not.
    /// </summary>
    /// <short>
    /// Check the default white label logos
    /// </short>
    /// <category>Rebranding</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.IsDefaultWhiteLabelLogosDto, ASC.Web.Api">Request properties of white label logos</returns>
    /// <path>api/2.0/settings/whitelabel/logos/isdefault</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpGet("whitelabel/logos/isdefault")]
    public async IAsyncEnumerable<IsDefaultWhiteLabelLogosDto> GetIsDefaultWhiteLabelLogos()
    {
        var tenantWhiteLabelSettings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        yield return new IsDefaultWhiteLabelLogosDto
        {
            Name = "logotext",
            Default = tenantWhiteLabelSettings.LogoText.IsNullOrEmpty() || tenantWhiteLabelSettings.LogoText.Equals(TenantWhiteLabelSettings.DefaultLogoText)
        };
        foreach (var logoType in (WhiteLabelLogoType[])Enum.GetValues(typeof(WhiteLabelLogoType)))
        {
            var result = new IsDefaultWhiteLabelLogosDto
            {
                Name = logoType.ToString(),
                Default = tenantWhiteLabelSettings.GetIsDefault(logoType)
            };

            yield return result;
        }
    }

    /// <summary>
    /// Returns the white label logo text.
    /// </summary>
    /// <short>
    /// Get the white label logo text
    /// </short>
    /// <category>Rebranding</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelQueryRequestsDto, ASC.Web.Api" name="inQueryDto">White label request parameters</param>
    /// <returns type="System.Object, System">Logo text</returns>
    /// <path>api/2.0/settings/whitelabel/logotext</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [AllowNotPayment]
    [HttpGet("whitelabel/logotext")]
    public async Task<object> GetWhiteLabelLogoTextAsync([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = inQueryDto != null && inQueryDto.IsDefault.HasValue && inQueryDto.IsDefault.Value
            ? await _settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>()
            : await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        return settings.LogoText ?? TenantWhiteLabelSettings.DefaultLogoText;
    }


    /// <summary>
    /// Restores the white label options.
    /// </summary>
    /// <short>
    /// Restore the white label options
    /// </short>
    /// <category>Rebranding</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WhiteLabelQueryRequestsDto, ASC.Web.Api" name="inQueryDto">White label request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/whitelabel/restore</path>
    /// <httpMethod>PUT</httpMethod>
    /// <visible>false</visible>
    [HttpPut("whitelabel/restore")]
    public async Task<bool> RestoreWhiteLabelOptionsAsync(WhiteLabelQueryRequestsDto inQueryDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inQueryDto != null && inQueryDto.IsDefault.HasValue && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            await RestoreWhiteLabelOptionsForDefaultTenantAsync();
        }
        else
        {
            await DemandWhiteLabelPermissionAsync();

            await RestoreWhiteLabelOptionsForCurrentTenantAsync();
        }

        return true;
    }

    private async Task RestoreWhiteLabelOptionsForCurrentTenantAsync()
    {
        var settings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        var tenant = await _tenantManager.GetCurrentTenantAsync();
        

        await RestoreWhiteLabelOptionsForTenantAsync(settings, null, tenant.Id);

        var tenantInfoSettings = await _settingsManager.LoadAsync<TenantInfoSettings>();
        await _tenantInfoSettingsHelper.RestoreDefaultLogoAsync(tenantInfoSettings, _tenantLogoManager);
        await _settingsManager.SaveAsync(tenantInfoSettings);
    }

    private async Task RestoreWhiteLabelOptionsForDefaultTenantAsync()
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();
        var storage = await _storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await RestoreWhiteLabelOptionsForTenantAsync(settings, storage, Tenant.DefaultTenant);
    }

    private async Task RestoreWhiteLabelOptionsForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId)
    {
        await _tenantWhiteLabelSettingsHelper.RestoreDefault(settings, _tenantLogoManager, tenantId, storage);
    }

    /// <summary>
    /// Returns the licensor data.
    /// </summary>
    /// <short>Get the licensor data</short>
    /// <category>Rebranding</category>
    /// <returns type="ASC.Web.Core.WhiteLabel.CompanyWhiteLabelSettings, ASC.Web.Core">List of company white label settings</returns>
    /// <path>api/2.0/settings/companywhitelabel</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpGet("companywhitelabel")]
    public async Task<List<CompanyWhiteLabelSettings>> GetLicensorDataAsync()
    {
        var result = new List<CompanyWhiteLabelSettings>();

        var instance = await _companyWhiteLabelSettingsHelper.InstanceAsync();

        result.Add(instance);

        if (!_companyWhiteLabelSettingsHelper.IsDefault(instance) && !instance.IsLicensor)
        {
            result.Add(_settingsManager.GetDefault<CompanyWhiteLabelSettings>());
        }

        return result;
    }

    /// <summary>
    /// Saves the company white label settings specified in the request.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Save the company white label settings</short>
    /// <param type="ASC.Web.Core.WhiteLabel.CompanyWhiteLabelSettingsWrapper, ASC.Web.Core" name="companyWhiteLabelSettingsWrapper">Company white label settings</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/rebranding/company</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("rebranding/company")]
    public async Task<bool> SaveCompanyWhiteLabelSettingsAsync(CompanyWhiteLabelSettingsWrapper companyWhiteLabelSettingsWrapper)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        if (companyWhiteLabelSettingsWrapper.Settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        companyWhiteLabelSettingsWrapper.Settings.IsLicensor = false;

        await _settingsManager.SaveForDefaultTenantAsync(companyWhiteLabelSettingsWrapper.Settings);

        return true;
    }

    /// <summary>
    /// Returns the company white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Get the company white label settings</short>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CompanyWhiteLabelSettingsDtov, ASC.Web.Api">Company white label settings</returns>
    /// <path>api/2.0/settings/rebranding/company</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [AllowNotPayment]
    [HttpGet("rebranding/company")]
    public async Task<CompanyWhiteLabelSettingsDto> GetCompanyWhiteLabelSettingsAsync()
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>();

        return _mapper.Map<CompanyWhiteLabelSettings, CompanyWhiteLabelSettingsDto>(settings);
    }

    /// <summary>
    /// Deletes the company white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Delete the company white label settings</short>
    /// <returns type="ASC.Web.Core.WhiteLabel.CompanyWhiteLabelSettings, ASC.Web.Core">Default company white label settings</returns>
    /// <path>api/2.0/settings/rebranding/company</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <visible>false</visible>
    [HttpDelete("rebranding/company")]
    public async Task<CompanyWhiteLabelSettings> DeleteCompanyWhiteLabelSettingsAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        var defaultSettings = _settingsManager.GetDefault<CompanyWhiteLabelSettings>();

        await _settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        return defaultSettings;
    }

    /// <summary>
    /// Saves the additional white label settings specified in the request.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Save the additional white label settings</short>
    /// <param type="ASC.Web.Core.WhiteLabel.AdditionalWhiteLabelSettingsWrapper, ASC.Web.Core" name="wrapper">Additional white label settings</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/rebranding/additional</path>
    /// <httpMethod>POST</httpMethod>
    ///<visible>false</visible>
    [HttpPost("rebranding/additional")]
    public async Task<bool> SaveAdditionalWhiteLabelSettingsAsync(AdditionalWhiteLabelSettingsWrapper wrapper)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        if (wrapper.Settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        await _settingsManager.SaveForDefaultTenantAsync(wrapper.Settings);

        return true;
    }

    /// <summary>
    /// Returns the additional white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Get the additional white label settings</short>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.AdditionalWhiteLabelSettingsDto, ASC.Web.Api">Additional white label settings</returns>
    /// <path>api/2.0/settings/rebranding/additional</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [AllowNotPayment]
    [HttpGet("rebranding/additional")]
    public async Task<AdditionalWhiteLabelSettingsDto> GetAdditionalWhiteLabelSettingsAsync()
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();

        return _mapper.Map<AdditionalWhiteLabelSettings, AdditionalWhiteLabelSettingsDto>(settings);
    }

    /// <summary>
    /// Deletes the additional white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Delete the additional white label settings</short>
    /// <returns type="ASC.Web.Core.WhiteLabel.AdditionalWhiteLabelSettings, ASC.Web.Core">Default additional white label settings</returns>
    /// <path>api/2.0/settings/rebranding/additional</path>
    /// <httpMethod>DELETE</httpMethod>
    ///<visible>false</visible>
    [HttpDelete("rebranding/additional")]
    public async Task<AdditionalWhiteLabelSettings> DeleteAdditionalWhiteLabelSettingsAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        var defaultSettings = _settingsManager.GetDefault<AdditionalWhiteLabelSettings>();

        await _settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        return defaultSettings;
    }

    /// <summary>
    /// Saves the mail white label settings specified in the request.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Save the mail white label settings</short>
    /// <param type="ASC.Web.Core.WhiteLabel.MailWhiteLabelSettings, ASC.Web.Core" name="settings">Mail white label settings</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/rebranding/mail</path>
    /// <httpMethod>POST</httpMethod>
    ///<visible>false</visible>
    [HttpPost("rebranding/mail")]
    public async Task<bool> SaveMailWhiteLabelSettingsAsync(MailWhiteLabelSettings settings)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(settings);

        await _settingsManager.SaveForDefaultTenantAsync(settings);

        return true;
    }

    /// <summary>
    /// Updates the mail white label settings with a paramater specified in the request.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Update the mail white label settings</short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.MailWhiteLabelSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for mail white label settings</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/rebranding/mail</path>
    /// <httpMethod>PUT</httpMethod>
    ///<visible>false</visible>
    [HttpPut("rebranding/mail")]
    public async Task<bool> UpdateMailWhiteLabelSettings(MailWhiteLabelSettingsRequestsDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        var settings = await _settingsManager.LoadForDefaultTenantAsync<MailWhiteLabelSettings>();

        settings.FooterEnabled = inDto.FooterEnabled;

        await _settingsManager.SaveForDefaultTenantAsync(settings);

        return true;
    }

    /// <summary>
    /// Returns the mail white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Get the mail white label settings</short>
    /// <returns type="ASC.Web.Core.WhiteLabel.MailWhiteLabelSettings, ASC.Web.Core">Mail white label settings</returns>
    /// <path>api/2.0/settings/rebranding/mail</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [HttpGet("rebranding/mail")]
    public async Task<MailWhiteLabelSettings> GetMailWhiteLabelSettingsAsync()
    {
        return await _settingsManager.LoadForDefaultTenantAsync<MailWhiteLabelSettings>();
    }

    /// <summary>
    /// Deletes the mail white label settings.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Delete the mail white label settings</short>
    /// <returns type="ASC.Web.Core.WhiteLabel.MailWhiteLabelSettings, ASC.Web.Core">Default mail white label settings</returns>
    /// <path>api/2.0/settings/rebranding/mail</path>
    /// <httpMethod>DELETE</httpMethod>
    ///<visible>false</visible>
    [HttpDelete("rebranding/mail")]
    public async Task<MailWhiteLabelSettings> DeleteMailWhiteLabelSettingsAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        var defaultSettings = _settingsManager.GetDefault<MailWhiteLabelSettings>();

        await _settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        return defaultSettings;
    }

    /// <summary>
    /// Checks if the white label is enabled or not.
    /// </summary>
    /// <category>Rebranding</category>
    /// <short>Check the white label availability</short>
    /// <returns type="System.Boolean, System">Boolean value: true if the white label is enabled</returns>
    /// <path>api/2.0/settings/enableWhitelabel</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [HttpGet("enableWhitelabel")]
    public async Task<bool> GetEnableWhitelabelAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return _coreBaseSettings.Standalone || _tenantLogoManager.WhiteLabelEnabled && await _tenantLogoManager.GetWhiteLabelPaidAsync();
    }

    private async Task DemandWhiteLabelPermissionAsync()
    {
        if (!_coreBaseSettings.Standalone && (!_tenantLogoManager.WhiteLabelEnabled || !await _tenantLogoManager.GetWhiteLabelPaidAsync()))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
        }
    }

    private async Task DemandRebrandingPermissionAsync()
    {
        await _tenantExtra.DemandAccessSpacePermissionAsync();

        if (_coreBaseSettings.CustomMode)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
        await DemandWhiteLabelPermissionAsync();
    }
}
