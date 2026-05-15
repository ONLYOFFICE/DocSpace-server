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
