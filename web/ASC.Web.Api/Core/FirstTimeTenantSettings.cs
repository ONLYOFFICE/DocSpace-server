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

namespace ASC.Web.Studio.UserControls.FirstTime;

[Transient]
public class FirstTimeTenantSettings(
    ILogger<FirstTimeTenantSettings> logger,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    TenantLogoManager tenantLogoManager,
    SettingsManager settingsManager,
    UserManager userManager,
    SetupInfo setupInfo,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    SecurityContext securityContext,
    MessageService messageService,
    LicenseReader licenseReader,
    StudioNotifyService studioNotifyService,
    CoreBaseSettings coreBaseSettings,
    IHttpClientFactory clientFactory,
    CookiesManager cookiesManager,
    CspSettingsHelper cspSettingsHelper,
    DocumentServiceLicense documentServiceLicense)
{
    public async Task<WizardSettings> SaveDataAsync(WizardRequestsDto inDto)
    {
        try
        {
            var (email, passwordHash, lng, timeZone, amiid, subscribeFromSite) = inDto;

            var tenant = tenantManager.GetCurrentTenant();
            var settings = await settingsManager.LoadAsync<WizardSettings>();
            if (settings.Completed)
            {
                throw new Exception("Wizard passed.");
            }

            var ami = !string.IsNullOrEmpty(setupInfo.AmiMetaUrl);

            if (ami && await IncorrectAmiId(amiid))
            {
                throw new Exception(Resource.EmailAndPasswordIncorrectAmiId);
            }

            if (tenant.OwnerId == Guid.Empty)
            {
                await Task.Delay(TimeSpan.FromSeconds(6));// wait cache interval
                tenant = await tenantManager.GetTenantAsync(tenant.Id);
                if (tenant.OwnerId == Guid.Empty)
                {
                    logger.ErrorOwnerEmpty(tenant.Id);
                }
            }

            var currentUser = await userManager.GetUsersAsync(tenantManager.GetCurrentTenant().OwnerId);

            if (!UserManagerWrapper.ValidateEmail(email))
            {
                throw new Exception(Resource.EmailAndPasswordIncorrectEmail);
            }

            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new Exception(Resource.ErrorPasswordEmpty);
            }

            if ((await tenantExtra.GetEnableTariffSettings() || ami) && tenantExtra.Enterprise)
            {
                if (await ApplyDocsTrialAsync())
                {
                    // Completing the wizard includes agreeing to the license terms, so the license counts as accepted
                    // here as well: a license file that later appears on the license path is applied by the background
                    // refresh without a separate accept
                    await TariffSettings.SetLicenseAcceptAsync(settingsManager);
                }
                else
                {
                    await licenseReader.RefreshLicenseAsync(documentServiceLicense.ValidateLicense);

                    await TariffSettings.SetLicenseAcceptAsync(settingsManager);
                    messageService.Send(MessageAction.LicenseKeyUploaded);
                }
            }

            await securityContext.SetUserPasswordHashAsync(currentUser.Id, passwordHash);

            email = email.Trim();
            if (currentUser.Email != email)
            {
                currentUser.Email = email;
                currentUser.ActivationStatus = EmployeeActivationStatus.NotActivated;
            }

            await userManager.UpdateUserInfoAsync(currentUser);

            settings.Completed = true;
            await settingsManager.SaveAsync(settings);

            TrySetLanguage(tenant, lng);

            tenant.TimeZone = TimeZoneConverter.GetIanaTimeZoneId(timeZone);

            await tenantManager.SaveTenantAsync(tenant);
            await cspSettingsHelper.SaveAsync(null);

            await studioNotifyService.SendCongratulationsAsync(currentUser);
            await studioNotifyService.SendRegDataAsync(currentUser);

            if (subscribeFromSite && tenantExtra.Opensource && !coreBaseSettings.CustomMode)
            {
                await SubscribeFromSite(currentUser);
            }

            await cookiesManager.AuthenticateMeAndSetCookiesAsync(currentUser.Id, checkSuspiciousLogin: false);

            return settings;
        }
        catch (BillingNotFoundException)
        {
            throw new Exception(UserControlsCommonResource.LicenseKeyNotFound);
        }
        catch (BillingNotConfiguredException ex)
        {
            logger.ErrorWithException(ex);
            throw new Exception(UserControlsCommonResource.LicenseKeyNotCorrect);
        }
        catch (BillingLicenseTypeException)
        {
            var logoText = await tenantLogoManager.GetLogoTextAsync();
            throw new Exception(string.Format(UserControlsCommonResource.LicenseTypeNotCorrect, logoText));
        }
        catch (BillingException)
        {
            throw new Exception(UserControlsCommonResource.LicenseException);
        }
        catch (Exception ex)
        {
            logger.ErrorFirstTimeTenantSettings(ex);
            throw;
        }
    }

    public async Task<bool> GetRequestLicense()
    {
        if (!await tenantExtra.GetEnableTariffSettings() ||
            !tenantExtra.Enterprise ||
            File.Exists(licenseReader.LicensePath))
        {
            return false;
        }

        // The editing service's own trial stands in for the license file until the trial ends, and only while the
        // installation's trial would still be in force once it is applied: a trial that already ended needs the file
        // the wizard asked for before. The question is asked without authentication, so a successful answer of the
        // editing service is reused for a while.
        var docsTrial = await documentServiceLicense.GetTrialAsync(useCache: true);
        return docsTrial.State != DocsTrialState.Active || !await licenseReader.WillDocsTrialBeInForceAsync(docsTrial.Trial);
    }

    /// <summary>
    /// Puts the editing service's own trial in force when no license file has been given, so the wizard can be
    /// completed without one.
    /// </summary>
    /// <returns>
    /// <c>true</c> when a trial is in force; <c>false</c> when a license file is on disk, the editing service runs
    /// no active trial, a license from a file is already in force, or the installation's trial has already ended —
    /// then the license file is required as before.
    /// </returns>
    /// <remarks>
    /// No upload is recorded in the audit trail: no file was given, and a file uploaded later is activated the usual
    /// way, replacing the trial.
    /// </remarks>
    private async Task<bool> ApplyDocsTrialAsync()
    {
        if (licenseReader.HasLicenseFile())
        {
            return false;
        }

        var docsTrial = await documentServiceLicense.GetTrialAsync(useCache: false);

        // The wizard skips the license upload only after it was told the editing service runs its trial, so an
        // editing service that cannot be asked now is not read as "no trial", which would ask for a file the wizard
        // never offered. The installation keeps its own trial (created on the first read of the tariff) with its
        // current end; the background refresh brings that end closer once the editing service answers. A file is
        // still required when no trial of the installation is in force (a license from a file, no license quota at
        // all, or a trial that has ended), or when the last answer the installation knows said the editing service
        // runs no trial or its trial has ended: then the wizard asked for a file, and the silence changes nothing.
        if (docsTrial.State == DocsTrialState.Unknown)
        {
            await tenantExtra.GetCurrentTariffAsync();

            if (!await licenseReader.IsOwnTrialInForceAsync())
            {
                return false;
            }

            var lastKnown = await documentServiceLicense.GetLastKnownTrialAsync();
            if (lastKnown is { State: DocsTrialState.NotTrial or DocsTrialState.Expired })
            {
                return false;
            }

            logger.WarningWizardOnOwnTrial();
            return true;
        }

        // A trial that would already be over is not taken, and nothing is written for it: the call then fails as it did
        // before the editing service's trial was followed. The write checks again under the lock of the license writers
        if (docsTrial.State != DocsTrialState.Active || !await licenseReader.WillDocsTrialBeInForceAsync(docsTrial.Trial))
        {
            return false;
        }

        return await licenseReader.DocsTrialToDBAsync(docsTrial.Trial);
    }

    private void TrySetLanguage(Tenant tenant, string lng)
    {
        if (string.IsNullOrEmpty(lng))
        {
            return;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(lng);
            tenant.Language = culture.Name;
        }
        catch (Exception err)
        {
            logger.ErrorTrySetLanguage(err);
        }
    }

    private async Task<bool> IncorrectAmiId(string customAmiId)
    {
        customAmiId = (customAmiId ?? "").Trim();
        if (string.IsNullOrEmpty(customAmiId))
        {
            return true;
        }

        try
        {
            #pragma warning disable CA2000 // Dispose objects before losing scope
            var httpClient = clientFactory.CreateClient();
            #pragma warning restore CA2000
            
            var amiToken = await GetResponseString(httpClient, HttpMethod.Put, setupInfo.AmiTokenUrl, new Dictionary<string, string> { { "X-aws-ec2-metadata-token-ttl-seconds", "21600" } });
            var amiId = await GetResponseString(httpClient, HttpMethod.Get, setupInfo.AmiMetaUrl, new Dictionary<string, string> { { "X-aws-ec2-metadata-token", amiToken } });

            return string.IsNullOrEmpty(amiId) || amiId != customAmiId;
        }
        catch (Exception e)
        {
            logger.ErrorRequestAMI(e);
            return true;
        }
    }

    private async Task<string> GetResponseString(HttpClient httpClient, HttpMethod method, string requestUrl, Dictionary<string, string> headers)
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            return null;
        }

        using var request = new HttpRequestMessage(method, requestUrl);

        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        
        string responseString = null;
        
        try
        {
            using (var response = await httpClient.SendAsync(request))
            {
                responseString = await response.Content.ReadAsStringAsync();
            }

            logger.DebugRequestAMI(requestUrl, responseString);
        }
        catch (Exception e)
        {
            logger.ErrorRequestAMI(e);
        }

        return responseString;
    }

    private async Task SubscribeFromSite(UserInfo user)
    {
        try
        {
            var url = externalResourceSettingsHelper.Site.GetDefaultRegionalFullEntry("subscribe");

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            var values = new NameValueCollection
                    {
                        { "type", "sendsubscription" },
                        { "subscr_type", "Opensource" },
                        { "email", user.Email }
                    };
            var data = JsonSerializer.Serialize(values);
            request.Content = new StringContent(data);
#pragma warning disable CA2000
            var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
            using var response = await httpClient.SendAsync(request);

            logger.DebugSubscribeResponse(response);//toto write

        }
        catch (Exception e)
        {
            logger.ErrorSubscribeRequest(e);
        }
    }
}