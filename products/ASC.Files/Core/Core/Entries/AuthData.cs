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

namespace ASC.Files.Core;

/// <summary>
/// The authentication data.
/// </summary>
[DebuggerDisplay("{Login} {Password} {RawToken} {Url}")]
public class AuthData(string url = null, string login = null, string password = null, string token = null, string provider = null)
{
    /// <summary>
    /// The authentication login.
    /// </summary>
    /// <example>user@example.com</example>
    public string Login { get; init; } = login ?? string.Empty;

    /// <summary>
    /// The authentication password.
    /// </summary>
    /// <example>p@ssw0rd!</example>
    public string Password { get; init; } = password ?? string.Empty;

    /// <summary>
    /// The authentication raw token.
    /// </summary>
    /// <example>{"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","expires_in":3600}</example>
    public string RawToken { get; init; } = token ?? string.Empty;

    /// <summary>
    /// The authentication URL.
    /// </summary>
    /// <example>https://auth.example.com</example>
    [Url]
    public string Url { get; set; } = url ?? string.Empty;

    /// <summary>
    /// The authentication provider.
    /// </summary>
    /// <example>OAuth2</example>
    public string Provider { get; init; } = provider ?? string.Empty;

    /// <summary>
    /// The authentication token.
    /// </summary>
    /// <example>
    /// {
    ///   "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "refresh_token": "def50200a1b2c3d4e5f6...",
    ///   "expires_in": 3600,
    ///   "client_id": "my-client-id",
    ///   "client_secret": "my-client-secret",
    ///   "redirect_uri": "https://app.example.com/callback",
    ///   "timestamp": "2026-01-01T00:00:00Z"
    /// }
    /// </example>
    public OAuth20Token Token
    {
        get
        {
            return field ??= OAuth20Token.FromJson(RawToken);
        }
        set;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty((Url ?? string.Empty) + (Login ?? string.Empty) + (Password ?? string.Empty) + (RawToken ?? string.Empty));
    }
}