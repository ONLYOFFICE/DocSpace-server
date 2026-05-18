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

namespace ASC.AI.Core.Tools.Retrieval;

[Scope]
public class WebSearchTool(WebSearchEngineFactory searchEngineFactory, IFaviconService faviconService)
{
    public const string Name = "docspace_web_search";
    private const string Description = "Search the web - performs real-time web searches and can scrape content from specific URLs.";
    private static AIFunctionFactoryOptions FactoryOptions => new()
    {
        Name = Name,
        Description = Description
    };

    public AIFunction Init(EngineConfig config, Dictionary<string, string>? metadata)
    {
        var engine = searchEngineFactory.Create(config, metadata);

        return AIFunctionFactory.Create(Function, FactoryOptions);

        async Task<ToolResponse<List<WebSearchResult>>> Function([Description("Search query")] string query)
        {
            try
            {
                query = query.Trim();
                ArgumentException.ThrowIfNullOrEmpty(query);

                var results = await engine.SearchAsync(new SearchQuery
                {
                    Query = query,
                    MaxResults = 5
                });

                var response = results.Select(x =>
                {
                    var faviconUrl = x.FaviconUrl;

                    if (!string.IsNullOrEmpty(x.Url))
                    {
                        var domain = new Uri(x.Url).Host;
                        faviconUrl = faviconService.GetFaviconUrl(domain);
                    }

                    return new WebSearchResult
                    {
                        Title = x.Title,
                        Url = x.Url,
                        FaviconUrl = faviconUrl,
                        Text = x.Text
                    };
                }).ToList();

                return new ToolResponse<List<WebSearchResult>> { Data = response };
            }
            catch (Exception e)
            {
                return new ToolResponse<List<WebSearchResult>> { Error = e.Message };
            }
        }
    }
}
