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

using System.Text.Json;

namespace ASC.ApiSystem.Controllers;

[Scope]
public class CommonMethods(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<CommonMethods> log,
    CoreSettings coreSettings,
    CommonLinkUtility commonLinkUtility,
    EmailValidationKeyProvider emailValidationKeyProvider,
    TimeZoneConverter timeZoneConverter, CommonConstants commonConstants,
    IMemoryCache memoryCache,
    HostedSolution hostedSolution,
    CoreBaseSettings coreBaseSettings,
    TenantManager tenantManager,
    IHttpClientFactory clientFactory)
{
    public object ToTenantWrapper(Tenant t, QuotaUsageDto quotaUsage = null, TenantOwnerDto owner = null, WizardSettings wizardSettings = null)
    {
        var tenantQuotaSettings = hostedSolution.GetTenantQuotaSettings(t.Id).Result;
        var tariffMaxTotalSize = hostedSolution.GetTenantQuotaAsync(t.Id).Result.MaxTotalSize;
        return new
        {
            created = t.CreationDateTime,
            domain = t.GetTenantDomain(coreSettings, false),
            mappedDomain = t.MappedDomain,
            hostedRegion = t.HostedRegion,
            industry = t.Industry,
            language = t.Language,
            name = t.Name == "" ? Resource.PortalName : t.Name,
            ownerId = t.OwnerId,
            paymentId = t.PaymentId,
            portalName = t.Alias,
            status = t.Status.ToString(),
            tenantId = t.Id,
            timeZoneName = timeZoneConverter.GetTimeZone(t.TimeZone).DisplayName,
            quotaUsage,
            customQuota = tenantQuotaSettings.EnableQuota && tenantQuotaSettings.Quota <= tariffMaxTotalSize ? 
                    tenantQuotaSettings.Quota :
                    tariffMaxTotalSize == long.MaxValue ? -1 : tariffMaxTotalSize,
            owner,
            wizardSettings
        };
    }

    public string CreateReference(int tenantId, string requestUriScheme, string tenantDomain, string email, bool first = false)
    {
        var url = commonLinkUtility.GetConfirmationUrlRelative(tenantId, email, ConfirmType.Auth, first ? "true" : "");
        return $"{requestUriScheme}{Uri.SchemeDelimiter}{tenantDomain}/{url}{(first ? "&first=true" : "")}";
    }

    public async Task<string> SendCongratulations(string requestUriScheme, Tenant tenant, bool skipWelcome)
    {
        return await CallSendMethod(requestUriScheme, "portal/sendcongratulations", HttpMethod.Post, tenant, ConfirmType.Auth, skipWelcome);
    }

    public async Task<string> SendRemoveInstructions(string requestUriScheme, Tenant tenant)
    {
        return await CallSendMethod(requestUriScheme, "portal/sendremoveinstructions", HttpMethod.Post, tenant, ConfirmType.PortalRemove, false);
    }

    private async Task<string> CallSendMethod(string requestUriScheme, string apiMethod, HttpMethod httpMethod, Tenant tenant, ConfirmType confirmType, bool skipAndReturnUrl)
    {
        var validationKey = emailValidationKeyProvider.GetEmailKey(tenant.Id, tenant.OwnerId.ToString() + confirmType);

        var url = string.Format("{0}{1}{2}{3}{4}?userid={5}&key={6}",
                            requestUriScheme,
                            Uri.SchemeDelimiter,
                            tenant.GetTenantDomain(coreSettings),
                            commonConstants.WebApiBaseUrl,
                            apiMethod,
                            tenant.OwnerId,
                            validationKey);

        if (skipAndReturnUrl)
        {
            log.LogDebug($"SendMethod {apiMethod} skiped");
            return url;
        }

        var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(url)
        };

        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        try
        {
            var httpClient = clientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);

            log.LogDebug($"SendMethod {apiMethod} result = {response.StatusCode}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var result = await response.Content.ReadAsStringAsync();
                throw new Exception(result);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"SendMethod {apiMethod} error");
            return url;
        }

        return null;
    }

    public async Task<(bool, Tenant)> TryGetTenantAsync(IModel model)
    {
        Tenant tenant;
        if (coreBaseSettings.Standalone && model != null && !string.IsNullOrWhiteSpace((model.PortalName ?? "")))
        {
            tenant = await tenantManager.GetTenantAsync((model.PortalName ?? "").Trim());
            return (true, tenant);
        }

        if (model is { TenantId: not null })
        {
            tenant = await hostedSolution.GetTenantAsync(model.TenantId.Value);
            return (true, tenant);
        }

        if (model != null && !string.IsNullOrWhiteSpace((model.PortalName ?? "")))
        {
            tenant = (await hostedSolution.GetTenantAsync((model.PortalName ?? "").Trim()));
            return (true, tenant);
        }

        return (false, null);
    }

    public async Task<List<Tenant>> GetTenantsAsync(TenantModel model)
    {
        var tenants = new List<Tenant>();
        var empty = true;

        if (!string.IsNullOrWhiteSpace((model.Email ?? "")))
        {
            empty = false;
            tenants.AddRange(await hostedSolution.FindTenantsAsync((model.Email ?? "").Trim()));
        }

        if (!string.IsNullOrWhiteSpace((model.PortalName ?? "")))
        {
            empty = false;
            var tenant = (await hostedSolution.GetTenantAsync((model.PortalName ?? "").Trim()));

            if (tenant != null)
            {
                tenants.Add(tenant);
            }
        }

        if (model.TenantId.HasValue)
        {
            empty = false;
            var tenant = await hostedSolution.GetTenantAsync(model.TenantId.Value);

            if (tenant != null)
            {
                tenants.Add(tenant);
            }
        }

        if (empty)
        {
            tenants.AddRange((await hostedSolution.GetTenantsAsync(DateTime.MinValue)).OrderBy(t => t.Id).ToList());
        }

        return tenants;
    }

    public async Task<List<Tenant>> GetTenantsAsync(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new Exception("Invalid login or password.");
        }

        var tenants = await hostedSolution.FindTenantsAsync(email, passwordHash);

        return tenants;
    }

    public bool IsTestEmail(string email)
    {
        //the point is not needed in gmail.com
        email = Regex.Replace(email ?? "", "\\.*(?=\\S*(@gmail.com$))", "").ToLower();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(commonConstants.AutotestSecretEmails))
        {
            return false;
        }

        var regex = new Regex(commonConstants.AutotestSecretEmails, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        return regex.IsMatch(email);
    }

    public bool CheckMuchRegistration(TenantModel model, string clientIP, Stopwatch sw)
    {
        if (IsTestEmail(model.Email))
        {
            return false;
        }

        log.LogDebug("clientIP = {0}", clientIP);

        var cacheKey = "ip_" + clientIP;

        if (memoryCache.TryGetValue(cacheKey, out int ipAttemptsCount))
        {
            memoryCache.Remove(cacheKey);
        }

        ipAttemptsCount++;

        memoryCache.Set(
            // String that represents the name of the cache item,
            // could be any string
            cacheKey,
            // Something to store in the cache
            ipAttemptsCount,
            new MemoryCacheEntryOptions
            {
                // Will not use absolute cache expiration
                AbsoluteExpiration = DateTime.MaxValue,
                // Cache will expire after one hour
                // You can change this time interval according
                // to your requriements
                SlidingExpiration = commonConstants.MaxAttemptsTimeInterval,
                // Cache will not be removed before expired
                Priority = CacheItemPriority.NeverRemove
            });

        if (ipAttemptsCount <= commonConstants.MaxAttemptsCount)
        {
            return false;
        }

        log.LogDebug("PortalName = {PortalName}; Too much requests from ip: {Ip}", model.PortalName, clientIP);
        sw.Stop();

        return true;
    }

    public string GetRequestScheme()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return Uri.UriSchemeHttp;
        }
        var header = request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        return string.IsNullOrEmpty(header) ? request.Scheme : header;
    }

    public string GetClientIp()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        //TODO: check old version

        //if (request.Properties.ContainsKey("MS_HttpContext"))
        //{
        //    return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
        //}

        //if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
        //{
        //    var prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
        //    return prop.Address;
        //}

        //return null;
    }

    public async Task<IEnumerable<string>> GetHostIpsAsync()
    {
        var hostName = Dns.GetHostName();
        var hostEntry = await Dns.GetHostEntryAsync(hostName);
        return hostEntry.AddressList.Select(ip => ip.ToString());
    }

    public async Task<bool> ValidateRecaptcha(RecaptchaType recaptchaType, string response, string ip)
    {
        try
        {
            var privateKey = recaptchaType switch
            {
                RecaptchaType.AndroidV2 => configuration["recaptcha:private-key:android"],
                RecaptchaType.iOSV2 => configuration["recaptcha:private-key:ios"],
                RecaptchaType.hCaptcha => configuration["hcaptcha:private-key"],
                _ => configuration["recaptcha:private-key:default"]
            };

            var data = $"secret={privateKey}&remoteip={ip}&response={response}";

            var url = recaptchaType is RecaptchaType.hCaptcha
                ? configuration["hcaptcha:verify-url"] ?? "https://api.hcaptcha.com/siteverify"
                : configuration["recaptcha:verify-url"] ?? "https://www.recaptcha.net/recaptcha/api/siteverify";

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var httpClient = clientFactory.CreateClient();
            using var httpClientResponse = await httpClient.SendAsync(request);
            var resp = await httpClientResponse.Content.ReadAsStringAsync();
            var recaptchData = JsonSerializer.Deserialize<Web.Core.RecaptchData>(resp, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (recaptchData.Success.GetValueOrDefault())
            {
                return true;
            }
            


            log.LogDebug("Recaptcha error: {0}", resp);
            
            if (recaptchData.ErrorCodes is { Count: > 0 })
            {
                log.LogDebug("Recaptcha api returns errors: {0}", resp);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "ValidateRecaptcha");
        }
        return false;
    }
}

public class RecaptchData
{
    public bool? Success { get; set; }
    
    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; set; }
}

