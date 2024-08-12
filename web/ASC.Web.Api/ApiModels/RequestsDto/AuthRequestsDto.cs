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

public class AuthRequestsDto
{
    [SwaggerSchemaCustomString("Username / email")]
    public string UserName { get; set; }

    [SwaggerSchemaCustomString("Password")]
    public string Password { get; set; }

    [SwaggerSchemaCustomString("Password hash")]
    public string PasswordHash { get; set; }

    [SwaggerSchemaCustomString("Provider type")]
    public string Provider { get; set; }

    [SwaggerSchemaCustomString("Provider access token")]
    public string AccessToken { get; set; }

    [SwaggerSchemaCustomString("Serialized user profile")]
    public string SerializedProfile { get; set; }

    [SwaggerSchemaCustomString("Two-factor authentication code")]
    public string Code { get; set; }

    [SwaggerSchemaCustomString("Code for getting a token")]
    public string CodeOAuth { get; set; }

    [SwaggerSchemaCustom("Session based authentication or not")]
    public bool Session { get; set; }

    [SwaggerSchemaCustom<ConfirmData>("Confirmation data")]
    public ConfirmData ConfirmData { get; set; }

    [SwaggerSchemaCustomString("Type of captcha", Example = "Default")]
    public RecaptchaType RecaptchaType { get; set; }

    [SwaggerSchemaCustomString("reCAPTCHA response")]
    public string RecaptchaResponse { get; set; }

    [SwaggerSchemaCustomString("Culture")]
    public string Culture { get; set; }
}

public class MobileRequestsDto
{
    [SwaggerSchemaCustomString("Mobile phone")]
    public string MobilePhone { get; set; }
}

public class ConfirmData
{
    [SwaggerSchemaCustomString("Email address")]
    public string Email { get; set; }

    [SwaggerSchemaCustom("Access an account for the first time or not", Nullable = true)]
    public bool? First { get; set; }

    [SwaggerSchemaCustomString("Key")]
    public string Key { get; set; }
}
