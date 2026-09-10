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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>One storage service this portal can connect, with the values a connection form needs.</summary>
public record ProviderDto(string Name, string Key, bool Connected, bool Oauth = false, string RedirectUrl = null, bool RequiredConnectionUrl = false, string ClientId = null)
{
    /// <summary>
    /// The display name of the service, and the only thing that tells the WebDAV presets apart: `kDrive`, `Yandex`,
    /// `WebDav`, `Nextcloud` and `ownCloud` all report the same key.
    /// </summary>
    /// <example>Nextcloud</example>
    public string Name { get; init; } = Name;

    /// <summary>The value to send as `providerKey` when an account of this service is connected.</summary>
    /// <example>WebDav</example>
    public string Key { get; init; } = Key;

    /// <summary>
    /// Whether the service can be used on this portal: it is enabled in the configuration and, for an OAuth service,
    /// its application is registered. It says nothing about whether an account of it is connected.
    /// </summary>
    /// <example>true</example>
    public bool Connected { get; init; } = Connected;

    /// <summary>
    /// Whether an account of this service is connected with an OAuth 2.0 authorization code in `token`; when false,
    /// it is connected with `login` and `password`.
    /// </summary>
    /// <example>true</example>
    public bool Oauth { get; init; } = Oauth;

    /// <summary>
    /// The redirect URL this portal is registered with at the service, to build the consent screen URL from. It comes
    /// back as null for the services that do not use OAuth.
    /// </summary>
    /// <example>https://example.com/thirdparty</example>
    public string RedirectUrl { get; init; } = RedirectUrl;

    /// <summary>
    /// Whether an account of this service cannot be connected without `url`, which is the case for the WebDAV servers
    /// whose address is not known in advance. The presets with a fixed address and the OAuth services do not need it.
    /// </summary>
    /// <example>false</example>
    public bool RequiredConnectionUrl { get; init; } = RequiredConnectionUrl;

    /// <summary>
    /// The OAuth 2.0 client ID this portal is registered with at the service, to build the consent screen URL from.
    /// It comes back as null for the services that do not use OAuth.
    /// </summary>
    /// <example>l1s2h3d4f5g6h7j8k9l0</example>
    public string ClientId { get; init; } = ClientId;
}