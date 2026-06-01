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

using Microsoft.Extensions.Caching.Distributed;

namespace ASC.Web.Api.Core;

[Scope]
public class CspSettingsHelper(
    SettingsManager settingsManager,
    FilesLinkUtility filesLinkUtility,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    GlobalStore globalStore,
    CoreBaseSettings coreBaseSettings,
    IFusionCache hybridCache,
    IDistributedCache distributedCache,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
{
    public async Task<string> SaveAsync(IEnumerable<string> domains, bool updateInDb = true)
    {
        var tenant = tenantManager.GetCurrentTenant();
        var domain = tenant.GetTenantDomain(coreSettings);
        HashSet<string> headerKeys = [GetKey(domain)];

        var baseDomain = await coreSettings.GetSettingAsync("BaseDomain");
        if (coreBaseSettings.Standalone && !string.IsNullOrEmpty(baseDomain))
        {
            var tenantWithoutAlias = await tenantManager.GetTenantAsync(baseDomain);
            if (tenant.Id == tenantWithoutAlias.Id)
            {
                _ = headerKeys.Add(GetKey(baseDomain));
            }
        }

        if (domain == Tenant.LocalHost && tenant.Alias == Tenant.LocalHost)
        {
            var domainsKey = $"{GetKey(domain)}:keys";
            if (httpContextAccessor.HttpContext != null)
            {
                var keys = new HashSet<string> { GetKey(Tenant.HostName) };

                var ips = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork);

                keys.UnionWith(ips.Select(ip => GetKey(ip.ToString())));

                if (httpContextAccessor.HttpContext.Connection.RemoteIpAddress != null)
                {
                    keys.Add(GetKey(httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString()));
                }

                var host = httpContextAccessor.HttpContext.Request.Host.Value;
                if (!string.IsNullOrEmpty(host))
                {
                    keys.Add(GetKey(host));
                }

                await hybridCache.SetAsync(domainsKey, string.Join(';', keys));
                headerKeys.UnionWith(keys);
            }
            else
            {
                string domainsValue;

                var oldScheme = false;
                try
                {
                    domainsValue = await hybridCache.GetOrDefaultAsync<string>(domainsKey);
                }
                catch (FusionCacheSerializationException)
                {
                    domainsValue = await distributedCache.GetStringAsync(domainsKey);
                    oldScheme = true;
                }

                if (oldScheme)
                {
                    await hybridCache.SetAsync(domainsKey, domainsValue);
                }

                if (!string.IsNullOrEmpty(domainsValue))
                {
                    headerKeys.UnionWith(domainsValue.Split(';'));
                }
            }
        }

        var headerValue = await CreateHeaderAsync(domains);
        var defaultOptions = configuration.GetSection("csp:default").Get<CspOptions>();
        if (!string.IsNullOrEmpty(headerValue) && Encoding.UTF8.GetByteCount(headerValue) > defaultOptions.MaxSize)
        {
            throw new InvalidOperationException($"CSP header size exceeds maximum allowed size of {defaultOptions.MaxSize} bytes.");
        }

        if (!string.IsNullOrEmpty(headerValue))
        {
            await Parallel.ForEachAsync(headerKeys, async (headerKey, cs) => await hybridCache.SetAsync(headerKey, headerValue, token: cs));
        }
        else
        {
            await Parallel.ForEachAsync(headerKeys, async (headerKey, cs) => await hybridCache.RemoveAsync(headerKey, token: cs));
        }

        if (updateInDb)
        {
            await settingsManager.ManageAsync<CspSettings>(current =>
            {
                current.Domains = domains;
            });
        }

        return headerValue;
    }

    public async Task<CspSettings> LoadAsync(DateTime? lastModified = null)
    {
        return await settingsManager.LoadAsync<CspSettings>(lastModified);
    }

    public async Task RenameDomainAsync(string oldDomain, string newDomain)
    {
        var oldKey = GetKey(oldDomain);

        string val;

        try
        {
            val = await hybridCache.GetOrDefaultAsync<string>(oldKey);
        }
        catch (FusionCacheSerializationException)
        {
            val = await distributedCache.GetStringAsync(oldKey);
        }

        if (!string.IsNullOrEmpty(val))
        {
            await hybridCache.RemoveAsync(oldKey);
            await hybridCache.SetAsync(GetKey(newDomain), val);
        }
    }

    public async Task UpdateBaseDomainAsync()
    {
        if (!coreBaseSettings.Standalone)
        {
            return;
        }

        var baseDomain = await coreSettings.GetSettingAsync("BaseDomain");
        if (string.IsNullOrEmpty(baseDomain))
        {
            return;
        }

        var tenantWithoutAlias = await tenantManager.GetTenantAsync(baseDomain);

        var domain = tenantWithoutAlias.GetTenantDomain(coreSettings);

        string val;

        try
        {
            val = await hybridCache.GetOrDefaultAsync<string>(GetKey(domain));
        }
        catch (FusionCacheSerializationException)
        {
            val = await distributedCache.GetStringAsync(GetKey(domain));
        }

        await hybridCache.SetAsync(GetKey(baseDomain), val);
    }

    public async Task<string> CreateHeaderAsync(IEnumerable<string> domains, bool currentTenant = true)
    {
        domains ??= [];

        var options = domains.Select(r => new CspOptions(r)).ToList();

        var defaultOptions = configuration.GetSection("csp:default").Get<CspOptions>();
        if (!coreBaseSettings.Standalone && !string.IsNullOrEmpty(coreBaseSettings.Basedomain))
        {
            defaultOptions.Def.Add($"*.{coreBaseSettings.Basedomain}");
        }

        if (await globalStore.GetStoreAsync(currentTenant) is S3Storage s3Storage)
        {
            var internalUrl = s3Storage.GetUriInternal(null).ToString();

            if (!string.IsNullOrEmpty(internalUrl))
            {
                defaultOptions.Img.Add(internalUrl);
                defaultOptions.Media.Add(internalUrl);
                defaultOptions.Connect.Add(internalUrl);
            }

            if (!string.IsNullOrEmpty(s3Storage.CdnDistributionDomain))
            {
                defaultOptions.Img.Add(s3Storage.CdnDistributionDomain);
                defaultOptions.Media.Add(s3Storage.CdnDistributionDomain);
                defaultOptions.Connect.Add(s3Storage.CdnDistributionDomain);
            }
        }

        options.Add(defaultOptions);

        var docServiceUrl = filesLinkUtility.GetDocServiceUrl();

        if (Uri.IsWellFormedUriString(docServiceUrl, UriKind.Absolute))
        {
            options.Add(new CspOptions { Script = [docServiceUrl], Frame = [docServiceUrl], Connect = [docServiceUrl] });
        }

        var firebaseDomain = configuration["firebase:authDomain"];
        if (!string.IsNullOrEmpty(firebaseDomain))
        {
            var firebaseOptions = configuration.GetSection("csp:firebase").Get<CspOptions>();
            if (firebaseOptions != null)
            {
                options.Add(firebaseOptions);
            }
        }

        if (!string.IsNullOrEmpty(configuration["web:zendesk-key"]))
        {
            var zenDeskOptions = configuration.GetSection("csp:zendesk").Get<CspOptions>();
            if (zenDeskOptions != null)
            {
                options.Add(zenDeskOptions);
            }
        }

        if (!string.IsNullOrEmpty(configuration["files:oform:domain"]))
        {
            var oformOptions = configuration.GetSection("csp:oform").Get<CspOptions>();
            if (oformOptions != null)
            {
                options.Add(oformOptions);
            }
        }

        var webSearch = configuration.GetSection("csp:websearch").Get<CspOptions>();
        if (webSearch != null)
        {
            options.Add(webSearch);
        }

        if (!string.IsNullOrEmpty(configuration["web:tagmanager-id"]))
        {
            var analytics = configuration.GetSection("csp:analytics").Get<CspOptions>();
            if (analytics != null)
            {
                options.Add(analytics);
            }
        }

        if (!string.IsNullOrEmpty(configuration["web:recaptcha:public-key"]) || !string.IsNullOrEmpty(configuration["web:hcaptcha:public-key"]))
        {
            var oformOptions = configuration.GetSection("csp:captcha").Get<CspOptions>();
            if (oformOptions != null)
            {
                options.Add(oformOptions);
            }
        }

        var csp = new CspBuilder();

        foreach (var domain in options.SelectMany(r => r.Def).Distinct())
        {
            csp.ByDefaultAllow.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Script).Distinct())
        {
            csp.AllowScripts.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Style).Distinct())
        {
            csp.AllowStyles.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Img).Distinct())
        {
            csp.AllowImages.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Frame).Distinct())
        {
            csp.AllowFrames.From(domain);
            csp.AllowFraming.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Fonts).Distinct())
        {
            csp.AllowFonts.From(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Connect).Distinct())
        {
            csp.AllowConnections.To(domain);
        }

        foreach (var domain in options.SelectMany(r => r.Media).Distinct())
        {
            csp.AllowAudioAndVideo.From(domain);
        }

        var (_, headerValue) = csp.BuildCspOptions().ToString(null);
        return headerValue;
    }

    public async Task RemoveFromCacheAsync(string domain)
    {
        var headerKey = GetKey(domain);
        await hybridCache.RemoveAsync(headerKey);
    }

    public async Task<bool> ExistsInCacheAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();
        var domain = tenant.GetTenantDomain(coreSettings);
        var key = GetKey(domain);

        try
        {
            var val = await hybridCache.GetOrDefaultAsync<string>(key);
            return !string.IsNullOrEmpty(val);
        }
        catch (FusionCacheSerializationException)
        {
            var val = await distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(val);
        }
    }

    private static string GetKey(string domain)
    {
        return $"csp:{domain}";
    }
}

public class CspOptions
{
    public List<string> Def { get; set; } = [];
    public List<string> Script { get; set; } = [];
    public List<string> Style { get; set; } = [];
    public List<string> Img { get; set; } = [];
    public List<string> Frame { get; set; } = [];
    public List<string> Fonts { get; set; } = [];
    public List<string> Connect { get; set; } = [];
    public List<string> Media { get; set; } = [];

    public int MaxSize { get; set; } = 15 * 1024;

    public CspOptions()
    {
    }

    public CspOptions(string domain)
    {
        Def = [];
        Script = [domain];
        Style = [domain];
        Img = [domain];
        Frame = [domain];
        Fonts = [domain];
        Connect = [domain];
        Media = [domain];
    }
}
