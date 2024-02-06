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

[DefaultRoute("license")]
public class LicenseController(ILoggerProvider option,
        MessageService messageService,
        ApiContext apiContext,
        UserManager userManager,
        TenantManager tenantManager,
        TenantExtra tenantExtra,
        AuthContext authContext,
        LicenseReader licenseReader,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        CoreBaseSettings coreBaseSettings,
        IMemoryCache memoryCache,
        FirstTimeTenantSettings firstTimeTenantSettings,
        ITariffService tariffService,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    private readonly ILogger _log = option.CreateLogger("ASC.Api");

    /// <summary>
    /// Refreshes the license.
    /// </summary>
    /// <short>Refresh the license</short>
    /// <category>License</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/license/refresh</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("refresh")]
    [AllowNotPayment]
    public async Task<bool> RefreshLicenseAsync()
    {
        if (!coreBaseSettings.Standalone)
        {
            return false;
        }

        await licenseReader.RefreshLicenseAsync();
        return true;
    }

    /// <summary>
    /// Activates a license for the portal.
    /// </summary>
    /// <short>
    /// Activate a license
    /// </short>
    /// <category>License</category>
    /// <returns type="System.Object, System">Message about the result of activating license</returns>
    /// <path>api/2.0/settings/license/accept</path>
    /// <httpMethod>POST</httpMethod>
    [AllowNotPayment]
    [HttpPost("accept")]
    public async Task<object> AcceptLicenseAsync()
    {
        if (!coreBaseSettings.Standalone)
        {
            return "";
        }

        await TariffSettings.SetLicenseAcceptAsync(settingsManager);
        await messageService.SendAsync(MessageAction.LicenseKeyUploaded);

        try
        {
            await licenseReader.RefreshLicenseAsync();
        }
        catch (BillingNotFoundException)
        {
            return UserControlsCommonResource.LicenseKeyNotFound;
        }
        catch (BillingNotConfiguredException)
        {
            return UserControlsCommonResource.LicenseKeyNotCorrect;
        }
        catch (BillingException)
        {
            return UserControlsCommonResource.LicenseException;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return "";
    }

    /// <summary>
    /// Activates a trial license for the portal.
    /// </summary>
    /// <short>
    /// Activate a trial license
    /// </short>
    /// <category>License</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/settings/license/trial</path>
    /// <httpMethod>POST</httpMethod>
    ///<visible>false</visible>
    [HttpPost("trial")]
    public async Task<bool> ActivateTrialAsync()
    {
        if (!coreBaseSettings.Standalone)
        {
            throw new NotSupportedException();
        }

        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        var curQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (curQuota.TenantId != Tenant.DefaultTenant)
        {
            return false;
        }

        if (curQuota.Trial)
        {
            return false;
        }

        var curTariff = await tenantExtra.GetCurrentTariffAsync();
        if (curTariff.DueDate.Date != DateTime.MaxValue.Date)
        {
            return false;
        }

        var quota = new TenantQuota(-1000)
        {
            Name = "apirequest",
            CountUser = curQuota.CountUser,
            MaxFileSize = curQuota.MaxFileSize,
            MaxTotalSize = curQuota.MaxTotalSize,
            Features = curQuota.Features
        };
        quota.Trial = true;

        await tenantManager.SaveTenantQuotaAsync(quota);

        const int DEFAULT_TRIAL_PERIOD = 30;

        var tariff = new Tariff
        {
            Quotas = [new(quota.TenantId, 1)],
            DueDate = DateTime.Today.AddDays(DEFAULT_TRIAL_PERIOD)
        };

        await tariffService.SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);

        await messageService.SendAsync(MessageAction.LicenseKeyUploaded);

        return true;
    }

    /// <summary>
    /// Requests a portal license if necessary.
    /// </summary>
    /// <short>
    /// Request a license
    /// </short>
    /// <category>License</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the license is required</returns>
    /// <path>api/2.0/settings/license/required</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("required")]
    public bool RequestLicense()
    {
        return firstTimeTenantSettings.RequestLicense;
    }


    /// <summary>
    /// Uploads a portal license specified in the request.
    /// </summary>
    /// <short>
    /// Upload a license
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.UploadLicenseRequestsDto, ASC.Web.Api" name="inDto">Request parameters to upload a license</param>
    /// <category>License</category>
    /// <returns type="System.Object, System">License</returns>
    /// <path>api/2.0/settings/license</path>
    /// <httpMethod>POST</httpMethod>
    [AllowNotPayment]
    [HttpPost("")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard, Administrators")]
    public async Task<object> UploadLicenseAsync([FromForm] UploadLicenseRequestsDto inDto)
    {
        try
        {
            await ApiContext.AuthByClaimAsync();
            if (!authContext.IsAuthenticated && (await settingsManager.LoadAsync<WizardSettings>()).Completed)
            {
                throw new SecurityException(Resource.PortalSecurity);
            }

            if (!coreBaseSettings.Standalone)
            {
                throw new NotSupportedException();
            }

            if (!inDto.Files.Any())
            {
                throw new Exception(Resource.ErrorEmptyUploadFileSelected);
            }

            var licenseFile = inDto.Files.First();
            var dueDate = licenseReader.SaveLicenseTemp(licenseFile.OpenReadStream());

            return dueDate >= DateTime.UtcNow.Date
                                    ? Resource.LicenseUploaded
                                    : string.Format(
                                        (await tenantManager.GetCurrentTenantQuotaAsync()).Update
                                            ? Resource.LicenseUploadedOverdueSupport
                                            : Resource.LicenseUploadedOverdue,
                                                    "",
                                                    "",
                                                    dueDate.Date.ToLongDateString());
        }
        catch (LicenseExpiredException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw new Exception(Resource.LicenseErrorExpired);
        }
        catch (LicenseQuotaException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw new Exception(Resource.LicenseErrorQuota);
        }
        catch (LicensePortalException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw new Exception(Resource.LicenseErrorPortal);
        }
        catch (Exception ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw new Exception(Resource.LicenseError);
        }
    }
}