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


namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The secret of an external link of an item in a private room, wrapped by a manager's browser for one user who manages
/// the room's links.
/// </summary>
/// <example>{"userId": "4f9c6a1e-2b7d-4c3a-9e8f-1a2b3c4d5e6f", "publicKeyId": "0b8e2f4a-6c1d-4e9b-8a7f-3c2d1e0f9a8b", "wrap": "AgEAAAAgAAAAyQ0bP8m1rKx3Zt2pS0mQx0J6r8u3aT1oV1cE9lK4n2w="}</example>
public class LinkSecretWrapRequest
{
    /// <summary>
    /// The room owner or a room manager the wrap is made for. Obtain the managers with `GET api/2.0/files/rooms/{id}/share`.
    /// </summary>
    /// <example>4f9c6a1e-2b7d-4c3a-9e8f-1a2b3c4d5e6f</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The id of that user's public key the secret is wrapped for, as `GET api/2.0/privacyroom/{roomId}/access` lists it.
    /// </summary>
    /// <example>0b8e2f4a-6c1d-4e9b-8a7f-3c2d1e0f9a8b</example>
    public Guid PublicKeyId { get; set; }

    /// <summary>
    /// The secret of the link wrapped for that public key by the browser.
    /// </summary>
    /// <example>AgEAAAAgAAAAyQ0bP8m1rKx3Zt2pS0mQx0J6r8u3aT1oV1cE9lK4n2w=</example>
    [StringLength(4096)]
    public string Wrap { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class LinkSecretWrapRequestMapper
{
    public static partial LinkSecretWrap Map(this LinkSecretWrapRequest source);

    public static List<LinkSecretWrap> Map(this IEnumerable<LinkSecretWrapRequest> source) => source?.Select(Map).ToList();
}
