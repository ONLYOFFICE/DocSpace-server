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
/// The setup TFA code parameters.
/// </summary>
/// <example>
/// {
///   "account": "john.doe@onlyoffice.com",
///   "manualEntryKey": "JBSWY3DPEHPK3PXP",
///   "qrCodeSetupImageUrl": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGMAAgAABAABiCEmiQAAAABJRU5ErkJggg=="
/// }
/// </example>
public class TfaSetupCodeDto
{
    /// <summary>
    /// The account for which the setup code is generated.
    /// </summary>
    /// <example>john.doe@onlyoffice.com</example>
    public string Account { get; private set; }

    /// <summary>
    /// The manual entry key.
    /// </summary>
    /// <example>JBSWY3DPEHPK3PXP</example>
    public string ManualEntryKey { get; private set; }

    /// <summary>
    /// The QR-code setup image URL (base64-encoded PNG image).
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGMAAgAABAABiCEmiQAAAABJRU5ErkJggg==</example>
    public string QrCodeSetupImageUrl { get; private set; }

    /// <summary>
    /// Creates the setup TFA code parameters from the generated setup code.
    /// </summary>
    /// <param name="setupCode">The generated setup code.</param>
    /// <returns>The setup TFA code parameters.</returns>
    public static TfaSetupCodeDto FromSetupCode(SetupCode setupCode)
    {
        return new TfaSetupCodeDto
        {
            Account = setupCode.Account,
            ManualEntryKey = setupCode.ManualEntryKey,
            QrCodeSetupImageUrl = setupCode.QrCodeSetupImageUrl
        };
    }
}
