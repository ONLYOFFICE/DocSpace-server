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

namespace ASC.Common.Radicale;

public abstract class RadicaleEntity(IConfiguration configuration, InstanceCrypto instanceCrypto)
{
    public string Uid { get; set; }

    protected readonly string _defaultRadicaleUrl = configuration["radicale:path"] != null ? configuration["radicale:path"] : "http://localhost:5232";
    protected const string DefaultAddBookName = "11111111-1111-1111-1111-111111111111";
    protected const string ReadonlyAddBookName = "11111111-1111-1111-1111-111111111111-readonly";

    public string GetRadicaleUrl(string url, string email, bool isReadonly = false, bool isCardDav = false, bool isRedirectUrl = false, string entityId = "", string itemID = "")
    {
        string requestUrl;
        var currentUserName = url.StartsWith("http") ? email.ToLower() + "@" + new Uri(url).Host : email.ToLower() + "@" + url;
        var protocolType = !isCardDav ? "/caldav/" : "/carddav/";
        var serverUrl = isRedirectUrl ? new Uri(url).Scheme + "://" + new Uri(url).Host + protocolType :
            _defaultRadicaleUrl;
        if (isCardDav)
        {
            var addbookId = isReadonly ? ReadonlyAddBookName : DefaultAddBookName;
            requestUrl = itemID != "" ? _defaultRadicaleUrl + "/" + HttpUtility.UrlEncode(currentUserName) + "/" + addbookId + "/" + itemID + ".vcf" :
                isRedirectUrl ? serverUrl + HttpUtility.UrlEncode(currentUserName) + "/" + addbookId :
                _defaultRadicaleUrl + "/" + HttpUtility.UrlEncode(currentUserName) + "/" + addbookId;
        }
        else
        {
            requestUrl = itemID != "" ? serverUrl + HttpUtility.UrlEncode(currentUserName) + "/" + entityId + (isReadonly ? "-readonly" : "") +
                                        "/" + HttpUtility.UrlEncode(itemID) + ".ics" :
                                        serverUrl + HttpUtility.UrlEncode(currentUserName) + "/" + entityId + (isReadonly ? "-readonly" : "");
        }
        return requestUrl;
    }

    public async Task<string> GetSystemAuthorizationAsync()
    {
        if (configuration["radicale:admin"] == null || configuration["radicale:admin"] == "")
        {
            return null;
        }
        return configuration["radicale:admin"] + ":" + await instanceCrypto.EncryptAsync(configuration["radicale:admin"]);
    }

    protected string GetData(string sample, string name, string description, string backgroundColor)
    {
        var numbers = Regex.Split(backgroundColor, @"\D+");
        var color = numbers.Length > 4 ? HexFromRGB(int.Parse(numbers[1]), int.Parse(numbers[2]), int.Parse(numbers[3])) : "#000000";
        return string.Format(sample, name, color, description);
    }

    private string HexFromRGB(int r, int g, int b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}