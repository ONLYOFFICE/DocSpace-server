﻿// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Api.Core;

[Scope]
public class CspSettingsHelper
{
    private readonly SettingsManager _settingsManager;
    private readonly FilesLinkUtility _filesLinkUtility;
    private readonly TenantManager _tenantManager;
    private readonly CoreSettings _coreSettings;
    private readonly GlobalStore _globalStore;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly IDistributedCache _distributedCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CspSettingsHelper(
        SettingsManager settingsManager,
        FilesLinkUtility filesLinkUtility,
        TenantManager tenantManager,
        CoreSettings coreSettings,
        GlobalStore globalStore,
        CoreBaseSettings coreBaseSettings,
        IDistributedCache distributedCache,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _settingsManager = settingsManager;
        _filesLinkUtility = filesLinkUtility;
        _tenantManager = tenantManager;
        _coreSettings = coreSettings;
        _globalStore = globalStore;
        _coreBaseSettings = coreBaseSettings;
        _distributedCache = distributedCache;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<string> SaveAsync(IEnumerable<string> domains, bool setDefaultIfEmpty)
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();
        var domain = tenant.GetTenantDomain(_coreSettings);
        List<string> headerKeys = new()
        {
            GetKey(domain)
        };

        if (domain == Tenant.LocalHost && tenant.Alias == Tenant.LocalHost)
        {
            var domainsKey = $"{GetKey(domain)}:keys";
            if (_httpContextAccessor.HttpContext != null)
            {
                var keys = new List<string>
                {
                    GetKey(Tenant.HostName)
                };

                var ips = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork);

                keys.AddRange(ips.Select(ip => GetKey(ip.ToString())));

                if (_httpContextAccessor.HttpContext.Connection.RemoteIpAddress != null)
                {
                    keys.Add(GetKey(_httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString()));
                }

                await _distributedCache.SetStringAsync(domainsKey, string.Join(';', keys));
                headerKeys.AddRange(keys);
            }
            else
            {
                var domainsValue = await _distributedCache.GetStringAsync(domainsKey);

                if (!string.IsNullOrEmpty(domainsValue))
                {
                    headerKeys.AddRange(domainsValue.Split(';'));
                }
            }
        }

        var headerValue = await CreateHeaderAsync(domains, setDefaultIfEmpty);

        if (!string.IsNullOrEmpty(headerValue))
        {
            await Parallel.ForEachAsync(headerKeys, async (headerKey, cs) => await _distributedCache.SetStringAsync(headerKey, headerValue, cs));
        }
        else
        {
            await Parallel.ForEachAsync(headerKeys, async (headerKey, cs) => await _distributedCache.RemoveAsync(headerKey, cs));
        }

        await _settingsManager.ManageAsync<CspSettings>(current =>
        {
            current.Domains = domains;
            current.SetDefaultIfEmpty = setDefaultIfEmpty;
        });

        return headerValue;
    }

    public async Task<CspSettings> LoadAsync()
    {
        return await _settingsManager.LoadAsync<CspSettings>();
    }

    public async Task RenameDomain(string oldDomain, string newDomain)
    {
        var oldKey = GetKey(oldDomain);
        var val = await _distributedCache.GetStringAsync(oldKey);
        if (!string.IsNullOrEmpty(val))
        {
            await _distributedCache.RemoveAsync(oldKey);
            await _distributedCache.SetStringAsync(GetKey(newDomain), val);
        }
    }

    public async Task<string> CreateHeaderAsync(IEnumerable<string> domains, bool setDefaultIfEmpty = false, bool currentTenant = true)
    {
        if (domains == null || !domains.Any())
        {
            if (setDefaultIfEmpty)
            {
                domains = Enumerable.Empty<string>();
            }
            else
            {
                return null;
            }
        }

        var options = domains.Select(r => new CspOptions(r)).ToList();

        var defaultOptions = _configuration.GetSection("csp:default").Get<CspOptions>();
        if (!_coreBaseSettings.Standalone && !string.IsNullOrEmpty(_coreBaseSettings.Basedomain))
        {
            defaultOptions.Def.Add($"*.{_coreBaseSettings.Basedomain}");
        }

        if (await _globalStore.GetStoreAsync(currentTenant) is S3Storage s3Storage)
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

        if (Uri.IsWellFormedUriString(_filesLinkUtility.DocServiceUrl, UriKind.Absolute))
        {
            options.Add(new CspOptions
            {
                Script = new List<string> { _filesLinkUtility.DocServiceUrl },
                Frame = new List<string> { _filesLinkUtility.DocServiceUrl },
                Connect = new List<string> { _filesLinkUtility.DocServiceUrl }
            });
        }

        var firebaseDomain = _configuration["firebase:authDomain"];
        if (!string.IsNullOrEmpty(firebaseDomain))
        {
            var firebaseOptions = _configuration.GetSection("csp:firebase").Get<CspOptions>();
            if (firebaseOptions != null)
            {
                options.Add(firebaseOptions);
            }
        }

        if (!string.IsNullOrEmpty(_configuration["web:zendesk-key"]))
        {
            var zenDeskOptions = _configuration.GetSection("csp:zendesk").Get<CspOptions>();
            if (zenDeskOptions != null)
            {
                options.Add(zenDeskOptions);
            }
        }

        if (!string.IsNullOrEmpty(_configuration["files:oform:domain"]))
        {
            var oformOptions = _configuration.GetSection("csp:oform").Get<CspOptions>();
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

    private string GetKey(string domain)
    {
        return $"csp:{domain}";
    }
}

public class CspOptions
{
    public List<string> Def { get; set; } = new();
    public List<string> Script { get; set; } = new();
    public List<string> Style { get; set; } = new();
    public List<string> Img { get; set; } = new();
    public List<string> Frame { get; set; } = new();
    public List<string> Fonts { get; set; } = new();
    public List<string> Connect { get; set; } = new();
    public List<string> Media { get; set; } = new();

    public CspOptions()
    {

    }

    public CspOptions(string domain)
    {
        Def = new List<string>();
        Script = new List<string> { domain };
        Style = new List<string> { domain };
        Img = new List<string> { domain };
        Frame = new List<string> { domain };
        Fonts = new List<string> { domain };
        Connect = new List<string> { domain };
        Media = new List<string> { domain };
    }
}
