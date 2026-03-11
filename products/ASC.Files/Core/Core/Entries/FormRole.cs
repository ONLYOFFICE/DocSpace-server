// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Core.Core.Entries;

/// <summary>
/// The form role.
/// </summary>
public class FormRole
{
    /// <summary>
    /// The room ID.
    /// </summary>
    /// <example>1</example>
    public int RoomId { get; set; }

    /// <summary>
    /// The role name.
    /// </summary>
    /// <example>Manager</example>
    public string RoleName { get; set; }

    /// <summary>
    /// The role color.
    /// </summary>
    /// <example>#4781D1</example>
    public string RoleColor { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The role sequence.
    /// </summary>
    /// <example>12</example>
    public int Sequence { get; set; }

    /// <summary>
    /// Specifies if the role was submitted or not.
    /// </summary>
    /// <example>false</example>
    public bool Submitted { get; set; }

    /// <summary>
    /// The date and time when the role was opened.
    /// </summary>
    /// <example>2026-01-01T10:00:00Z</example>
    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// The date and time when the role was submitted.
    /// </summary>
    /// <example>2026-01-01T10:00:00Z</example>
    public DateTime SubmissionDate { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class FormRoleMapper
{
    public static partial FormRole Map(this DbFilesFormRoleMapping source);
}