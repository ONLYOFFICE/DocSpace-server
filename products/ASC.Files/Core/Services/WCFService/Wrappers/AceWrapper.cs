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

namespace ASC.Web.Files.Services.WCFService;

public class AceCollection<T>
{
    public IEnumerable<T> Files { get; init; }
    public IEnumerable<T> Folders { get; init; }
    public List<AceWrapper> Aces { get; init; }
    public string Message { get; init; }
    public AceAdvancedSettingsWrapper AdvancedSettings { get; init; }
}

public class AceWrapper : IMapFrom<RoomInvitation>
{
    public Guid Id { get; set; }
    public string Email { get; init; }
    public SubjectType SubjectType { get; set; }
    public FileShareOptions FileShareOptions { get; init; }
    public bool CanEditAccess { get; set; }

    [JsonPropertyName("title")]
    public string SubjectName { get; set; }

    public string Link { get; set; }

    [JsonPropertyName("is_group")]
    public bool SubjectGroup { get; set; }

    public bool Owner { get; set; }

    [JsonPropertyName("ace_status")]
    public FileShare Access { get; set; }

    [JsonPropertyName("locked")]
    public bool LockedRights { get; set; }

    [JsonPropertyName("disable_remove")]
    public bool DisableRemove { get; set; }
    public string RequestToken { get; set; }

    [JsonIgnore] 
    public bool IsLink => (SubjectType is SubjectType.InvitationLink or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink) || !string.IsNullOrEmpty(Link);
}

public class AceShortWrapper(string subjectName, string permission, bool isLink)
{
    /// <summary>
    /// User
    /// </summary>
    public string User { get; init; } = subjectName;

    /// <summary>
    /// User access rights to the file
    /// </summary>
    public string Permissions { get; init; } = permission;

    /// <summary>
    /// Is link
    /// </summary>
    public bool isLink { get; init; } = isLink;
}

public class AceAdvancedSettingsWrapper
{
    public bool AllowSharingPrivateRoom { get; set; }
    public bool InvitationLink { get; init; }
}
