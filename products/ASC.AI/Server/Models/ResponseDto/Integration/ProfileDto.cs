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

using ASC.AI.Integration.Profiles;
using ASC.AI.Models.RequestDto.Integration;

namespace ASC.AI.Models.ResponseDto.Integration;

public class ProfileDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string ProviderType { get; init; }
    public required string BaseUrl { get; init; }
    public string? Key { get; init; }
    public required string ModelId { get; init; }
    public bool? Reasoning { get; init; }
    public Capabilities? Capabilities { get; init; }
    public bool? UseResponsesApi { get; init; }
    public bool? CanUseTool { get; init; }
    public long CreatedAt { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ProfileMapper
{
    public static partial ProfileDto MapToDto(Profile profile);

    public static partial ProfileData MapToProfileData(CreateProfileRequestDto dto);

    [MapProperty(nameof(UpdateProfileRequestDto.Id), nameof(Profile.Id))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.Name)}", nameof(Profile.Name))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.ProviderType)}", nameof(Profile.ProviderType))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.BaseUrl)}", nameof(Profile.BaseUrl))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.Key)}", nameof(Profile.Key))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.ModelId)}", nameof(Profile.ModelId))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.Reasoning)}", nameof(Profile.Reasoning))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.Capabilities)}", nameof(Profile.Capabilities))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.UseResponsesApi)}", nameof(Profile.UseResponsesApi))]
    [MapProperty($"{nameof(UpdateProfileRequestDto.Body)}.{nameof(UpdateProfileBody.CanUseTool)}", nameof(Profile.CanUseTool))]
    public static partial Profile MapToProfile(UpdateProfileRequestDto dto);

    private static long MapDateTimeToMs(DateTime dateTime) =>
        new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
