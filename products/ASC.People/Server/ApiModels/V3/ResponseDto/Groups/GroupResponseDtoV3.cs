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

namespace ASC.People.ApiModels.V3.ResponseDto.Groups;

using ASC.People.ApiModels.V3.ResponseDto.Users;

/// <summary>
/// Response DTO representing a group with full details.
/// </summary>
/// <remarks>
/// This DTO includes complete group information including manager and members.
/// Includes HATEOAS links for related resources.
/// </remarks>
public class GroupResponseDtoV3
{
    /// <summary>
    /// The unique identifier of the group.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    /// <example>Engineering Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The manager of the group.
    /// </summary>
    public UserSummaryDtoV3 Manager { get; set; }

    /// <summary>
    /// The number of members in the group.
    /// </summary>
    /// <example>15</example>
    public int MembersCount { get; set; }

    /// <summary>
    /// List of group members (only included when explicitly requested).
    /// </summary>
    public IEnumerable<UserSummaryDtoV3> Members { get; set; }

    /// <summary>
    /// HATEOAS links to related resources.
    /// </summary>
    /// <remarks>
    /// Includes links to:
    /// - self: This group's resource URL
    /// - members: Group members endpoint
    /// - manager: Group manager endpoint
    /// </remarks>
    public Dictionary<string, string> Links { get; set; }
}

/// <summary>
/// Minimal user information for group members.
/// </summary>
public class UserSummaryDtoV3
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// Paginated list of groups.
/// </summary>
public class GroupListResponseDtoV3
{
    /// <summary>
    /// List of groups in this page.
    /// </summary>
    public IEnumerable<GroupResponseDtoV3> Items { get; set; }

    /// <summary>
    /// Total number of groups matching the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// HATEOAS navigation links.
    /// </summary>
    public Dictionary<string, string> Links { get; set; }
}
