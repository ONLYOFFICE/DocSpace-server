// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
    public static partial Profile MapToProfile(UpdateProfileRequestDto dto);

    private static long MapDateTimeToMs(DateTime dateTime) =>
        new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
