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

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class BitlyLoginProvider : Consumer, IValidateKeysProvider
{
    private string BitlyToken => this["bitlyToken"];

    private readonly string _bitlyUrl = "https://api-ssl.bitly.com/v4/shorten";
    private readonly RequestHelper _requestHelper;

    public BitlyLoginProvider() { }

    public BitlyLoginProvider(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        RequestHelper requestHelper,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _requestHelper = requestHelper;
    }

    public Task<bool> ValidateKeysAsync()
    {
        try
        {
            return Task.FromResult(!string.IsNullOrEmpty(GetShortenLink("https://www.onlyoffice.com")));
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public bool Enabled => !string.IsNullOrEmpty(BitlyToken);

    public string GetShortenLink(string shareLink)
    {
        var data = $"{{\"long_url\":\"{shareLink}\"}}";
        var headers = new Dictionary<string, string>
            {
                {"Authorization" ,"Bearer " + BitlyToken}
            };

        var response = _requestHelper.PerformRequest(_bitlyUrl, "application/json", "POST", data, headers);

        var parser = JObject.Parse(response);
        var link = parser.Value<string>("link");

        return link;
    }
}