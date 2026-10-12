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

namespace ASC.FederatedLogin;

/// <summary>
/// Configuration of the login handler, bound once per process: <see cref="Login"/> itself is
/// scoped, so binding it there would run the configuration binder on every request.
/// </summary>
[Singleton]
public class FederatedLoginSettings(IConfiguration configuration)
{
    /// <summary>
    /// The complete allow-list for absolute return urls — nothing is implicitly allowed, so a
    /// deployment that hands out an absolute (rather than relative) return url to the portal
    /// itself has to list its own host here as well. The host of the current request is
    /// deliberately not one of them: with <c>ForwardedHeaders.XForwardedHost</c> enabled it
    /// comes from a client-controlled header.
    /// </summary>
    public IReadOnlyCollection<string> AllowedReturnUrlHosts { get; } =
        configuration.GetSection("federated-login:allowed-return-url-hosts").Get<string[]>() ?? [];
}
