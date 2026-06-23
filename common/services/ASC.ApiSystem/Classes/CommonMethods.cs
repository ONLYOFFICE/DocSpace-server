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

namespace ASC.ApiSystem.Classes;

[Scope]
public class CommonMethods(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<CommonMethods> log,
    CoreSettings coreSettings,
    CommonLinkUtility commonLinkUtility,
    EmailValidationKeyProvider emailValidationKeyProvider,
    CommonConstants commonConstants,
    IMemoryCache memoryCache,
    HostedSolution hostedSolution,
    CoreBaseSettings coreBaseSettings,
    TenantManager tenantManager,
    IHttpClientFactory clientFactory)
{
    public async Task<TenantResponseDto> ToTenantResponseDto(Tenant t, QuotaUsageDto quotaUsage = null, TenantOwnerDto owner = null, WizardSettings wizardSettings = null)
    {
        var tenantQuotaSettings = await hostedSolution.GetTenantQuotaSettings(t.Id);
        var tariffMaxTotalSize = (await hostedSolution.GetTenantQuotaAsync(t.Id)).MaxTotalSize;
        var timeZone = TimeZoneConverter.GetTimeZone(t.TimeZone);

        return new TenantResponseDto
        {
            Created = t.CreationDateTime,
            Domain = t.GetTenantDomain(coreSettings, false),
            MappedDomain = t.MappedDomain,
            HostedRegion = t.HostedRegion,
            Industry = t.Industry,
            Language = t.Language,
            Name = t.Name == "" ? Resource.PortalName : t.Name,
            OwnerId = t.OwnerId,
            PaymentId = t.PaymentId,
            PartnerId = t.PartnerId,
            PortalName = t.Alias,
            Status = t.Status.ToStringFast(),
            TenantId = t.Id,
            TimeZoneId = TimeZoneConverter.GetIanaTimeZoneId(timeZone),
            TimeZoneName = timeZone.DisplayName,
            QuotaUsage = quotaUsage,
            CustomQuota = tenantQuotaSettings.EnableQuota && tenantQuotaSettings.Quota <= tariffMaxTotalSize
                ? tenantQuotaSettings.Quota
                : tariffMaxTotalSize == long.MaxValue ? -1 : tariffMaxTotalSize,
            Owner = owner,
            WizardSettings = wizardSettings
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
        var validationKey = emailValidationKeyProvider.GetEmailKey(tenant.OwnerId.ToString() + confirmType, tenant.Id);
        var domain = tenant.GetTenantDomain(coreSettings);
        var port = httpContextAccessor.HttpContext?.Request.Host.Port ?? 80;

        var url = string.Format("{0}{1}{2}{3}{4}?userid={5}&key={6}",
                            requestUriScheme,
                            Uri.SchemeDelimiter,
                            domain == "localhost" && port != 80 ? $"{domain}:{port}" : domain,
                            commonConstants.WebApiBaseUrl,
                            apiMethod,
                            tenant.OwnerId.ToString(),
                            validationKey);

        if (skipAndReturnUrl)
        {
            log.DebugSendMethodSkipped(apiMethod);
            return url;
        }

        using var request = new HttpRequestMessage(httpMethod, url);

        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        try
        {
#pragma warning disable CA2000
            var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            log.DebugSendMethodResult(apiMethod, response.StatusCode);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var result = await response.Content.ReadAsStringAsync();
                throw new Exception(result);
            }
        }
        catch (Exception ex)
        {
            log.ErrorSendMethod(apiMethod, ex);
            return url;
        }

        return null;
    }

    public async Task<(bool, Tenant)> TryGetTenantAsync(IModel model)
    {
        Tenant tenant;
        if (coreBaseSettings.Standalone && model != null && !string.IsNullOrWhiteSpace(model.PortalName ?? ""))
        {
            tenant = await tenantManager.GetTenantAsync((model.PortalName ?? "").Trim());
            return (true, tenant);
        }

        if (model is { TenantId: not null })
        {
            tenant = await hostedSolution.GetTenantAsync(model.TenantId.Value);
            return (true, tenant);
        }

        if (model != null && !string.IsNullOrWhiteSpace(model.PortalName ?? ""))
        {
            tenant = await hostedSolution.GetTenantAsync((model.PortalName ?? "").Trim());
            return (true, tenant);
        }

        return (false, null);
    }

    public async Task<List<Tenant>> GetTenantsAsync(TenantModel model)
    {
        var tenants = new List<Tenant>();
        var empty = true;

        if (!string.IsNullOrWhiteSpace(model.Email ?? ""))
        {
            empty = false;
            tenants.AddRange(await hostedSolution.FindTenantsAsync((model.Email ?? "").Trim()));
        }

        if (!string.IsNullOrWhiteSpace(model.PortalName ?? ""))
        {
            empty = false;
            var tenant = await hostedSolution.GetTenantAsync((model.PortalName ?? "").Trim());

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

        if (tenants.Count == 0)
        {
            throw new Exception("Invalid login or password.");
        }

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

    public bool CheckMuchRegistration(TenantModel model, string clientIP)
    {
        if (IsTestEmail(model.Email))
        {
            return false;
        }

        log.DebugClientIp(clientIP);

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
                AbsoluteExpiration = DateTimeOffset.MaxValue,
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

        log.DebugTooMuchRequests(model.PortalName, clientIP);

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
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            var header = request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                return header.Split(',').First();
            }
        }

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

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            request.Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

#pragma warning disable CA2000
            var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
            using var httpClientResponse = await httpClient.SendAsync(request);
            var resp = await httpClientResponse.Content.ReadAsStringAsync();
            var recaptchData = JsonSerializer.Deserialize<Web.Core.RecaptchData>(resp, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (recaptchData.Success.GetValueOrDefault())
            {
                return true;
            }

            log.DebugRecaptchaResponseError(resp);

            if (recaptchData.ErrorCodes is { Count: > 0 })
            {
                log.DebugRecaptchaApiErrors(resp);
            }
        }
        catch (Exception ex)
        {
            log.ErrorValidateRecaptcha(ex);
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
