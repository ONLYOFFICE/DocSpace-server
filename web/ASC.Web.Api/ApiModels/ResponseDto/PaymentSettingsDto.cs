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
/// The payment settings parameters.
/// </summary>
/// <example>
/// {
///   "feedbackAndSupportUrl": "example value"
/// }
/// </example>
public class PaymentSettingsDto
{
    /// <summary>
    /// The email address for sales inquiries and support.
    /// </summary>
    /// <example>sales@example.com</example>
    public required string SalesEmail { get; set; }

    /// <summary>
    /// The URL for accessing the feedback and support resources.
    /// </summary>
    /// <example>https://example.com</example>
    public string FeedbackAndSupportUrl { get; set; }

    /// <summary>
    /// The URL for purchasing or upgrading the product.
    /// </summary>
    /// <example>https://example.com/buy</example>
    public required string BuyUrl { get; set; }

    /// <summary>
    /// Indicates whether the system is running in standalone mode.
    /// </summary>
    /// <example>false</example>
    public required bool Standalone { get; set; }

    /// <summary>
    /// The current license information.
    /// </summary>
    /// <example>{"trial": false, "dueDate": "2025-06-15T10:30:00.0000000Z"}</example>
    public required CurrentLicenseInfo CurrentLicense { get; set; }

    /// <summary>
    /// The maximum quota quantity.
    /// </summary>
    /// <example>1</example>
    public required int Max { get; set; }
}

/// <summary>
/// The current license information.
/// </summary>
public class CurrentLicenseInfo
{
    /// <summary>
    /// Specifies whether the license is trial or not.
    /// </summary>
    /// <example>false</example>
    public required bool Trial { get; set; }

    /// <summary>
    /// The date when the license expires.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public required DateTime DueDate { get; set; }
}