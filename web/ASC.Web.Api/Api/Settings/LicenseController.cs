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
    /// Re-reads the license file of this self-hosted Enterprise installation and rewrites the portal-wide quota and
    /// tariff from it, so a file replaced on disk or a renewal issued by the vendor takes effect without a restart. A
    /// license staged by `POST api/2.0/settings/license` is promoted to the active one here as well, but the usual
    /// first-time order is upload and then `POST api/2.0/settings/license/accept`; this operation is for later
    /// refreshes. The caller only has to be signed in - no administrator right is checked. Despite the `GET`, the
    /// call rewrites stored data, and it is idempotent: repeating it applies the same license again. The editing
    /// service is asked to confirm the license as part of the check, and the license it reports must match the file.
    /// The answer is `true` when the license was applied and `false` on an installation with no license path
    /// configured at all, such as a SaaS or open-source portal, where nothing is read and nothing changes. A missing
    /// or unreadable file, a mismatched customer or edition, and an editing service that rejects the license all fail
    /// the call instead of answering `false`. The operation stays reachable while the portal is unpaid.
    /// </remarks>
    /// <summary>Refresh the license</summary>
    /// <path>api/2.0/settings/license/refresh</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "`true` when the license file was re-read and the portal quota and tariff rewritten from it, `false` on an installation that has no license path configured", typeof(bool))]
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
    /// Activates the license staged by `POST api/2.0/settings/license` on this self-hosted Enterprise installation:
    /// it records that the license was accepted, promotes the staged file to the active one and rewrites the
    /// portal-wide quota and tariff from it. Upload a file first: with nothing staged and no license on disk there is
    /// nothing to activate. The caller only has to be signed in, and the activation is recorded in the audit trail.
    /// Repeating the call is safe: the acceptance stamp is written only once and the same license is simply applied
    /// again. Read the outcome from the body rather than the status code - an empty string means the license is now
    /// active, and any other string is a message explaining why it is not: no license key was found, the key is not
    /// correct, the installed edition does not match the license type, or the license is expired or too small for the
    /// current user count. The acceptance stamp survives a failed activation, so a corrected file needs nothing
    /// extra. An installation with no license path configured answers that its pricing plan does not support the
    /// option and changes nothing. The operation stays reachable while the portal is unpaid.
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
    /// Reports whether this installation still has to be given a license file before it can be used, which is the
    /// question the setup wizard asks before offering its license upload step. No authentication is needed, so it can
    /// be called on a portal nobody has signed in to yet, and the call is read-only. The answer is `true` only for a
    /// self-hosted Enterprise build whose license file is not on disk yet; an open-source or SaaS portal, a portal
    /// configured to let anyone in without an account, an installation whose configuration hides the pricing section,
    /// and one that takes its setup from cloud-image metadata all answer `false`. A `false` answer therefore does not
    /// mean the portal is licensed - it also covers every build that needs no license at all. Nothing here describes
    /// a license already in place, neither its due date nor whether the editing service still accepts it, and the
    /// answer turns to `false` only once a staged file has been activated by `POST api/2.0/settings/license/accept`,
    /// not when it is uploaded. The operation stays reachable while the portal is unpaid.
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
    /// Takes the license file of this self-hosted Enterprise installation as `multipart/form-data` and stages it for
    /// activation; only the first entry of `Files` is read and the rest are ignored. The file is validated but not
    /// put in force here - follow with `POST api/2.0/settings/license/accept` to activate it, and until then the
    /// portal keeps the license it already had. The caller must be a DocSpace administrator, or hold a wizard or
    /// administrator confirmation link while the setup wizard is still unfinished; after the wizard is complete such
    /// a link alone is refused. An earlier staged file is overwritten, so the upload can be repeated safely. The
    /// answer is a localized sentence, not a structured result: `Uploaded successfully` on its own, or the same words
    /// plus the date since when support and updates are not covered, because a file already past its due date is
    /// still accepted. A request carrying no file, and a license whose start date has not arrived yet, are rejected
    /// as invalid; a file that cannot be read as a license, carries no customer id or signature, or was issued for
    /// the other edition fails the call. Whether the editing service accepts it is only checked at activation.
    /// </remarks>
    /// <summary>Upload a license</summary>
    /// <path>api/2.0/settings/license</path>
    [Tags("Settings / License")]
    [SwaggerResponse(200, "A localized confirmation that the file was staged, carrying the date support and updates ended when the license is already overdue", typeof(string))]
    [SwaggerResponse(400, "The request carried no license file, or the license does not start until a later date")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or a confirmation link was used after the setup wizard had already been completed")]
    [SwaggerResponse(405, "The installation has no license path configured, so it cannot be given a license file")]
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