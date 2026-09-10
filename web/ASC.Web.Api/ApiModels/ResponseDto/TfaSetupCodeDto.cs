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
/// The secret to enrol in an authenticator application, in both of the forms an application can take it.
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
    /// The label the authenticator application will list the credential under, which is the caller's own email
    /// address. It identifies the entry to a person, and no application checks it.
    /// </summary>
    /// <example>john.doe@onlyoffice.com</example>
    public string Account { get; private set; }

    /// <summary>
    /// The secret in the base32 form that is typed into an application by hand. It describes the very same
    /// credential as `qrCodeSetupImageUrl`, and repeating the call hands back the same value for the account until
    /// the credential is reset.
    /// </summary>
    /// <example>JBSWY3DPEHPK3PXP</example>
    public string ManualEntryKey { get; private set; }

    /// <summary>
    /// The same secret as a scannable image, given as a `data:image/png;base64,` URL that can be rendered
    /// directly - it is not a link to fetch.
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
