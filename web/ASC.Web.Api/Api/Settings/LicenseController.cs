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

[ApiEndpoint(Template = "license")]
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
    /// Re-reads the license file of this Enterprise installation and applies its quota and tariff to the portal. So a
    /// replaced file or a renewed license takes effect without a restart. If a file was staged by
    /// `POST api/2.0/settings/license`, the call activates it. For the first activation, use
    /// `POST api/2.0/settings/license/accept` instead. Any signed-in user can call it. Despite the `GET`, the call
    /// changes data. You can repeat it, and each call applies the same license again. The call returns `true` when
    /// it applied the license. It returns `false` and changes nothing on a SaaS or open-source installation, which
    /// has no license path. The call returns 402 if the file is missing, cannot be read, or has no customer id or
    /// signature. It also returns 402 if the editing service does not confirm the license or reports another one. If
    /// the license starts later, the call returns 400. The call works while the portal is unpaid.
    /// </remarks>
    /// <summary>Refresh the license</summary>
    /// <path>api/2.0/settings/license/refresh</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "`true` when the license file was re-read and the portal quota and tariff rewritten from it, `false` on an installation that has no license path configured", typeof(bool))]
    [SwaggerResponse(400, "The license file does not start until a later date")]
    [SwaggerResponse(402, "No license file is on disk, the file cannot be read as a license or carries no customer id or signature, or the editing service does not confirm it")]
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
    /// Activates the license file staged by `POST api/2.0/settings/license` on this Enterprise installation. The
    /// call makes the staged file the active one and applies its quota and tariff to the portal. Without a staged
    /// file, it applies the active license file again. Any signed-in user can call it. The call changes data, and
    /// you can repeat it. Read the result from the body, not from the status code. An empty string means the license
    /// is now active. Any other string is a message that explains why the activation failed. The call returns such a
    /// message for a missing or incorrect file, or a license the editing service rejects. It also returns a message
    /// if the license starts later. The call records the acceptance of the license even if the activation
    /// fails. On a SaaS or open-source installation, the call changes nothing. It returns a message that the pricing
    /// plan does not support the option. The call works while the portal is unpaid.
    /// </remarks>
    /// <summary>Activate a license</summary>
    /// <path>api/2.0/settings/license/accept</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "An empty string when the license is now active, or a localized sentence explaining why it was not activated", typeof(string))]
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
    /// Starts a 30-day trial license on this self-hosted installation. The trial keeps the current limits and
    /// features of the portal. Requires a DocSpace admin. Other callers get 403. The call changes data and returns
    /// `true` when it starts the trial. It returns `false` and changes nothing if the portal already has a trial,
    /// license, or due date. So a repeated call returns `false`. On a SaaS portal, the call fails.
    /// </remarks>
    /// <summary>
    /// Activate a trial license
    /// </summary>
    /// <path>api/2.0/settings/license/trial</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / License")]
    [SwaggerResponse(200, "`true` when the trial started, `false` when the portal already has a trial, a license, or a due date", typeof(bool))]
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
    /// Returns whether this installation still needs a license file. The setup wizard asks
    /// this before it offers to upload a license. Anyone can call it without signing in. The call is read-only. It
    /// returns `true` only for an Enterprise installation whose license file is not on disk yet. It returns `false`
    /// on a SaaS or open-source installation, which needs no license. It also returns `false` if the portal lets
    /// anyone in without an account, or hides the pricing section. The same applies to an installation set up from
    /// cloud image metadata. So `false` does not mean that the portal has a license. The call does not describe the
    /// active license or check it with the editing service. The answer becomes `false` only after a staged file is
    /// activated, not after the upload. To activate it, use `POST api/2.0/settings/license/accept`. The call works
    /// while the portal is unpaid.
    /// </remarks>
    /// <summary>Check if a license is required</summary>
    /// <path>api/2.0/settings/license/required</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "`true` when this Enterprise installation still needs a license file, `false` when one is already active or the build needs none", typeof(bool))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("required")]
    public async Task<bool> GetIsLicenseRequired()
    {
        return await firstTimeTenantSettings.GetRequestLicense();
    }


    /// <remarks>
    /// Uploads a license file for this Enterprise installation as `multipart/form-data` and stages it. The call reads
    /// only the first entry of `Files` and ignores the rest. It checks the file but does not activate it. To activate
    /// it, use `POST api/2.0/settings/license/accept`. Until then, the portal keeps its current license. Requires a
    /// DocSpace admin, or a wizard or admin link token before the setup wizard is complete. Other callers get 403,
    /// including such a token after the wizard is complete. The call changes data. A
    /// new upload replaces the staged file, so you can repeat it. The call returns a localized text that you can show
    /// to the user. For a license past its due date, the text adds the date when support and updates ended. The call
    /// still accepts such a license. The call returns 400 if the request has no file or the license starts
    /// later. It returns 415 on a SaaS or open-source installation, which takes no license file. The editing service
    /// checks the license only at activation.
    /// </remarks>
    /// <summary>Upload a license</summary>
    /// <path>api/2.0/settings/license</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "A localized confirmation that the file was staged, carrying the date support and updates ended when the license is already overdue", typeof(string))]
    [SwaggerResponse(400, "The request carried no license file, or the license does not start until a later date")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or a confirmation link was used after the setup wizard had already been completed")]
    [SwaggerResponse(415, "The installation has no license path configured, so it cannot be given a license file")]
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