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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The keys to store for one third-party authorization or storage provider.
/// </summary>
/// <example>
/// {
///   "name": "google",
///   "props": [{"name": "googleClientId", "value": "1234567890-abc.apps.googleusercontent.com"}]
/// }
/// </example>
public class SaveAuthKeysRequestDto
{
    /// <summary>
    /// The internal key of the provider, such as `google` or `box`. Take it from the `name` of
    /// `GET api/2.0/settings/authservice`.
    /// </summary>
    /// <example>google</example>
    public string Name { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>Google</example>
    public string Title { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>Lets users sign in with a Google account.</example>
    public string Description { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>Create a project in the Google Cloud console and copy its OAuth client ID and secret.</example>
    public string Instruction { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>true</example>
    public bool CanSet { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>false</example>
    public bool Paid { get; set; }

    /// <summary>
    /// The keys of the provider with their new values, by the key names `GET api/2.0/settings/authservice` lists in
    /// `props`.
    /// </summary>
    /// <example>[{"name": "googleClientId", "value": "1234567890-abc.apps.googleusercontent.com"}]</example>
    public List<AuthKeyRequest> Props { get; set; }
}

/// <summary>
/// One key of a provider and the value to store for it.
/// </summary>
public class AuthKeyRequest
{
    /// <summary>
    /// The key name, as `GET api/2.0/settings/authservice` lists it in `props`.
    /// </summary>
    /// <example>googleClientId</example>
    public required string Name { get; set; }

    /// <summary>
    /// The value to store. An empty string clears the key.
    /// </summary>
    /// <example>1234567890-abc.apps.googleusercontent.com</example>
    [StringLength(4000)]
    public required string Value { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>Client ID</example>
    public string Title { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>text</example>
    public string Type { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>["s3", "gcs"]</example>
    public List<string> Options { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>storageType</example>
    public string DependsOn { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>s3</example>
    public string DependsOnValue { get; set; }
}
