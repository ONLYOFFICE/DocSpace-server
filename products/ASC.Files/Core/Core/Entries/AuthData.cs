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
/// The credentials of a third-party storage account. The portal takes them when an account is connected and does not
/// give them back afterwards.
/// </summary>
[DebuggerDisplay("{Login} {Password} {RawToken} {Url}")]
public class AuthData(string url = null, string login = null, string password = null, string token = null, string provider = null)
{
    /// <summary>The account name at the storage service.</summary>
    /// <example>user@example.com</example>
    public string Login { get; init; } = login ?? string.Empty;

    /// <summary>The password of the account at the storage service.</summary>
    /// <example>p@ssw0rd!</example>
    public string Password { get; init; } = password ?? string.Empty;

    /// <summary>The token of the account, kept as the raw JSON document the storage service issued it in.</summary>
    /// <example>{"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","expires_in":3600}</example>
    public string RawToken { get; init; } = token ?? string.Empty;

    /// <summary>The address of the storage server the account lives on.</summary>
    /// <example>https://cloud.example.com/remote.php/dav/files/admin/</example>
    [Url]
    public string Url { get; set; } = url ?? string.Empty;

    /// <summary>
    /// The storage service the credentials belong to, as the provider key the account was connected with.
    /// </summary>
    /// <example>WebDav</example>
    public string Provider { get; init; } = provider ?? string.Empty;

    /// <summary>The same token as in `rawToken`, parsed into its OAuth 2.0 fields.</summary>
    /// <example>{"access_token":"eyJhbGciOiJIUzI1NiJ9...","expires_in":3600}</example>
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