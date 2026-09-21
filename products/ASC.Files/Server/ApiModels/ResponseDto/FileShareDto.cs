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

#pragma warning disable CS0612 // Type or member is obsolete
namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// One access entry on a file, a folder or a room: who holds it, at which level, and what the caller may change about
/// it.
/// </summary>
public class FileShareDto
{
    /// <summary>
    /// The level the subject holds on the entry. On a link entry it is the level the link hands to whoever opens it,
    /// and in a batch answer `Varies` means the subject holds different levels on the listed entries.
    /// </summary>
    /// <example>10</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// Deprecated: repeats whichever of `sharedToUser`, `sharedToGroup` and `sharedLink` is filled in, so its shape
    /// changes from entry to entry. Read the three fields themselves instead.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    [Obsolete]
    public object SharedTo { get; set; }

    /// <summary>
    /// The account the entry belongs to. It is filled in only when `subjectType` says an account, and is null for a
    /// group entry and for a link.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto SharedToUser { get; set; }

    /// <summary>
    /// The portal group the entry belongs to, which hands the level to everybody in it. It is filled in only for a
    /// group entry, and is null otherwise.
    /// </summary>
    /// <example>{"id": "9a2c1b3e-6d47-4f10-9b52-ac7d3e5f0812", "name": "Marketing"}</example>
    public GroupSummaryDto SharedToGroup { get; set; }

    /// <summary>
    /// The sharing link the entry stands for, together with everything set on it. It is filled in only for a link
    /// entry, and is null for an account or a group.
    /// </summary>
    /// <example>{"id": "9a2c1b3e-6d47-4f10-9b52-ac7d3e5f0812", "title": "Shared document", "primary": true}</example>
    public FileShareLink SharedLink { get; set; }

    /// <summary>
    /// Whether this entry is the caller's own, which is why they cannot change its level. Link entries never report
    /// it.
    /// </summary>
    /// <example>false</example>
    public required bool IsLocked { get; set; }

    /// <summary>
    /// Whether the subject created the entry the access is given on, and so cannot be removed from it.
    /// </summary>
    /// <example>false</example>
    public required bool IsOwner { get; set; }

    /// <summary>
    /// Whether the caller may change the level of this entry. It is false on the caller's own entry, on every link,
    /// and whenever the caller may not hand out access at all.
    /// </summary>
    /// <example>true</example>
    public required bool CanEditAccess { get; set; }

    /// <summary>
    /// Whether the caller may switch this link between being open to anybody and asking the visitor to sign in to the
    /// portal first.
    /// </summary>
    /// <example>true</example>
    public required bool CanEditInternal { get; set; }

    /// <summary>
    /// Whether the caller may forbid downloading through this link. Only a link of a virtual data room reports true,
    /// and only while the room itself still allows downloads.
    /// </summary>
    /// <example>true</example>
    public required bool CanEditDenyDownload { get; set; }

    /// <summary>Whether the caller may move the moment this link stops working.</summary>
    /// <example>true</example>
    public required bool CanEditExpirationDate { get; set; }

    /// <summary>
    /// Whether the caller may take this entry away altogether, which for a link means deleting the link.
    /// </summary>
    /// <example>true</example>
    public required bool CanRevoke { get; set; }
    /// <summary>
    /// What the entry was given to, which tells which of the three subject fields is filled in: an account, a group,
    /// or one of the kinds of link.
    /// </summary>
    /// <example>0</example>
    public required SubjectType SubjectType { get; set; }
}

/// <summary>A sharing link of a file, a folder or a room, with everything set on it.</summary>
public class FileShareLink
{
    /// <summary>The identifier of the link, the one to send back as `linkId` to change or delete it.</summary>
    /// <example>9a2c1b3e-6d47-4f10-9b52-ac7d3e5f0812</example>
    public Guid Id { get; set; }

    /// <summary>The name the link is listed under, which its author is free to choose and to leave empty.</summary>
    /// <example>Shared document</example>
    public string Title { get; set; }

    /// <summary>
    /// The shortened address to hand out. Opening it is what turns the link into access; the address stays the same
    /// while the link exists.
    /// </summary>
    /// <example>https://portal.example.com/s/a1b2c3d4</example>
    public string ShareLink { get; set; }

    /// <summary>
    /// The moment the link stops working, written with the offset of the portal time zone. Null when the link was
    /// left without an end.
    /// </summary>
    /// <example>2025-06-30T23:59:59.000+03:00</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// Which of the two jobs the link does: letting somebody into the room as a member, or handing out the entry
    /// itself. The counters of uses are filled in for the first kind only.
    /// </summary>
    /// <example>1</example>
    public LinkType LinkType { get; set; }

    /// <summary>
    /// The password a visitor has to send before the link resolves, readable only by those who may manage the link.
    /// Empty when the link asks for none.
    /// </summary>
    /// <example>S3cretPhrase</example>
    public string Password { get; set; }

    /// <summary>
    /// Whether visitors coming through this link may only read the entry in the editor and not download or print it.
    /// </summary>
    /// <example>false</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// Whether the moment in `expirationDate` has already passed, which leaves the link in place but refuses
    /// everybody who opens it.
    /// </summary>
    /// <example>false</example>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// Whether this is the one link the entry always keeps: a public or a form-filling room is given it at creation,
    /// and deleting it there only makes a new one.
    /// </summary>
    /// <example>true</example>
    public bool Primary { get; set; }

    /// <summary>
    /// Whether the visitor has to sign in to the portal before the link resolves, as opposed to it being open to
    /// anybody who has the address.
    /// </summary>
    /// <example>false</example>
    public bool? Internal { get; set; }

    /// <summary>
    /// The key that stands for this link in the calls that resolve it, such as `GET api/2.0/files/share/{key}`. It is
    /// filled in for links that hand out the entry, and empty for the ones that invite into a room.
    /// </summary>
    /// <example>gg9J4mBW7pW9Wk0HqQoQ9L2mS1x6bK8vTnQ0aZ3</example>
    public string RequestToken { get; set; }

    /// <summary>
    /// How many accounts may still join the room through this invitation link in total. Null on a link that hands out
    /// the entry, where nothing is counted.
    /// </summary>
    /// <example>10</example>
    public int? MaxUseCount { get; set; }

    /// <summary>
    /// How many accounts have already joined through this invitation link. Once it reaches `maxUseCount` the link
    /// stops letting anybody else in. Null on a link that hands out the entry.
    /// </summary>
    /// <example>5</example>
    public int? CurrentUseCount { get; set; }
}

/// <summary>The job a sharing link does.</summary>
public enum LinkType
{
    [Description("Invitation")]
    Invitation,

    [Description("External")]
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

                if (result.SharedLink.LinkType == LinkType.Invitation)
                {
                    result.SharedLink.MaxUseCount = aceWrapper.FileShareOptions?.MaxUseCount;
                    result.SharedLink.CurrentUseCount = aceWrapper.FileShareOptions?.CurrentUseCount;
                }

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