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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for configuring the Two-Factor Authentication (TFA) settings.
/// </summary>
/// <example>
/// {
///   "type": "EnumValue",
///   "id": {},
///   "trustedIps": ["item1", "item2"],
///   "mandatoryUsers": [],
///   "mandatoryGroups": []
/// }
/// </example>
public class TfaRequestsDto
{
    /// <summary>
    /// The two-factor authentication type.
    /// </summary>
    /// <example>None</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TfaRequestsDtoType Type { get; set; }

    /// <summary>
    /// The ID of the user for whom the TFA settings are being configured.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The list of IP addresses that bypass TFA verification.
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public List<string> TrustedIps { get; set; }

    /// <summary>
    /// The list of user IDs for whom TFA is mandatory.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>
    /// The list group IDs whose members must use TFA.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryGroups { get; set; }
}

/// <summary>
/// The two-factor authentication type.
/// </summary>
public enum TfaRequestsDtoType
{
    [Description("None")]
    None = 0,

    [Description("Sms")]
    Sms = 1,

    [Description("App")]
    App = 2
}

/// <summary>
/// The request parameters for validating the two-factor authentication codes.
/// </summary>
public class TfaValidateRequestsDto
{
    /// <summary>
    /// The verification code provided by the user.
    /// </summary>
    /// <example>123456</example>
    public required string Code { get; set; }

    /// <summary>
    /// Specifies whether the authentication is session-based.
    /// </summary>
    /// <example>true</example>
    public bool Session { get; set; }
}