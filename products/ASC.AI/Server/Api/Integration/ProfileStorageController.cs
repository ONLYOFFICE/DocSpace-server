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

using ASC.AI.Models.ResponseDto.Integration;

namespace ASC.AI.Api.Integration;

[Scope]
[InternalRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ProfileStorageController(ProfileStorageService profileStorageService) : ControllerBase
{
    [HttpPost("integration/profiles")]
    public async Task<ProfileDto> CreateAsync(CreateProfileRequestDto inDto)
    {
        var created = await profileStorageService.CreateAsync(ProfileMapper.MapToProfileData(inDto));
        return ProfileMapper.MapToDto(created);
    }

    [HttpPost("integration/profiles/batch")]
    public async Task<IReadOnlyList<ProfileDto>> CreateManyAsync(CreateProfilesRequestDto inDto)
    {
        var profiles = inDto.Profiles.Select(ProfileMapper.MapToProfileData).ToList();
        var created = await profileStorageService.CreateManyAsync(profiles);
        return created.Select(ProfileMapper.MapToDto).ToList();
    }

    [HttpGet("integration/profiles/{id}")]
    public async Task<ProfileDto> ReadByIdAsync(ReadProfileRequestDto inDto)
    {
        var profile = await profileStorageService.ReadByIdAsync(inDto.Id);
        return ProfileMapper.MapToDto(profile);
    }

    [HttpGet("integration/profiles")]
    public async Task<List<ProfileDto>> ReadAllAsync()
    {
        var profiles = await profileStorageService.ReadAllAsync();
        return profiles.Select(ProfileMapper.MapToDto).ToList();
    }

    [HttpPut("integration/profiles/{id}")]
    public async Task<ProfileDto> UpdateAsync(UpdateProfileRequestDto inDto)
    {
        var updated = await profileStorageService.UpdateAsync(ProfileMapper.MapToProfile(inDto));
        return ProfileMapper.MapToDto(updated);
    }

    [HttpDelete("integration/profiles/{id}")]
    public async Task<IActionResult> DeleteAsync(DeleteProfileRequestDto inDto)
    {
        await profileStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }
}
