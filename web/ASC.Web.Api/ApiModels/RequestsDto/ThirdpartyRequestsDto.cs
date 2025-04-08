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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for handling third-party OAuth authentication requests.
/// </summary>
public class ThirdpartyRequestsDto
{
    /// <summary>
    /// The authorization code received from the third-party OAuth provider.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// The callback URL where the user should be redirected after authentication.
    /// </summary>
    public string Redirect { get; set; }
}

/// <summary>
/// The request parameters for generating OAuth confirmation code.
/// </summary>
public class ConfirmationCodeRequestDto
{
    /// <summary>
    /// The callback URL for redirecting the user after successful authentication.
    /// </summary>
    [FromQuery(Name = "redirect")]
    public string Redirect { get; set; }

    /// <summary>
    /// The OAuth authorization code received from the identity provider.
    /// </summary>
    [FromQuery(Name = "code")]
    public string Code { get; set; }

    /// <summary>
    /// The error message or code returned by the OAuth provider if authentication fails.
    /// </summary>
    [FromQuery(Name = "error")]
    public string Error { get; set; }
}

/// <summary>
/// The request parameters for generating OAuth confirmation URLs.
/// </summary>
public class ConfirmationCodeUrlRequestDto
{
    /// <summary>
    /// The identity provider used for authentication.
    /// </summary>
    [FromRoute(Name = "provider")]
    public LoginProvider Provider { get; set; }
}