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

using ASC.Core.Common.EF.Model;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The invitation link parameters.
/// </summary>
public class InvitationLinkDto
{
    /// <summary>
    /// The ID of the invitation link.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The type of employee role for the invitation link.
    /// </summary>
    /// <example>0</example>
    [JsonConverter(typeof(JsonNumberEnumConverter<EmployeeType>))]
    public required EmployeeType EmployeeType { get; set; }

    /// <summary>
    /// The expiration date of the invitation link.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Expiration { get; set; }

    /// <summary>
    /// Indicates whether the invitation link has expired.
    /// </summary>
    /// <example>true</example>
    public bool IsExpired { get; set; }

    /// <summary>
    /// The maximum number of times the invitation link can be used.
    /// </summary>
    /// <example>1</example>
    public int? MaxUseCount { get; set; }

    /// <summary>
    /// The current number of times the invitation link has been used.
    /// </summary>
    /// <example>1</example>
    public int CurrentUseCount { get; set; }

    /// <summary>
    /// The URL of the invitation link.
    /// </summary>
    /// <example>https://example.com</example>
    public string Url { get; set; }
}

[Scope]
public class InvitationLinkDtoHelper(
    TenantUtil tenantUtil,
    ApiDateTimeHelper apiDateTimeHelper,
    Signature signature,
    CommonLinkUtility commonLinkUtility,
    IUrlShortener urlShortener)
{
    public async Task<InvitationLinkDto> GetAsync(InvitationLink source, string tenantAlias, Guid currentAccountId)
    {
        if (source == null)
        {
            return default;
        }

        var result = new InvitationLinkDto
        {
            Id = source.Id,
            EmployeeType = source.EmployeeType,
            IsExpired = source.Expiration != DateTime.MinValue && source.Expiration < DateTime.UtcNow,
            Expiration = source.Expiration != DateTime.MinValue ? apiDateTimeHelper.Get(tenantUtil.DateTimeFromUtc(source.Expiration)) : null,
            MaxUseCount = source.MaxUseCount,
            CurrentUseCount = source.CurrentUseCount
        };

        var key = signature.Create((int)source.EmployeeType + "." + source.Id + "." + currentAccountId + "." + tenantAlias);

        var link = commonLinkUtility.GetConfirmationUrl(key, ConfirmType.LinkInvite, currentAccountId);

        result.Url = await urlShortener.GetShortenLinkAsync($"{link}&emplType={source.EmployeeType:d}");

        return result;
    }
}