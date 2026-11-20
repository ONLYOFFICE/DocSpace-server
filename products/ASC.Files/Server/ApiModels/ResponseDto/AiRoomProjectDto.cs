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

/// <summary>
/// An AI project that uses a Knowledge or an Instructions room as a source.
/// </summary>
public class AiRoomProjectDto
{
    /// <summary>
    /// ID of the project, the `id` that the AI project calls take, such as `GET api/2.0/files/rooms/{id}/sources`.
    /// </summary>
    /// <example>17040</example>
    public required int Id { get; set; }

    /// <summary>
    /// Project title.
    /// </summary>
    /// <example>Website relaunch</example>
    public required string Title { get; set; }

    /// <summary>
    /// The addresses of the project logo in four sizes, together with the colour and the built-in cover that are
    /// drawn when no logo was uploaded. A project without a logo answers with four empty addresses rather than with
    /// null.
    /// </summary>
    public LogoDto Logo { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AiRoomProjectDtoMapper(RoomLogoManager roomLogoManager)
{
    public async Task<AiRoomProjectDto> MapAsync(Folder<int> project)
    {
        var dto = Map(project);
        dto.Logo = await roomLogoManager.GetLogoAsync(project);

        return dto;
    }

    [MapperIgnoreTarget(nameof(AiRoomProjectDto.Logo))]
    private partial AiRoomProjectDto Map(Folder<int> project);
}
