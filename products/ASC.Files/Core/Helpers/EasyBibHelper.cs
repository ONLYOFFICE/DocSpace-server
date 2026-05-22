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

namespace ASC.Web.Files.Helpers;

[Scope]
public class EasyBibHelper : Consumer
{
    public ILogger Logger { get; set; }

    private static readonly string _searchBookUrl = "https://worldcat.citation-api.com/query?search=",
                    _searchJournalUrl = "https://crossref.citation-api.com/query?search=",
                    _searchWebSiteUrl = "https://web.citation-api.com/query?search=",
                    _easyBibStyles = "https://api.citation-api.com/2.1/rest/styles";

    public enum EasyBibSource
    {
        book = 0,
        journal = 1,
        website = 2
    }

    public string AppKey => this["easyBibappkey"];
    private readonly RequestHelper _requestHelper;

    public EasyBibHelper() { }

    public EasyBibHelper(
        ILogger<EasyBibHelper> logger,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory factory,
        RequestHelper requestHelper,
        string name,
        int order,
        bool paid,
        Dictionary<string, string> props,
        Dictionary<string, string> additional = null)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, factory, name, order, paid, props, additional)
    {
        Logger = logger;
        _requestHelper = requestHelper;
    }

    public string GetEasyBibCitationsList(int source, string data)
    {
        var uri = source switch
        {
            0 => _searchBookUrl,
            1 => _searchJournalUrl,
            2 => _searchWebSiteUrl,
            _ => ""
        };
        uri += data;

        const string method = "GET";
        var headers = new Dictionary<string, string>();
        try
        {
            return _requestHelper.PerformRequest(uri, "", method, "", headers);
        }
        catch (Exception)
        {
            return "error";
        }

    }

    public string GetEasyBibStyles()
    {

        const string method = "GET";
        var headers = new Dictionary<string, string>();
        try
        {
            return _requestHelper.PerformRequest(_easyBibStyles, "", method, "", headers);
        }
        catch (Exception)
        {
            return "error";
        }
    }

    public object GetEasyBibCitation(string data)
    {
        try
        {
            var easyBibappkey = ConsumerFactory.Get<EasyBibHelper>().AppKey;

            var jsonBlogInfo = JObject.Parse(data);
            jsonBlogInfo.Add("key", easyBibappkey);
            var citationData = jsonBlogInfo.ToString();

            const string uri = "https://api.citation-api.com/2.0/rest/cite";
            const string contentType = "application/json";
            const string method = "POST";
            var headers = new Dictionary<string, string>();

            return _requestHelper.PerformRequest(uri, contentType, method, citationData, headers);

        }
        catch (Exception)
        {
            return null;
        }

    }
}