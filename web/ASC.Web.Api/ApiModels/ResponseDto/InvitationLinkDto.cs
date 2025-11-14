// (c) Copyright Ascensio System SIA 2009-2025
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
    public Guid Id { get; set; }

    /// <summary>
    /// The type of employee role for the invitation link (All, RoomAdmin, Guest, DocSpaceAdmin, User).
    /// </summary>
    public required EmployeeType EmployeeType { get; set; }

    /// <summary>
    /// The expiration date of the invitation link.
    /// </summary>
    public ApiDateTime Expiration { get; set; }

    /// <summary>
    /// The maximum number of times the invitation link can be used.
    /// </summary>
    public int MaxUseCount { get; set; }

    /// <summary>
    /// The current number of times the invitation link has been used.
    /// </summary>
    public int CurrentUseCount { get; set; }

    /// <summary>
    /// The URL of the invitation link.
    /// </summary>
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

        var result = new InvitationLinkDto()
        {
            Id = source.Id,
            EmployeeType = source.EmployeeType,
            Expiration = apiDateTimeHelper.Get(tenantUtil.DateTimeFromUtc(source.Expiration)),
            MaxUseCount = source.MaxUseCount,
            CurrentUseCount = source.CurrentUseCount
        };

        var key = signature.Create((int)source.EmployeeType + "." + source.Id + "." + currentAccountId + "." + tenantAlias);

        var link = commonLinkUtility.GetConfirmationUrl(key, ConfirmType.LinkInvite, currentAccountId);

        result.Url = await urlShortener.GetShortenLinkAsync($"{link}&emplType={source.EmployeeType:d}");

        return result;
    }
}