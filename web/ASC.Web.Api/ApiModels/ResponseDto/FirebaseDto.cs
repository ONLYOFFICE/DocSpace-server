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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The Firebase parameters.
/// </summary>
public class FirebaseDto
{
    /// <summary>
    /// The Firebase API key.
    /// </summary>
    /// <example>AIzaSyDxK9L3j4H8mN2pQ5rS6tU7vW8xY9zA1bC</example>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The Firebase authentication domain.
    /// </summary>
    /// <example>myapp-12345.firebaseapp.com</example>
    public required string AuthDomain { get; set; }

    /// <summary>
    /// The Firebase project ID.
    /// </summary>
    /// <example>myapp-12345</example>
    public required string ProjectId { get; set; }

    /// <summary>
    /// The Firebase storage bucket.
    /// </summary>
    /// <example>myapp-12345.appspot.com</example>
    public required string StorageBucket { get; set; }

    /// <summary>
    /// The Firebase messaging sender ID.
    /// </summary>
    /// <example>123456789012</example>
    public required string MessagingSenderId { get; set; }

    /// <summary>
    /// The Firebase application ID.
    /// </summary>
    /// <example>1:123456789012:web:a1b2c3d4e5f6g7h8</example>
    public required string AppId { get; set; }

    /// <summary>
    /// The Firebase measurement ID.
    /// </summary>
    /// <example>G-ABCD123456</example>
    public required string MeasurementId { get; set; }

    /// <summary>
    /// The Firebase database URL.
    /// </summary>
    /// <example>https://myapp-12345.firebaseio.com</example>
    public required string DatabaseURL { get; set; }
}