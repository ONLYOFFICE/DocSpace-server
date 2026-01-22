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
// the GNU AGPL at: http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.People.ApiModels.V3.RequestDto.Groups;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating an existing group.
/// </summary>
/// <remarks>
/// Used for both PUT and PATCH operations on /api/3.0/groups/{id}.
///
/// All fields are optional for PATCH operations.
/// For PUT operations, typically all fields should be provided.
///
/// Side Effects:
/// - Changing name updates display name across the system
/// - Changing manager reassigns management permissions
/// - Adding members updates access control lists
/// - Removing members revokes group-based permissions
/// </remarks>
public class UpdateGroupRequestDtoV3
{
    /// <summary>
    /// The new name for the group.
    /// </summary>
    /// <example>Updated Engineering Team</example>
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; }

    /// <summary>
    /// The ID of the new group manager.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// List of user IDs to add to the group.
    /// </summary>
    /// <example>["660e8400-e29b-41d4-a716-446655440001"]</example>
    public List<Guid> MembersToAdd { get; set; }

    /// <summary>
    /// List of user IDs to remove from the group.
    /// </summary>
    /// <example>["770e8400-e29b-41d4-a716-446655440002"]</example>
    public List<Guid> MembersToRemove { get; set; }
}
