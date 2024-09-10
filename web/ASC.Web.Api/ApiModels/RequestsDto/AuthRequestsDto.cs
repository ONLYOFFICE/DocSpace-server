﻿// (c) Copyright Ascensio System SIA 2009-2024
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
/// Authentication request parameters
/// </summary>
public class AuthRequestsDto
{
    /// <summary>
    /// Username / email
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// Provider type
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Provider access token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Serialized user profile
    /// </summary>
    public string SerializedProfile { get; set; }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Code for getting a token
    /// </summary>
    public string CodeOAuth { get; set; }

    /// <summary>
    /// Session based authentication or not
    /// </summary>
    public bool Session { get; set; }

    /// <summary>
    /// Confirmation data
    /// </summary>
    public ConfirmData ConfirmData { get; set; }

    /// <summary>
    /// Type of captcha
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// reCAPTCHA response
    /// </summary>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// Culture
    /// </summary>
    public string Culture { get; set; }
}

/// <summary>
/// Mobile phone request parameters
/// </summary>
public class MobileRequestsDto
{
    /// <summary>
    /// Mobile phone
    /// </summary>
    public string MobilePhone { get; set; }
}

public class ConfirmData
{
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Access an account for the first time or not
    /// </summary>
    public bool? First { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; set; }
}
