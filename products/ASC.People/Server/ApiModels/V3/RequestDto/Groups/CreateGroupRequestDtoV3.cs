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

namespace ASC.People.ApiModels.V3.RequestDto.Groups;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new group (department).
/// </summary>
/// <remarks>
/// This DTO is used when creating a new group via POST /api/3.0/groups.
///
/// Business Context:
/// Groups (also called departments) are organizational units that help structure
/// users within the DocSpace tenant. They are used for:
/// - Access control and permissions management
/// - Organizing users by department or team
/// - Simplifying user management at scale
///
/// Validation Rules:
/// - Name is required and must be between 1 and 255 characters
/// - Manager must be a valid user ID (optional)
/// - Members must be an array of valid user IDs (optional)
///
/// Post-Creation Side Effects:
/// - Group is created with specified name
/// - Manager (if specified) is assigned and automatically added as member
/// - Members (if specified) are added to the group
/// - Access control lists are updated
/// - Webhook events are fired (GroupCreated)
/// - Audit log entry is created
/// </remarks>
/// <example>
/// {
///   "name": "Engineering Team",
///   "managerId": "550e8400-e29b-41d4-a716-446655440000",
///   "memberIds": [
///     "660e8400-e29b-41d4-a716-446655440001",
///     "770e8400-e29b-41d4-a716-446655440002"
///   ]
/// }
/// </example>
public class CreateGroupRequestDtoV3
{
    /// <summary>
    /// The name of the group.
    /// </summary>
    /// <remarks>
    /// Required field. Must be unique within the tenant.
    /// Used for display purposes and group identification.
    /// </remarks>
    /// <example>Engineering Team</example>
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 255 characters")]
    public string Name { get; set; }

    /// <summary>
    /// The ID of the user who will manage this group.
    /// </summary>
    /// <remarks>
    /// Optional. If specified, the user will be:
    /// - Assigned as the group manager
    /// - Automatically added to the group as a member
    /// - Given additional permissions for managing group members
    ///
    /// The manager can:
    /// - Add/remove group members
    /// - Update group information
    /// - View all group members
    /// </remarks>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// List of user IDs to add as group members.
    /// </summary>
    /// <remarks>
    /// Optional. Users specified here will be added to the group immediately.
    ///
    /// Notes:
    /// - Manager is automatically added as a member (no need to include in this list)
    /// - Duplicate IDs are ignored
    /// - Invalid user IDs will cause validation error
    /// - Users must exist in the tenant
    /// </remarks>
    /// <example>["660e8400-e29b-41d4-a716-446655440001", "770e8400-e29b-41d4-a716-446655440002"]</example>
    public List<Guid> MemberIds { get; set; }
}
