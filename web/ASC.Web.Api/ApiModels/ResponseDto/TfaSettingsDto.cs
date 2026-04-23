// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The parameters representing the Two-Factor Authentication (TFA) configuration settings.
/// </summary>
/// <example>
/// {
///   "id": "tfa-default",
///   "title": "Default TFA policy",
///   "enabled": true,
///   "available": true,
///   "trustedIps": ["item1", "item2"],
///   "mandatoryUsers": [],
///   "mandatoryGroups": []
/// }
/// </example>
public class TfaSettingsDto
{
    /// <summary>
    /// The ID of the TFA configuration.
    /// </summary>
    /// <example>tfa-default</example>
    public required string Id { get; set; }

    /// <summary>
    /// The display name or description of the TFA configuration.
    /// </summary>
    /// <example>Default TFA policy</example>
    public required string Title { get; set; }

    /// <summary>
    /// Indicates whether the TFA configuration is currently active.
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Indicates whether the TFA configuration can be used.
    /// </summary>
    /// <example>true</example>
    public required bool Available { get; set; }

    /// <summary>
    /// The list of IP addresses that are exempt from TFA requirements.
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public List<string> TrustedIps { get; set; }

    /// <summary>
    /// The list of user IDs that are required to use TFA.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>
    /// The list of group IDs whose members are required to use TFA.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryGroups { get; set; }
}

/// <summary>
/// The TFA confirmation data.
/// </summary>
/// <example>
/// {
/// "url": "https://example.com/confirm?type=TfaAuth&amp;key=abc123",
/// "cookieName": "asc_confirm_key_TfaAuth"
/// "cookieValue": "1234567890.abcdef"
/// }
/// </example>
public class TfaConfirmDataDto
{
    /// <summary>
    /// The confirmation URL.
    /// </summary>
    /// <example>https://example.com/confirm?type=TfaAuth&amp;key=abc123</example>
    public string Url { get; set; }

    /// <summary>
    /// The confirmation cookie name.
    /// </summary>
    /// <example>asc_confirm_key_TfaAuth</example>
    public string CookieName { get; set; }

    /// <summary>
    /// The confirmation cookie value.
    /// </summary>
    /// <example>1234567890.abcdef</example>
    public string CookieValue { get; set; }
}

/// <summary>
/// The TFA app code.
/// </summary>
/// <example>
/// {
/// "isUsed": true,
/// "code": "123456"
/// }
/// </example>
public class TfaAppCodeDto
{
    /// <summary>
    /// The TFA app code usage status.
    /// </summary>
    /// <example>true</example>
    public bool IsUsed { get; set; }

    /// <summary>
    /// The TFA app code.
    /// </summary>
    /// <example>123456</example>
    public string Code { get; set; }
}
