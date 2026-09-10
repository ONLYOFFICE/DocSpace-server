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

namespace ASC.Web.Files.Services.WCFService;

/// <summary>
/// The collection of the user access rights.
/// </summary>
public class AceCollection<T>
{
    /// <summary>
    /// The collection of shared files.
    /// </summary>
    public IEnumerable<T> Files { get; init; }

    /// <summary>
    /// The collection of shared folders.
    /// </summary>
    public IEnumerable<T> Folders { get; init; }

    /// <summary>
    /// The collection of access rights.
    /// </summary>
    public List<AceWrapper> Aces { get; init; }

    /// <summary>
    /// A message to send when notifying about the shared file.
    /// </summary>
    public string Message { get; init; }
}

/// <summary>
/// The parameters of the access rights.
/// </summary>
public class AceWrapper
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user email.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// The subject type.
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// The parameters of the file shared link.
    /// </summary>
    public FileShareOptions FileShareOptions { get; init; }

    /// <summary>
    /// Specifies whether a user with the access rights to a file can edit it or not.
    /// </summary>
    public bool CanEditAccess { get; set; }

    /// <summary>
    /// Determines whether internal access can be edited.
    /// </summary>
    public bool CanEditInternal { get; set; }

    /// <summary>
    /// Indicates whether the expiration date of access permissions can be edited.
    /// </summary>
    public bool CanEditExpirationDate { get; set; }

    /// <summary>
    /// Indicates whether the access rights can be revoked.
    /// </summary>
    public bool CanRevoke { get; set; }

    /// <summary>
    /// Determines whether the user has permission to modify the deny download setting for the file share.
    /// </summary>
    public bool CanEditDenyDownload { get; set; } = true;

    /// <summary>
    /// The subject name.
    /// </summary>
    [JsonPropertyName("title")]
    public string SubjectName { get; set; }

    /// <summary>
    /// The external or invitation link.
    /// </summary>
    public string Link { get; set; }

    /// <summary>
    /// Specifies whether the subject type is a group or not.
    /// </summary>
    [JsonPropertyName("is_group")]
    public bool SubjectGroup { get; set; }

    /// <summary>
    /// Specifies whether the access rights subject is the owner or not.
    /// </summary>
    public bool Owner { get; set; }

    /// <summary>
    /// The access rights type.
    /// </summary>
    [JsonPropertyName("ace_status")]
    public FileShare Access { get; set; }

    /// <summary>
    /// Specifies if the access rights are locked or not.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool LockedRights { get; set; }

    /// <summary>
    /// Specifies if the access rights can be removed or not.
    /// </summary>
    [JsonPropertyName("disable_remove")]
    public bool DisableRemove { get; set; }

    /// <summary>
    /// The request token of the access rights.
    /// </summary>
    public string RequestToken { get; set; }

    /// <summary>
    /// Specifies whether the subject type is a link or not.
    /// </summary>
    [JsonIgnore]
    public bool IsLink => SubjectType is SubjectType.InvitationLink or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink || !string.IsNullOrEmpty(Link);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class RoomInvitationMapper
{
    public static partial AceWrapper Map(this RoomInvitation source);
    public static partial List<AceWrapper> Map(this List<RoomInvitation> source);

}

/// <summary>
/// One line of a document sharing list in display form: who the document is shared with and the label of their access
/// level, rather than an access record with identifiers. Entries that deny access and invitation links are left out,
/// so the list names only the subjects and links that can currently open the document.
/// </summary>
public class AceShortWrapper(string subjectName, string permission, bool isLink)
{
    /// <summary>
    /// Who or what the line stands for, as a display string: the display name of a member, the name of a group, or
    /// the title given to a shared link when `isLink` is true. It is empty when the subject has no name to show - a
    /// shared link that was never given a title, for instance.
    /// </summary>
    /// <example>John Doe</example>
    public string User { get; init; } = subjectName;

    /// <summary>
    /// The access level of that subject as a localized label, not a code: inside a room it usually names the role the
    /// subject holds there ("Viewer", "Editor", "Room Manager"), while outside a room it names the access itself
    /// ("Read Only", "Full Access"). The wording comes from the portal resources and is translated for the current
    /// language, so show it to a person rather than compare it in code.
    /// </summary>
    /// <example>Read Only</example>
    public string Permissions { get; init; } = permission;

    /// <summary>
    /// Whether the line stands for a shared link instead of a member or a group. Clients use it to draw a link badge
    /// where they would otherwise draw an avatar.
    /// </summary>
    /// <example>false</example>
    public bool isLink { get; init; } = isLink;
}