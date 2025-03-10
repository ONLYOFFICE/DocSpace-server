// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.Controllers;

///<summary>
/// Third-party API.
///</summary>
///<name>thirdparty</name>
[ApiExplorerSettings(IgnoreApi = true)]
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("thirdparty")]
public class ThirdPartyController(OAuth20TokenHelper oAuth20TokenHelper) : ControllerBase
{
    /// <summary>
    /// Returns a request to get the confirmation code from URL.
    /// </summary>
    /// <short>Get the code request</short>
    /// <remarks>List of providers: Google, Dropbox, Docusign, Box, OneDrive, Wordpress.</remarks>
    /// <path>api/2.0/thirdparty/{provider}</path>
    [Tags("ThirdParty")]
    [SwaggerResponse(200, "Code request", typeof(object))]
    [HttpGet("{provider}")]
    public object Get(ConfirmationCodeUrlRequestDto inDto)
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
            _ => null
        };
    }

    /// <summary>
    /// Returns the confirmation code for requesting an OAuth token.
    /// </summary>
    /// <short>Get the confirmation code</short>
    /// <path>api/2.0/thirdparty/{provider}/code</path>
    [Tags("ThirdParty")]
    [SwaggerResponse(200, "Confirmation code", typeof(object))]
    [SwaggerResponse(400, "Error")]
    [HttpGet("{provider}/code")]
    public object GetCode(ConfirmationCodeRequestDto inDto)
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
                        ? (string.IsNullOrEmpty(code)
                                ? string.Empty
                                : "code=" + HttpUtility.UrlEncode(code))
                        : ("error/" + HttpUtility.UrlEncode(error)));

        return url;
    }
}
