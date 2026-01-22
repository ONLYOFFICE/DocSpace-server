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
// the GNU AGPL at: http://creativecommons.org/licenses/agpl-3.0.html
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

namespace ASC.People.ApiModels.V3.ResponseDto.Users;

/// <summary>
/// Paginated list response containing multiple users with navigation metadata.
/// </summary>
/// <remarks>
/// This DTO provides a complete paginated response for user queries including:
/// - The actual user data for the current page
/// - Pagination metadata (counts, page numbers, totals)
/// - Navigation flags (hasNext, hasPrevious)
/// - HATEOAS links for pagination navigation
///
/// Pagination Design:
/// - Uses offset-based pagination (page number + page size)
/// - Page numbers are 1-based (first page is page 1, not 0)
/// - Default page size is typically 25, maximum is often 100
/// - Total count is always provided for UI pagination controls
///
/// HATEOAS Navigation:
/// The links dictionary provides URLs for:
/// - self: Current page
/// - first: First page of results
/// - last: Last page of results
/// - next: Next page (if available)
/// - previous: Previous page (if available)
///
/// These links allow API clients to:
/// - Navigate through pages without building URLs manually
/// - Implement "load more" or infinite scroll patterns
/// - Build pagination UI controls automatically
/// - Handle page navigation without hardcoding logic
///
/// Performance Considerations:
/// - Large result sets are paginated to reduce memory usage
/// - Clients should not request all pages at once
/// - Consider caching responses with appropriate TTL
/// - Use query parameters (filters, search) to reduce total count
///
/// Usage Scenarios:
/// - User directory listings with pagination
/// - Search results across large user bases
/// - Administrative user management interfaces
/// - Batch processing with page-by-page iteration
/// - AI-powered user discovery and analysis
/// </remarks>
/// <example>
/// {
///   "items": [
///     {
///       "id": "550e8400-e29b-41d4-a716-446655440000",
///       "email": "john.doe@company.com",
///       "firstName": "John",
///       "lastName": "Doe",
///       ...
///     }
///   ],
///   "totalCount": 150,
///   "pageNumber": 1,
///   "pageSize": 25,
///   "totalPages": 6,
///   "hasNextPage": true,
///   "hasPreviousPage": false,
///   "links": {
///     "self": "/api/3.0/users?page=1&pageSize=25",
///     "first": "/api/3.0/users?page=1&pageSize=25",
///     "last": "/api/3.0/users?page=6&pageSize=25",
///     "next": "/api/3.0/users?page=2&pageSize=25"
///   }
/// }
/// </example>
public class UserListResponseDtoV3
{
    /// <summary>
    /// The collection of users for the current page.
    /// Empty array if no users match the query criteria.
    /// </summary>
    public IEnumerable<UserResponseDtoV3> Items { get; set; }

    /// <summary>
    /// The total number of users matching the query across all pages.
    /// Used for calculating pagination UI controls and total pages.
    /// </summary>
    /// <example>150</example>
    public int TotalCount { get; set; }

    /// <summary>
    /// The current page number (1-based indexing).
    /// First page is 1, not 0.
    /// </summary>
    /// <example>1</example>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items requested per page.
    /// Actual items returned may be less on the last page.
    /// </summary>
    /// <example>25</example>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of pages available.
    /// Calculated as Ceiling(TotalCount / PageSize).
    /// </summary>
    /// <example>6</example>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates whether there is a next page available.
    /// True if PageNumber < TotalPages.
    /// Useful for "load more" buttons and infinite scroll.
    /// </summary>
    /// <example>true</example>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Indicates whether there is a previous page available.
    /// True if PageNumber > 1.
    /// Useful for "previous page" navigation buttons.
    /// </summary>
    /// <example>false</example>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Hypermedia links for pagination navigation following HATEOAS principles.
    /// Provides URLs for self, first, last, next (if available), and previous (if available) pages.
    ///
    /// Link Keys:
    /// - self: Current page URL
    /// - first: First page URL
    /// - last: Last page URL
    /// - next: Next page URL (only if HasNextPage is true)
    /// - previous: Previous page URL (only if HasPreviousPage is true)
    /// </summary>
    /// <example>
    /// {
    ///   "self": "/api/3.0/users?page=2&pageSize=25",
    ///   "first": "/api/3.0/users?page=1&pageSize=25",
    ///   "last": "/api/3.0/users?page=6&pageSize=25",
    ///   "next": "/api/3.0/users?page=3&pageSize=25",
    ///   "previous": "/api/3.0/users?page=1&pageSize=25"
    /// }
    /// </example>
    public Dictionary<string, string> Links { get; set; }
}
