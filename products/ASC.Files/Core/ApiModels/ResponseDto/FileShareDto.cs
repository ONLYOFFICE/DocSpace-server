// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.ApiModels.ResponseDto;

public class FileShareDto
{
    /// <summary>
    /// Sharing rights
    /// </summary>
    public FileShare Access { get; set; }

    [SwaggerSchemaCustom<object>("A user who has the access to the specified file")]
    public object SharedTo { get; set; }

    [SwaggerSchemaCustom("Specifies if the file is locked by this user or not", Example = false)]
    public bool IsLocked { get; set; }

    /// <summary>
    /// Specifies if this user is an owner of the specified file or not
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Spceifies if this user can edit the access to the specified file or not
    /// </summary>
    public bool CanEditAccess { get; set; }

    /// <summary>
    /// Subject type
    /// </summary>
    public SubjectType SubjectType { get; set; }
}

public class FileShareLink
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Share link
    /// </summary>
    public string ShareLink { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// link type
    /// </summary>
    public LinkType LinkType { get; set; }
    public string Password { get; set; }
    public bool? DenyDownload { get; set; }
    public bool? IsExpired { get; set; }
    public bool Primary { get; set; }
    public bool? Internal { get; set; }
    public string RequestToken { get; set; }
}

public enum LinkType
{
    [SwaggerEnum(Description = "Invitation")]
    Invitation,

    [SwaggerEnum(Description = "External")]
    External
}

[Scope]
public class FileShareDtoHelper(
    GroupSummaryDtoHelper groupSummaryDtoHelper,
    UserManager userManager,
    EmployeeFullDtoHelper employeeWraperFullHelper,
    ApiDateTimeHelper apiDateTimeHelper)
{
    public async Task<FileShareDto> Get(AceWrapper aceWrapper)
    {
        var result = new FileShareDto
        {
            IsOwner = aceWrapper.Owner,
            IsLocked = aceWrapper.LockedRights,
            CanEditAccess = aceWrapper.CanEditAccess,
            SubjectType = aceWrapper.SubjectType
        };

        if (aceWrapper.SubjectGroup)
        {
            if (!string.IsNullOrEmpty(aceWrapper.Link))
            {
                var date = aceWrapper.FileShareOptions?.ExpirationDate;
                var expired = aceWrapper.FileShareOptions?.IsExpired;

                result.SharedTo = new FileShareLink
                {
                    Id = aceWrapper.Id,
                    Title = aceWrapper.FileShareOptions?.Title,
                    ShareLink = aceWrapper.Link,
                    ExpirationDate = date.HasValue && date.Value != default ? apiDateTimeHelper.Get(date) : null,
                    Password = aceWrapper.FileShareOptions?.Password,
                    DenyDownload = aceWrapper.FileShareOptions?.DenyDownload,
                    LinkType = aceWrapper.SubjectType switch
                    {
                        SubjectType.InvitationLink => LinkType.Invitation,
                        SubjectType.ExternalLink => LinkType.External,
                        SubjectType.PrimaryExternalLink => LinkType.External,
                        _ => LinkType.Invitation
                    },
                    IsExpired = expired,
                    Primary = aceWrapper.SubjectType == SubjectType.PrimaryExternalLink,
                    Internal = aceWrapper.FileShareOptions?.Internal,
                    RequestToken = aceWrapper.RequestToken
                };
            }
            else
            {
                //Shared to group
                result.SharedTo = await groupSummaryDtoHelper.GetAsync(await userManager.GetGroupInfoAsync(aceWrapper.Id));
            }
        }
        else
        {
            result.SharedTo = await employeeWraperFullHelper.GetFullAsync(await userManager.GetUsersAsync(aceWrapper.Id));
        }

        result.Access = aceWrapper.Access;

        return result;
    }
}
