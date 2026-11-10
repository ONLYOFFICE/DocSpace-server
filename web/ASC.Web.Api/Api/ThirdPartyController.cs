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

/// <remarks>
/// The first step of connecting an external account to a portal: the OAuth 2.0 consent URL that a client opens so
/// that a user can grant this portal access to their Google, Dropbox, DocuSign, Box, OneDrive, WordPress or GitHub
/// account. Nothing is stored here and no token is issued - the single public operation only assembles the address of
/// the provider's own consent page out of the client id, secret and redirect URI that an administrator has saved for
/// the portal with `POST api/2.0/settings/authservice`. The provider sends the user back to this portal's
/// `thirdparty/{provider}/code` page with the authorization code in the URL fragment, and the code is then handed to
/// the module that needs the account - a storage connection takes it as `token` in `POST api/2.0/files/thirdparty`.
/// The operation is open to any signed-in portal user and needs no administrative right.
/// </remarks>
/// <name>thirdparty</name>
[Scope]
[ApiEndpoint("thirdparty")]
public class ThirdPartyController(OAuth20TokenHelper oAuth20TokenHelper) : ControllerBase
{
    /// <remarks>
    /// Returns the OAuth 2.0 consent URL of an external provider as a string. The user opens it in a browser to give
    /// the portal access to their account. First, save the client ID, secret, and redirect URI of the provider with
    /// `POST api/2.0/settings/authservice`. Without them, the URL has no `client_id`, and the provider refuses it.
    /// Any signed-in user can call it. The call is read-only. The URL also asks for the scopes the portal needs, such
    /// as Drive with offline access for Google. Its `state` points to the `thirdparty/{provider}/code` page of the
    /// portal. There, the provider returns the code, or an error if the user declines. Only Google, Dropbox,
    /// DocuSign, Box, OneDrive, WordPress, and GitHub produce a URL. For any other provider, the call returns 200
    /// with no URL. If the provider is not a valid value, the call returns 400. If the installation does not
    /// register the provider, the call returns 500. With `desktop=true` in the query, the call copies the query
    /// string into `state` for the callback. This call does not exchange the code. To
    /// connect a storage account, pass the code as `token` to `POST api/2.0/files/thirdparty`.
    /// </remarks>
    /// <summary>
    /// Get provider consent URL
    /// </summary>
    /// <path>api/2.0/thirdparty/{provider}</path>
    [Tags("ThirdParty")]
    [SwaggerResponse(200, "The consent URL of the provider, ready to be opened in a browser; empty when the requested provider is not one of the seven this operation supports", typeof(string))]
    [SwaggerResponse(400, "The `provider` in the path is not one of the login provider names or numbers")]
    [SwaggerResponse(500, "The provider is one of the seven supported ones but is not registered among the installation's authorization consumers")]
    [HttpGet("{provider}")]
    public string GetThirdPartyCode(ConfirmationCodeUrlRequestDto inDto)
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