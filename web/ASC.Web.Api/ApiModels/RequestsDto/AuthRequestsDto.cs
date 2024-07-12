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
    [SwaggerSchemaCustom(Example = "some text", Description = "Username / email")]
    public string UserName { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Password")]
    public string Password { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Password hash")]
    public string PasswordHash { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Provider type")]
    public string Provider { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Provider access token")]
    public string AccessToken { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Serialized user profile")]
    public string SerializedProfile { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Two-factor authentication code")]
    public string Code { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Code for getting a token")]
    public string CodeOAuth { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Session based authentication or not")]
    public bool Session { get; set; }

    [SwaggerSchemaCustom(Description = "Confirmation data")]
    public ConfirmData ConfirmData { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Type of captcha")]
    public RecaptchaType RecaptchaType { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "reCAPTCHA response")]
    public string RecaptchaResponse { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Culture")]
    public string Culture { get; set; }
}

public class MobileRequestsDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Mobile phone")]
    public string MobilePhone { get; set; }
}

public class ConfirmData
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Email address")]
    public string Email { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Access an account for the first time or not", Nullable = true)]
    public bool? First { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Key")]
    public string Key { get; set; }
}
