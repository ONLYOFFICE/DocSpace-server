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

namespace ASC.People.ApiModels.ResponseDto;

/// <summary>
/// The response data for the API key operations.
/// </summary>
public class ApiKeyResponseDto
{
    /// <summary>
    /// The API key unique identifier.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid Id { get; set; }

    /// <summary>
    /// The API key name.
    /// </summary>
    /// <example>My API Key</example>
    public required string Name { get; set; }

    /// <summary>
    /// The full API key value (only returned when creating a new key).
    /// </summary>
    /// <example>api_key_1234567890abcdef</example>
    public required string Key { get; set; }

    /// <summary>
    /// The API key postfix (used for identification).
    /// </summary>
    /// <example>...cdef</example>
    public string KeyPostfix { get; set; }

    /// <summary>
    /// The list of permissions granted to the API key.
    /// </summary>
    /// <example>["read", "write", "delete"]</example>
    public required List<string> Permissions { get; set; }

    /// <summary>
    /// The date and time when the API key was last used.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public ApiDateTime LastUsed { get; set; }

    /// <summary>
    /// The date and time when the API key was created.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public ApiDateTime CreateOn { get; set; }

    /// <summary>
    /// The identifier of the user who created the API key.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "displayName": "Mike Zanyatski"}</example>
    public EmployeeDto CreateBy { get; set; }

    /// <summary>
    /// The date and time when the API key expires.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public ApiDateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the API key is active or not.
    /// </summary>
    /// <example>true</example>
    public required bool IsActive { get; set; } = true;
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class ApiKeyMapper(ApiDateTimeHelper apiDateTimeHelper, EmployeeDtoHelper employeeWrapperHelper)
{
    [MapperIgnoreTarget(nameof(ApiKeyResponseDto.LastUsed))]
    [MapperIgnoreTarget(nameof(ApiKeyResponseDto.CreateOn))]
    [MapperIgnoreTarget(nameof(ApiKeyResponseDto.CreateBy))]
    [MapperIgnoreTarget(nameof(ApiKeyResponseDto.ExpiresAt))]
    private partial ApiKeyResponseDto Map(ApiKey source);

    public async Task<ApiKeyResponseDto> MapManual(ApiKey source)
    {
        var result = Map(source);
        result.LastUsed = source.LastUsed.HasValue ? apiDateTimeHelper.Get(source.LastUsed.Value) : null;
        result.CreateOn = apiDateTimeHelper.Get(source.CreateOn);
        result.CreateBy = await employeeWrapperHelper.GetAsync(source.CreateBy);
        result.ExpiresAt = source.ExpiresAt.HasValue ? apiDateTimeHelper.Get(source.ExpiresAt.Value) : null;
        return result;
    }
}