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
/// The portal's standing invitation link for one role: what it grants, how long it lasts, how often it was used.
/// </summary>
public class InvitationLinkDto
{
    /// <summary>
    /// The identifier to address the link by in `PUT api/2.0/portal/users/invitationlink` and
    /// `DELETE api/2.0/portal/users/invitationlink`. It survives a change of deadline or use limit, so it is
    /// worth storing rather than re-reading.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The role an account gets by joining through this link. A portal keeps at most one link per role, and the
    /// role of an existing link cannot be changed - the link has to be deleted and created again.
    /// </summary>
    /// <example>0</example>
    [JsonConverter(typeof(JsonNumberEnumConverter<EmployeeType>))]
    public required EmployeeType EmployeeType { get; set; }

    /// <summary>
    /// When the link stops working, in the portal time zone. It is empty for a link that never expires, which is
    /// what omitting the deadline on create or update leaves behind.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Expiration { get; set; }

    /// <summary>
    /// Whether that deadline has already passed. A link without a deadline always reports `false`, and an expired
    /// link is still returned rather than treated as gone - it can be revived by moving `expiration`.
    /// </summary>
    /// <example>true</example>
    public bool IsExpired { get; set; }

    /// <summary>
    /// How many accounts may join through the link in total. It is empty for a link with no use limit, and an
    /// update may not lower it below `currentUseCount`.
    /// </summary>
    /// <example>1</example>
    public int? MaxUseCount { get; set; }

    /// <summary>
    /// How many accounts have already joined through the link. It only ever grows, and reaching `maxUseCount`
    /// retires the link as surely as a passed deadline.
    /// </summary>
    /// <example>1</example>
    public int CurrentUseCount { get; set; }

    /// <summary>
    /// The shortened address to hand to the people being invited. It is signed for the account that read it, so
    /// two administrators are given two different URLs for one and the same link and both of them work; the `id`
    /// above, not this string, is what identifies the link.
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