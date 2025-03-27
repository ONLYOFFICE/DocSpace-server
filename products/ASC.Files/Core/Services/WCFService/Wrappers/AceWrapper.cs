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

    /// <summary>
    /// The information about the advanced settings which allow to share the document with other users.
    /// </summary>
    public AceAdvancedSettingsWrapper AdvancedSettings { get; init; }
}

/// <summary>
/// The parameters of the access rights.
/// </summary>
public class AceWrapper : IMapFrom<RoomInvitation>
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
    /// Specifies whether a user with the access rights to a file can edit it or not.
    /// </summary>
    public FileShareOptions FileShareOptions { get; init; }

    /// <summary>
    /// Specifies whether a user with the access rights to a file can edit it or not.
    /// </summary>
    public bool CanEditAccess { get; set; }

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
    public bool IsLink => (SubjectType is SubjectType.InvitationLink or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink) || !string.IsNullOrEmpty(Link);
}

/// <summary>
/// The information about the settings which allow to share the document with other users.
/// </summary>
public class AceShortWrapper(string subjectName, string permission, bool isLink)
{
    /// <summary>
    /// The name of the user the document will be shared with.
    /// </summary>
    public string User { get; init; } = subjectName;

    /// <summary>
    /// The access rights for the user with the name above.
    /// Can be "Full Access", "Read Only", or "Deny Access".
    /// </summary>
    public string Permissions { get; init; } = permission;

    /// <summary>
    /// Specifies whether to change the user icon to the link icon.
    /// </summary>
    public bool isLink { get; init; } = isLink;
}

/// <summary>
/// The information about the advanced settings which allow to share the document with other users.
/// </summary>
public class AceAdvancedSettingsWrapper
{
    /// <summary>
    /// Specifies whether to allow sharing private room or not.
    /// </summary>
    public bool AllowSharingPrivateRoom { get; set; }

    /// <summary>
    /// Specifies whether to allow creating an invitation link or not.
    /// </summary>
    public bool InvitationLink { get; init; }
}
