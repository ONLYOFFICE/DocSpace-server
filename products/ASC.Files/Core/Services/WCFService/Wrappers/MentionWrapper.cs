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
/// The parameters of a user mentioned in a message.
/// </summary>
public class MentionWrapper
{
    internal MentionWrapper() { }

    /// <summary>
    /// The user information.
    /// </summary>
    public UserInfo User { get; internal set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    [EmailAddress]
    public string Email { get; internal set; }

    /// <summary>
    /// The user unique identification.
    /// </summary>
    public string Id { get; internal set; }

    /// <summary>
    /// The path to the user's avatar.
    /// </summary>
    public string Image { get; internal set; }

    /// <summary>
    /// Specifies whether the user has the access to the file where they are mentioned.
    /// </summary>
    public bool HasAccess { get; internal set; }

    /// <summary>
    /// The user full name.
    /// </summary>
    public string Name { get; internal set; }
}

/// <summary>
/// The mention message parameters.
/// </summary>
public class MentionMessageWrapper
{
    /// <summary>
    /// The config parameter which contains the information about the action in the document that will be scrolled to.
    /// </summary>
    public ActionLinkConfig ActionLink { get; set; }

    /// <summary>
    /// A list of emails that will receive the mention message.
    /// </summary>
    public List<string> Emails { get; set; }

    /// <summary>
    /// The mention message.
    /// </summary>
    [StringLength(255)]
    public string Message { get; set; }
}

/// <summary>
/// The request parameters for sending the mention message.
/// </summary>
public class MentionMessageWrapperRequestDto<T>
{
    /// <summary>
    /// The file ID with the mention message.
    /// </summary>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// The mention message.
    /// </summary>
    [FromBody]
    public MentionMessageWrapper MentionMessage { get; set; }
}