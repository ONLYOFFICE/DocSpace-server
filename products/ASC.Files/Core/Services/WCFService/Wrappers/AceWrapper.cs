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
/// The ace collection parameters.
/// </summary>
public class AceCollection<T>
{
    /// <summary>
    /// The ace collection files.
    /// </summary>
    public IEnumerable<T> Files { get; init; }

    /// <summary>
    /// The ace collection folders.
    /// </summary>
    public IEnumerable<T> Folders { get; init; }

    /// <summary>
    /// The ace collection aces.
    /// </summary>
    public List<AceWrapper> Aces { get; init; }

    /// <summary>
    /// The ace collection message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// The ace collection advanced settings.
    /// </summary>
    public AceAdvancedSettingsWrapper AdvancedSettings { get; init; }
}

/// <summary>
/// The ace wrapper parameters.
/// </summary>
public class AceWrapper : IMapFrom<RoomInvitation>
{
    /// <summary>
    /// The ace wrapper ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ace wrapper email.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// The ace wrapper subject type.
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// The ace wrapper file share options.
    /// </summary>
    public FileShareOptions FileShareOptions { get; init; }
    public bool CanEditAccess { get; set; }

    /// <summary>
    /// The ace wrapper subject name.
    /// </summary>
    [JsonPropertyName("title")]
    public string SubjectName { get; set; }

    /// <summary>
    /// The ace wrapper link.
    /// </summary>
    public string Link { get; set; }

    /// <summary>
    /// The ace wrapper subject group.
    /// </summary>
    [JsonPropertyName("is_group")]
    public bool SubjectGroup { get; set; }

    /// <summary>
    /// Specifies whether the ace wrapper is the owner or not.
    /// </summary>
    public bool Owner { get; set; }

    /// <summary>
    /// The ace wrapper file access.
    /// </summary>
    [JsonPropertyName("ace_status")]
    public FileShare Access { get; set; }

    /// <summary>
    /// The ace wrapper locked rights.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool LockedRights { get; set; }

    /// <summary>
    /// Specifies whether to disable removing of the ace wrapper.
    /// </summary>
    [JsonPropertyName("disable_remove")]
    public bool DisableRemove { get; set; }

    /// <summary>
    /// The request token of the ace wrapper.
    /// </summary>
    public string RequestToken { get; set; }

    /// <summary>
    /// Specifies whether it is link or not.
    /// </summary>
    [JsonIgnore] 
    public bool IsLink => (SubjectType is SubjectType.InvitationLink or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink) || !string.IsNullOrEmpty(Link);
}

/// <summary>
/// The ace short wrapper parameters.
/// </summary>
public class AceShortWrapper(string subjectName, string permission, bool isLink)
{
    /// <summary>
    /// The user of the wrapper.
    /// </summary>
    public string User { get; init; } = subjectName;

    /// <summary>
    /// The user access rights to the file.
    /// </summary>
    public string Permissions { get; init; } = permission;

    /// <summary>
    /// Specifies whether the message is link.
    /// </summary>
    public bool isLink { get; init; } = isLink;
}

/// <summary>
/// The ace advanced settings wrapper parameters.
/// </summary>
public class AceAdvancedSettingsWrapper
{
    /// <summary>
    /// Specifies whether to allow sharing private room.
    /// </summary>
    public bool AllowSharingPrivateRoom { get; set; }

    /// <summary>
    /// Specifies whether it is the invitation link or not.
    /// </summary>
    public bool InvitationLink { get; init; }
}
