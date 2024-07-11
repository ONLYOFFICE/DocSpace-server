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
/// </summary>
public class AuthRequestsDto
{
    /// <summary>Username / email</summary>
    /// <type>System.String, System</type>
    public string UserName { get; set; }

    /// <summary>Password</summary>
    /// <type>System.String, System</type>
    public string Password { get; set; }

    /// <summary>Password hash</summary>
    /// <type>System.String, System</type>
    public string PasswordHash { get; set; }

    /// <summary>Provider type</summary>
    /// <type>System.String, System</type>
    public string Provider { get; set; }

    /// <summary>Provider access token</summary>
    /// <type>System.String, System</type>
    public string AccessToken { get; set; }

    /// <summary>Serialized user profile</summary>
    /// <type>System.String, System</type>
    public string SerializedProfile { get; set; }

    /// <summary>Two-factor authentication code</summary>
    /// <type>System.String, System</type>
    public string Code { get; set; }

    /// <summary>Code for getting a token</summary>
    /// <type>System.String, System</type>
    public string CodeOAuth { get; set; }

    /// <summary>Session based authentication or not</summary>
    /// <type>System.Boolean, System</type>
    public bool Session { get; set; }

    /// <summary>Confirmation data</summary>
    /// <type>ASC.Web.Api.ApiModel.RequestsDto.ConfirmData, ASC.Web.Api</type>
    public ConfirmData ConfirmData { get; set; }

    /// <summary>Type of captcha</summary>
    /// <type>ASC.Web.Core.RecaptchaType, ASC.Web.Core</type>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>reCAPTCHA response</summary>
    /// <type>System.String, System</type>
    public string RecaptchaResponse { get; set; }
    
    /// <summary>Culture</summary>
    /// <type>System.String, System</type>
    public string Culture { get; set; }
}

/// <summary>
/// </summary>
public class MobileRequestsDto
{
    /// <summary>Mobile phone</summary>
    /// <type>System.String, System</type>
    public string MobilePhone { get; set; }
}

/// <summary>
/// </summary>
public class ConfirmData
{
    /// <summary>Email address</summary>
    /// <type>System.String, System</type>
    public string Email { get; set; }

    /// <summary>Access an account for the first time or not</summary>
    /// <type>System.Nullable{System.Boolean}, System</type>
    public bool? First { get; set; }

    /// <summary>Key</summary>
    /// <type>System.String, System</type>
    public string Key { get; set; }
}
