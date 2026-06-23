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
public class WordpressToken(TokenHelper tokenHelper, OAuth20TokenHelper oAuth20TokenHelper)
{
    public const string AppAttr = "wordpress";

    public async Task<OAuth20Token> GetTokenAsync()
    {
        return await tokenHelper.GetTokenAsync(AppAttr);
    }

    public async Task SaveTokenAsync(OAuth20Token token)
    {
        ArgumentNullException.ThrowIfNull(token);

        await tokenHelper.SaveTokenAsync(new Token(token, AppAttr));
    }

    public async Task<OAuth20Token> SaveTokenFromCodeAsync(string code)
    {
        var token = oAuth20TokenHelper.GetAccessToken<WordpressLoginProvider>(code);
        ArgumentNullException.ThrowIfNull(token);

        await tokenHelper.SaveTokenAsync(new Token(token, AppAttr));

        return token;
    }

    public async Task DeleteTokenAsync(OAuth20Token token)
    {
        ArgumentNullException.ThrowIfNull(token);

        await tokenHelper.DeleteTokenAsync(AppAttr);
    }
}

[Singleton]
public class WordpressHelper(ILogger<WordpressHelper> logger, RequestHelper requestHelper)
{
    public enum WordpressStatus
    {
        draft = 0,
        publish = 1
    }

    public WordpressMeInfo GetWordpressMeInfo(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<WordpressMeInfo>(WordpressLoginProvider.GetWordpressMeInfo(requestHelper, token));
        }
        catch (Exception ex)
        {
            logger.ErrorGetWordpressInfo(ex);

            return new WordpressMeInfo();
        }

    }

    public bool CreateWordpressPost(string title, string content, int status, string blogId, OAuth20Token token)
    {
        try
        {
            var wpStatus = ((WordpressStatus)status).ToString();
            WordpressLoginProvider.CreateWordpressPost(requestHelper, title, content, wpStatus, blogId, token);

            return true;
        }
        catch (Exception ex)
        {
            logger.ErrorCreateWordpressPost(ex);

            return false;
        }
    }
}

public class WordpressMeInfo
{
    [JsonPropertyName("token_site_id")]
    public string TokenSiteId { get; set; }

    [JsonPropertyName("username")]
    public string UserName { get; set; }
}