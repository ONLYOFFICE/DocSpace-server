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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The external sharing information and validation data.
/// </summary>
public class ExternalShareDto
{
    /// <summary>
    /// The external data status.
    /// </summary>
    /// <example>0</example>
    public required Status Status { get; set; }

    /// <summary>
    /// The external data ID.
    /// </summary>
    /// <example>123</example>
    public string Id { get; set; }

    /// <summary>
    /// The external data title.
    /// </summary>
    /// <example>Shared Document</example>
    public string Title { get; set; }

    /// <summary>
    /// The type of the external data.
    /// </summary>
    /// <example>0</example>
    public FileEntryType? Type { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The unique identifier of the shared entity.
    /// </summary>
    /// <example>456</example>
    public string EntityId { get; set; }

    /// <summary>
    /// The title of the shared entity.
    /// </summary>
    /// <example>Entity Title</example>
    public string EntityTitle { get; set; }

    /// <summary>
    /// The entry type of the external data.
    /// </summary>
    /// <example>0</example>
    public FileEntryType? EntityType { get; set; }

    /// <summary>
    /// Indicates whether the entity represents a room.
    /// </summary>
    /// <example>false</example>
    public bool? IsRoom { get; set; }

    /// <summary>
    /// Specifies whether to share the external data or not.
    /// </summary>
    /// <example>true</example>
    public required bool Shared { get; set; }

    /// <summary>
    /// The link ID of the external data.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid LinkId { get; set; }

    /// <summary>
    /// Specifies whether the user is authenticated or not.
    /// </summary>
    /// <example>true</example>
    public required bool IsAuthenticated { get; set; }

    /// <summary>
    /// The room ID of the external data.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomMember { get; set; }
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ExternalShareDtoMapper
{
    public static partial ExternalShareDto Map(this ValidationInfo source);
}