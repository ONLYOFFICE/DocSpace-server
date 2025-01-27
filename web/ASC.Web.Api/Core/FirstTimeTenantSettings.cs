// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Studio.UserControls.FirstTime;

[Transient]
public class FirstTimeTenantSettings(
    ILogger<FirstTimeTenantSettings> logger,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    SettingsManager settingsManager,
    UserManager userManager,
    SetupInfo setupInfo,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    SecurityContext securityContext,
    MessageService messageService,
    LicenseReader licenseReader,
    StudioNotifyService studioNotifyService,
    TimeZoneConverter timeZoneConverter,
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

            var currentUser = await userManager.GetUsersAsync((tenantManager.GetCurrentTenant()).OwnerId);

            if (!UserManagerWrapper.ValidateEmail(email))
            {
                throw new Exception(Resource.EmailAndPasswordIncorrectEmail);
            }

            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new Exception(Resource.ErrorPasswordEmpty);
            }

            await securityContext.SetUserPasswordHashAsync(currentUser.Id, passwordHash);

            email = email.Trim();
            if (currentUser.Email != email)
            {
                currentUser.Email = email;
                currentUser.ActivationStatus = EmployeeActivationStatus.NotActivated;
            }

            await userManager.UpdateUserInfoAsync(currentUser);

            if ((await tenantExtra.GetEnableTariffSettings() || ami) && tenantExtra.Enterprise)
            {
                await TariffSettings.SetLicenseAcceptAsync(settingsManager);
                messageService.Send(MessageAction.LicenseKeyUploaded);

                await licenseReader.RefreshLicenseAsync(documentServiceLicense.ValidateLicense);
            }

            settings.Completed = true;
            await settingsManager.SaveAsync(settings);

            TrySetLanguage(tenant, lng);

            tenant.TimeZone = timeZoneConverter.GetTimeZone(timeZone).Id;

            await tenantManager.SaveTenantAsync(tenant);
            await cspSettingsHelper.SaveAsync(null);
            
            await studioNotifyService.SendCongratulationsAsync(currentUser);
            await studioNotifyService.SendRegDataAsync(currentUser);

            if (subscribeFromSite && tenantExtra.Opensource && !coreBaseSettings.CustomMode)
            {
                await SubscribeFromSite(currentUser);
            }

            await cookiesManager.AuthenticateMeAndSetCookiesAsync(currentUser.Id);

            return settings;
        }
        catch (BillingNotFoundException)
        {
            throw new Exception(UserControlsCommonResource.LicenseKeyNotFound);
        }
        catch (BillingNotConfiguredException)
        {
            throw new Exception(UserControlsCommonResource.LicenseKeyNotCorrect);
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
        return await tenantExtra.GetEnableTariffSettings() && 
               tenantExtra.Enterprise &&
               !File.Exists(licenseReader.LicensePath);
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
            var httpClient = clientFactory.CreateClient();

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
        string responseString = null;

        if (string.IsNullOrEmpty(requestUrl))
        {
            return responseString;
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(requestUrl),
            Method = method
        };

        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

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

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url)
            };
            var values = new NameValueCollection
                    {
                        { "type", "sendsubscription" },
                        { "subscr_type", "Opensource" },
                        { "email", user.Email }
                    };
            var data = JsonSerializer.Serialize(values);
            request.Content = new StringContent(data);

            var httpClient = clientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);

            logger.DebugSubscribeResponse(response);//toto write

        }
        catch (Exception e)
        {
            logger.ErrorSubscribeRequest(e);
        }
    }
}