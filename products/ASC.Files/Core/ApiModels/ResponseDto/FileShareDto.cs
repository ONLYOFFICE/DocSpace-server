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

#pragma warning disable CS0612 // Type or member is obsolete
namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The file sharing information and access rights.
/// </summary>
public class FileShareDto
{
    /// <summary>
    /// The access rights type.
    /// </summary>
    public FileShare Access { get; set; }

    /// <summary>
    /// The user who has the access to the specified file.
    /// </summary>
    [Obsolete]
    public object SharedTo { get; set; }
    
    /// <summary>
    /// The user who has the access to the specified file.
    /// </summary>
    public EmployeeFullDto SharedToUser { get; set; }
    
    /// <summary>
    /// The user who has the access to the specified file.
    /// </summary>
    public GroupSummaryDto SharedToGroup { get; set; }
    
    /// <summary>
    /// The user who has the access to the specified file.
    /// </summary>
    public FileShareLink SharedLink { get; set; }

    /// <summary>
    /// Specifies if the access right is locked or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public required bool IsLocked { get; set; }

    /// <summary>
    /// Specifies if the user is an owner of the specified file or not.
    /// </summary>
    public required bool IsOwner { get; set; }

    /// <summary>
    /// Specifies if the user can edit the access to the specified file or not.
    /// </summary>
    public required bool CanEditAccess { get; set; }

    /// <summary>
    /// Indicates whether internal editing permissions are granted.
    /// </summary>
    public required bool CanEditInternal { get; set; }

    /// <summary>
    /// Determines whether the user has permission to modify the deny download setting for the file share.
    /// </summary>
    public required bool CanEditDenyDownload { get; set; }
    
    /// <summary>
    /// Indicates whether the expiration date of access permissions can be edited.
    /// </summary>
    public required bool CanEditExpirationDate { get; set; }

    /// <summary>
    /// Specifies whether the file sharing access can be revoked by the current user.
    /// </summary>
    public required bool CanRevoke { get; set; }
    /// <summary>
    /// The subject type.
    /// </summary>
    public required SubjectType SubjectType { get; set; }
}

/// <summary>
/// A shareable link for a file with its configuration and status.
/// </summary>
public class FileShareLink
{
    /// <summary>
    /// The unique identifier of the shared link.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The title of the shared content.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The URL for accessing the shared content.
    /// </summary>
    public string ShareLink { get; set; }

    /// <summary>
    /// The date when the shared link expires.
    /// </summary>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// The sharing link type (e.g., Invitation).
    /// </summary>
    public LinkType LinkType { get; set; }

    /// <summary>
    /// The password protection for accessing the shared content.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Indicates whether downloading of the shared content is prohibited.
    /// </summary>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// Indicates whether the shared link has expired.
    /// </summary>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// Indicates whether this is the primary shared link.
    /// </summary>
    public bool Primary { get; set; }

    /// <summary>
    /// Indicates whether the link is for the internal sharing only.
    /// </summary>
    public bool? Internal { get; set; }

    /// <summary>
    /// The token for validating access requests.
    /// </summary>
    public string RequestToken { get; set; }
}

/// <summary>
/// Defines the types of the sharing links.
/// </summary>
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
    EmployeeFullDtoHelper employeeWrapperFullHelper,
    ApiDateTimeHelper apiDateTimeHelper)
{
    public async Task<FileShareDto> Get(AceWrapper aceWrapper)
    {
        if (aceWrapper == null)
        {
            return null;
        }
        
        var result = new FileShareDto
        {
            IsOwner = aceWrapper.Owner,
            IsLocked = aceWrapper.LockedRights,
            CanEditAccess = aceWrapper.CanEditAccess,
            CanEditInternal = aceWrapper.CanEditInternal,
            CanEditDenyDownload = aceWrapper.CanEditDenyDownload,
            CanEditExpirationDate = aceWrapper.CanEditExpirationDate,
            CanRevoke = aceWrapper.CanRevoke,
            SubjectType = aceWrapper.SubjectType
        };

        if (aceWrapper.SubjectGroup)
        {
            if (!string.IsNullOrEmpty(aceWrapper.Link))
            {
                var date = aceWrapper.FileShareOptions?.ExpirationDate;
                var expired = aceWrapper.FileShareOptions?.IsExpired;

                result.SharedLink = new FileShareLink
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
                result.SharedTo = result.SharedLink;
            }
            else
            {
                result.SharedToGroup = await groupSummaryDtoHelper.GetAsync(await userManager.GetGroupInfoAsync(aceWrapper.Id));
                result.SharedTo = result.SharedToGroup;
            }
        }
        else
        {
            result.SharedToUser = await employeeWrapperFullHelper.GetFullAsync(await userManager.GetUsersAsync(aceWrapper.Id));
            result.SharedTo = result.SharedToUser;
        }

        result.Access = aceWrapper.Access;

        return result;
    }
}
