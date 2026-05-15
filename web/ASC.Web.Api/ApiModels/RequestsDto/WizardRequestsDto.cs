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
/// The request parameters for initial configuration of the setup wizard.
/// </summary>
/// <example>
/// {
///   "lng": "en-US",
///   "timeZone": "UTC",
///   "amiId": "00000000-0000-0000-0000-000000000001",
///   "subscribeFromSite": true
/// }
/// </example>
public class WizardRequestsDto
{
    /// <summary>
    /// The user's email address for authentication and communication.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// The hashed representation of the user's password.
    /// </summary>
    /// <example>2DYmIoA/aYKEksFocEf6uw==</example>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// The user's preferred interface language code.
    /// </summary>
    /// <example>en-US</example>
    public string Lng { get; set; }

    /// <summary>
    /// The user's time zone identifier.
    /// </summary>
    /// <example>UTC</example>
    public string TimeZone { get; set; }

    /// <summary>
    /// The Amazon Machine Image (AMI) identifier.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string AmiId { get; set; }

    /// <summary>
    /// Specifies whether the user opted in for site communications.
    /// </summary>
    /// <example>true</example>
    public bool SubscribeFromSite { get; set; }

    public void Deconstruct(out string email, out string passwordHash, out string lng, out string timeZone, out string amiid, out bool subscribeFromSite)
    {
        (email, passwordHash, lng, timeZone, amiid, subscribeFromSite) = (Email, PasswordHash, Lng, TimeZone, AmiId, SubscribeFromSite);
    }
}