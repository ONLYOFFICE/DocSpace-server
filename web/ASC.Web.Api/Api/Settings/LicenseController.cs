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

[DefaultRoute("license")]
public class LicenseController(
    ILoggerFactory loggerFactory,
    MessageService messageService,
    SecurityContext securityContext,
    UserManager userManager,
    TenantManager tenantManager,
    TenantLogoManager tenantLogoManager,
    TenantExtra tenantExtra,
    AuthContext authContext,
    LicenseReader licenseReader,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    CoreBaseSettings coreBaseSettings,
    IFusionCache fusionCache,
    FirstTimeTenantSettings firstTimeTenantSettings,
    ITariffService tariffService,
    DocumentServiceLicense documentServiceLicense)
    : BaseSettingsController(fusionCache, webItemManager)
{
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Api");

    /// <remarks>
    /// Refreshes the portal license.
    /// </remarks>
    /// <summary>Refresh the license</summary>
    /// <path>api/2.0/settings/license/refresh</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpGet("refresh")]
    [AllowNotPayment]
    public async Task<bool> RefreshLicense()
    {
        if (!tenantExtra.Enterprise)
        {
            return false;
        }

        await licenseReader.RefreshLicenseAsync(documentServiceLicense.ValidateLicense);
        return true;
    }

    /// <remarks>
    /// Activates a license for the portal.
    /// </remarks>
    /// <summary>
    /// Activate a license
    /// </summary>
    /// <path>api/2.0/settings/license/accept</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "Message about the result of activating license", typeof(string))]
    [AllowNotPayment]
    [HttpPost("accept")]
    public async Task<string> AcceptLicense()
    {
        if (!tenantExtra.Enterprise)
        {
            return Resource.ErrorNotAllowedOption;
        }

        await TariffSettings.SetLicenseAcceptAsync(settingsManager);
        messageService.Send(MessageAction.LicenseKeyUploaded);

        try
        {
            await licenseReader.RefreshLicenseAsync(documentServiceLicense.ValidateLicense);
        }
        catch (BillingNotFoundException)
        {
            return UserControlsCommonResource.LicenseKeyNotFound;
        }
        catch (BillingNotConfiguredException ex)
        {
            _log.ErrorWithException(ex);
            return UserControlsCommonResource.LicenseKeyNotCorrect;
        }
        catch (BillingLicenseTypeException)
        {
            var logoText = await tenantLogoManager.GetLogoTextAsync();
            return string.Format(UserControlsCommonResource.LicenseTypeNotCorrect, logoText);
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

    /// <remarks>
    /// Activates a trial license for the portal.
    /// </remarks>
    /// <summary>
    /// Activate a trial license
    /// </summary>
    /// <path>api/2.0/settings/license/trial</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / License")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("trial")]
    public async Task<bool> ActivateTrialLicense()
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
            Features = curQuota.Features,
            Trial = true
        };

        await tenantManager.SaveTenantQuotaAsync(quota);

        const int DEFAULT_TRIAL_PERIOD = 30;

        var tariff = new Tariff
        {
            Quotas = [new Quota(quota.TenantId, 1)],
            DueDate = DateTime.Today.AddDays(DEFAULT_TRIAL_PERIOD)
        };

        await tariffService.SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);

        messageService.Send(MessageAction.LicenseKeyUploaded);

        return true;
    }

    /// <remarks>
    /// Requests a portal license if necessary.
    /// </remarks>
    /// <summary>
    /// Request a license
    /// </summary>
    /// <path>api/2.0/settings/license/required</path>
    /// <requiresAuthorization>false</requiresAuthorization>\
    [Tags("Settings / License")]
    [SwaggerResponse(200, "Boolean value: true if the license is required", typeof(bool))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("required")]
    public async Task<bool> GetIsLicenseRequired()
    {
        return await firstTimeTenantSettings.GetRequestLicense();
    }


    /// <remarks>
    /// Uploads a portal license specified in the request.
    /// </remarks>
    /// <summary>
    /// Upload a license
    /// </summary>
    /// <path>api/2.0/settings/license</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "License", typeof(string))]
    [SwaggerResponse(400, "The uploaded file could not be found")]
    [SwaggerResponse(403, "Portal Access")]
    [SwaggerResponse(405, "Your pricing plan does not support this option")]
    [AllowNotPayment]
    [HttpPost("")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard, Administrators")]
    public async Task<string> UploadLicense([FromForm] UploadLicenseRequestsDto inDto)
    {
        try
        {
            await securityContext.AuthByClaimAsync();
            if (!authContext.IsAuthenticated && (await settingsManager.LoadAsync<WizardSettings>()).Completed)
            {
                throw new SecurityException(Resource.PortalSecurity);
            }

            if (!tenantExtra.Enterprise)
            {
                throw new NotSupportedException(Resource.ErrorNotAllowedOption);
            }

            if (!inDto.Files.Any())
            {
                throw new ArgumentException(Resource.ErrorEmptyUploadFileSelected);
            }

            var licenseFile = inDto.Files.First();
            var dueDate = await licenseReader.SaveLicenseTemp(licenseFile.OpenReadStream());

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
        catch (SecurityException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw;
        }
        catch (NotSupportedException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw;
        }
        catch (ArgumentException ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw;
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
        catch (BillingLicenseTypeException ex)
        {
            _log.ErrorLicenseUpload(ex);
            var logoText = await tenantLogoManager.GetLogoTextAsync();
            throw new Exception(string.Format(UserControlsCommonResource.LicenseTypeNotCorrect, logoText));
        }
        catch (Exception ex)
        {
            _log.ErrorLicenseUpload(ex);
            throw new Exception(Resource.LicenseError);
        }
    }
}