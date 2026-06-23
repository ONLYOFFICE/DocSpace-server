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

namespace ASC.Web.Api.Controllers;

///<remarks>
/// Third-party API.
///</remarks>
///<name>thirdparty</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("thirdparty")]
public class ThirdPartyController(OAuth20TokenHelper oAuth20TokenHelper) : ControllerBase
{
    /// <remarks>
    /// Returns a request to get the confirmation code from URL.
    /// </remarks>
    /// <summary>Get the code request</summary>
    /// <remarks>List of providers: Google, Dropbox, Docusign, Box, OneDrive, Wordpress.</remarks>
    /// <path>api/2.0/thirdparty/{provider}</path>
    [Tags("ThirdParty")]
    [SwaggerResponse(200, "Code request", typeof(object))]
    [HttpGet("{provider}")]
    public object GetThirdPartyCode(ConfirmationCodeUrlRequestDto inDto)
    {
        var desktop = HttpContext.Request.Query["desktop"] == "true";
        var additionals = new Dictionary<string, string>();

        if (desktop)
        {
            additionals = HttpContext.Request.Query.ToDictionary(r => r.Key, r => r.Value.FirstOrDefault());
        }

        return inDto.Provider switch
        {
            LoginProvider.Google => oAuth20TokenHelper.RequestCode<GoogleLoginProvider>(
                GoogleLoginProvider.GoogleScopeDrive,
                GoogleLoginProvider.GoogleAdditionalArgs,
                additionalStateArgs: additionals),
            LoginProvider.Dropbox => oAuth20TokenHelper.RequestCode<DropboxLoginProvider>(
                additionalArgs: new Dictionary<string, string>
                {
                    { "force_reauthentication", "true" }, { "token_access_type", "offline" }
                }, additionalStateArgs: additionals),
            LoginProvider.Docusign => oAuth20TokenHelper.RequestCode<DocuSignLoginProvider>(
                DocuSignLoginProvider.DocuSignLoginProviderScopes,
                new Dictionary<string, string> { { "prompt", "login" } }, additionalStateArgs: additionals),
            LoginProvider.Box => oAuth20TokenHelper.RequestCode<BoxLoginProvider>(additionalStateArgs: additionals),
            LoginProvider.OneDrive => oAuth20TokenHelper.RequestCode<OneDriveLoginProvider>(
                OneDriveLoginProvider.OneDriveLoginProviderScopes, additionalStateArgs: additionals),
            LoginProvider.Wordpress => oAuth20TokenHelper.RequestCode<WordpressLoginProvider>(
                additionalStateArgs: additionals),
            LoginProvider.Github => oAuth20TokenHelper.RequestCode<GithubLoginProvider>(additionalStateArgs: additionals),
            _ => null
        };
    }

    /// <remarks>
    /// Returns the confirmation code for requesting an OAuth token.
    /// </remarks>
    /// <summary>Get the confirmation code</summary>
    /// <path>api/2.0/thirdparty/{provider}/code</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("ThirdParty")]
    [SwaggerResponse(200, "Confirmation code", typeof(object))]
    [SwaggerResponse(400, "Error")]
    [HttpGet("{provider}/code")]
    public object GetThirdPartyProviderCode(ConfirmationCodeRequestDto inDto)
    {
        try
        {
            if (!string.IsNullOrEmpty(inDto.Error))
            {
                if (inDto.Error == "access_denied")
                {
                    inDto.Error = "Canceled at provider";
                }
                throw new Exception(inDto.Error);
            }

            if (!string.IsNullOrEmpty(inDto.Redirect))
            {
                return AppendCode(inDto.Redirect, inDto.Code);
            }

            return inDto.Code;
        }
        catch (ThreadAbortException)
        {
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(inDto.Redirect))
            {
                return AppendCode(inDto.Redirect, error: ex.Message);
            }

            return ex.Message;
        }

        return null;
    }


    private static string AppendCode(string url, string code = null, string error = null)
    {
        url += (url.Contains('#') ? "&" : "#")
                + (string.IsNullOrEmpty(error)
                        ? string.IsNullOrEmpty(code)
                            ? string.Empty
                            : "code=" + HttpUtility.UrlEncode(code)
                        : "error/" + HttpUtility.UrlEncode(error));

        return url;
    }
}