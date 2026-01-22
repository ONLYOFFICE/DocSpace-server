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

namespace ASC.People.Mappers;

using ASC.People.ApiModels.RequestDto;
using ASC.People.ApiModels.V3.RequestDto.Users;
using ASC.People.ApiModels.V3.ResponseDto.Users;

/// <summary>
/// Mapper service for converting between v2 DTOs, v3 DTOs, and domain models for user operations.
/// </summary>
/// <remarks>
/// This mapper serves as a translation layer that allows the v3 API to reuse
/// existing business logic from the v2 API without code duplication.
///
/// Key Responsibilities:
/// - Convert v3 request DTOs to v2 format for processing
/// - Convert v2/domain models to v3 response format
/// - Generate HATEOAS links for API discoverability
/// - Handle pagination metadata and list responses
/// - Map contact information and group summaries
///
/// Design Pattern:
/// This follows the Adapter pattern, adapting the new v3 interface to work
/// with existing v2 business logic, ensuring backward compatibility while
/// providing improved REST semantics.
///
/// Benefits:
/// - No duplication of business logic
/// - Consistent behavior between v2 and v3 APIs
/// - Easy maintenance (fixes in v2 logic benefit v3)
/// - Gradual migration path for clients
/// </remarks>
[Scope]
public class UserDtoMapperV3
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserDtoMapperV3(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Converts a v3 CreateUserRequest DTO to a v2 MemberRequest DTO.
    /// This allows the v3 API to reuse existing v2 user creation business logic.
    /// </summary>
    public MemberRequestDto ToV2CreateRequest(CreateUserRequestDtoV3 v3Dto)
    {
        return new MemberRequestDto
        {
            Email = v3Dto.Email,
            Password = v3Dto.Password,
            PasswordHash = v3Dto.PasswordHash,
            FirstName = v3Dto.FirstName,
            LastName = v3Dto.LastName,
            Type = v3Dto.Type,
            Title = v3Dto.Title,
            Department = v3Dto.Department,
            Location = v3Dto.Location,
            Sex = v3Dto.Sex,
            Birthday = v3Dto.Birthday,
            Worksfrom = v3Dto.WorksFrom,
            Comment = v3Dto.Comment,
            Contacts = v3Dto.Contacts,
            Files = v3Dto.AvatarUrl,
            Spam = v3Dto.AllowMarketing ?? false
        };
    }

    /// <summary>
    /// Converts a v3 UpdateUserRequest DTO to a v2 UpdateMemberRequest DTO.
    /// This allows the v3 API to reuse existing v2 user update business logic.
    /// </summary>
    public UpdateMemberRequestDto ToV2UpdateRequest(UpdateUserRequestDtoV3 v3Dto, string userId)
    {
        return new UpdateMemberRequestDto
        {
            UserId = userId,
            FirstName = v3Dto.FirstName,
            LastName = v3Dto.LastName,
            Title = v3Dto.Title,
            Department = v3Dto.Department,
            Location = v3Dto.Location,
            Sex = v3Dto.Sex,
            Birthday = v3Dto.Birthday,
            Worksfrom = v3Dto.WorksFrom,
            Comment = v3Dto.Comment,
            Contacts = v3Dto.Contacts,
            Spam = v3Dto.AllowMarketing,
            Disable = v3Dto.IsActive.HasValue ? !v3Dto.IsActive.Value : (bool?)null,
            IsUser = v3Dto.Type.HasValue ? v3Dto.Type.Value != EmployeeType.Guest : (bool?)null
        };
    }

    /// <summary>
    /// Converts a domain EmployeeFullDto to a v3 UserResponse DTO.
    /// </summary>
    public UserResponseDtoV3 FromDomain(EmployeeFullDto domain)
    {
        var baseUrl = GetBaseUrl();

        return new UserResponseDtoV3
        {
            Id = domain.Id,
            Email = domain.Email,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            DisplayName = domain.DisplayName,
            Type = domain.ActivationStatus.ToString(),
            Status = domain.Status.ToString(),
            ActivationStatus = domain.ActivationStatus.ToString(),
            Title = domain.Title,
            Departments = domain.Groups?.Select(g => new GroupSummaryDtoV3
            {
                Id = g.Id,
                Name = g.Name
            }),
            Location = domain.Location,
            Sex = domain.Sex,
            Birthday = domain.Birthday?.UtcTime,
            WorksFrom = domain.WorkFrom?.UtcTime ?? DateTime.UtcNow,
            Comment = domain.Notes,
            Contacts = domain.Contacts?.Select(c => new ContactDtoV3
            {
                Type = c.Type,
                Value = c.Value
            }),
            PhotoUrl = $"{baseUrl}/api/3.0/users/{domain.Id}/photo",
            CultureName = domain.CultureName,
            AllowMarketing = false, // Default value
            CreatedAt = domain.WorkFrom?.UtcTime ?? DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Links = new Dictionary<string, string>
            {
                ["self"] = $"{baseUrl}/api/3.0/users/{domain.Id}",
                ["photo"] = $"{baseUrl}/api/3.0/users/{domain.Id}/photo",
                ["contacts"] = $"{baseUrl}/api/3.0/users/{domain.Id}/contacts",
                ["groups"] = $"{baseUrl}/api/3.0/users/{domain.Id}/groups"
            }
        };
    }

    /// <summary>
    /// Converts a collection of domain users to a paginated v3 list response.
    /// </summary>
    public UserListResponseDtoV3 FromDomainList(
        IEnumerable<EmployeeFullDto> users,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var userDtos = users.Select(u => FromDomain(u)).ToList();
        var baseUrl = GetBaseUrl();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new UserListResponseDtoV3
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1,
            Links = BuildPaginationLinks(baseUrl, "/api/3.0/users", pageNumber, pageSize, totalPages)
        };
    }

    /// <summary>
    /// Gets the base URL (scheme + host) from the current HTTP request.
    /// Used for building absolute URLs in HATEOAS links.
    /// </summary>
    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return string.Empty;
        }

        return $"{request.Scheme}://{request.Host}";
    }

    /// <summary>
    /// Builds pagination navigation links for a list response.
    /// Creates self, first, last, next, and previous links as appropriate.
    /// </summary>
    private Dictionary<string, string> BuildPaginationLinks(
        string baseUrl,
        string path,
        int currentPage,
        int pageSize,
        int totalPages)
    {
        var links = new Dictionary<string, string>
        {
            ["self"] = $"{baseUrl}{path}?page={currentPage}&pageSize={pageSize}",
            ["first"] = $"{baseUrl}{path}?page=1&pageSize={pageSize}",
            ["last"] = $"{baseUrl}{path}?page={totalPages}&pageSize={pageSize}"
        };

        if (currentPage < totalPages)
        {
            links["next"] = $"{baseUrl}{path}?page={currentPage + 1}&pageSize={pageSize}";
        }

        if (currentPage > 1)
        {
            links["previous"] = $"{baseUrl}{path}?page={currentPage - 1}&pageSize={pageSize}";
        }

        return links;
    }
}
