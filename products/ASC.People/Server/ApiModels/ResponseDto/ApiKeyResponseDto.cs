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