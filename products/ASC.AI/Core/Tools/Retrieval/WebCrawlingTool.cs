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
public class WebCrawlingTool(WebSearchEngineFactory searchEngineFactory, IFaviconService faviconService)
{
    public const string Name = "docspace_web_crawling";
    private const string Description = "Extract and crawl content from specific URLs - retrieves full text content, metadata, and structured information from web pages. Ideal for extracting detailed content from known URLs.";

    private static AIFunctionFactoryOptions FactoryOptions => new()
    {
        Name = Name,
        Description = Description
    };

    public AIFunction Init(EngineConfig config, Dictionary<string, string>? metadata)
    {
        var engine = searchEngineFactory.Create(config, metadata);

        return AIFunctionFactory.Create(Function, FactoryOptions);

        async Task<ToolResponse<WebSearchResult>> Function(
            [Description("URL to crawl and extract content from")] string url,
            [Description("Maximum characters to extract (default: 10000)")] int? maxCharacters)
        {
            try
            {
                url = url.Trim();
                ArgumentException.ThrowIfNullOrEmpty(url);

                var result = await engine.GetPageContentAsync(new PageContentQuery
                {
                    Url = url,
                    MaxCharacters = maxCharacters ?? 10000
                });

                if (result != null && !string.IsNullOrEmpty(url))
                {
                    var domain = new Uri(url).Host;
                    result.FaviconUrl = faviconService.GetFaviconUrl(domain);
                }

                return new ToolResponse<WebSearchResult> { Data = result };
            }
            catch (Exception e)
            {
                return new ToolResponse<WebSearchResult> { Error = e.Message };
            }
        }
    }
}
