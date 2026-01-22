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
// the GNU AGPL at: http://creativecommons.org/licenses/agpl-3.0/legalcode

namespace ASC.People.Mappers;

using ASC.People.ApiModels.RequestDto;
using ASC.People.ApiModels.V3.RequestDto.Groups;
using ASC.People.ApiModels.V3.ResponseDto.Groups;
using ASC.Api.Core.Model;

/// <summary>
/// Mapper for converting between v2 and v3 Group DTOs.
/// </summary>
[Scope]
public class GroupDtoMapperV3
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GroupDtoMapperV3(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Converts v3 CreateGroupRequest to v2 GroupRequest.
    /// </summary>
    public GroupRequestDto ToV2CreateRequest(CreateGroupRequestDtoV3 v3Dto)
    {
        return new GroupRequestDto
        {
            GroupName = v3Dto.Name,
            GroupManager = v3Dto.ManagerId ?? Guid.Empty,
            Members = v3Dto.MemberIds?.ToArray()
        };
    }

    /// <summary>
    /// Converts v3 UpdateGroupRequest to v2 UpdateGroupRequest.
    /// </summary>
    public UpdateGroupRequest ToV2UpdateRequest(UpdateGroupRequestDtoV3 v3Dto)
    {
        return new UpdateGroupRequest
        {
            GroupName = v3Dto.Name,
            GroupManager = v3Dto.ManagerId ?? Guid.Empty,
            MembersToAdd = v3Dto.MembersToAdd?.ToArray(),
            MembersToRemove = v3Dto.MembersToRemove?.ToArray()
        };
    }

    /// <summary>
    /// Converts domain GroupDto to v3 GroupResponse.
    /// </summary>
    public GroupResponseDtoV3 FromDomain(GroupDto domain)
    {
        var baseUrl = GetBaseUrl();

        return new GroupResponseDtoV3
        {
            Id = domain.Id,
            Name = domain.Name,
            Manager = domain.Manager != null ? new UserSummaryDtoV3
            {
                Id = domain.Manager.Id,
                DisplayName = domain.Manager.DisplayName,
                Email = domain.Manager.Email
            } : null,
            MembersCount = domain.MembersCount,
            Members = domain.Members?.Select(m => new UserSummaryDtoV3
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Email = m.Email
            }),
            Links = new Dictionary<string, string>
            {
                ["self"] = $"{baseUrl}/api/3.0/groups/{domain.Id}",
                ["members"] = $"{baseUrl}/api/3.0/groups/{domain.Id}/members",
                ["manager"] = $"{baseUrl}/api/3.0/groups/{domain.Id}/manager"
            }
        };
    }

    /// <summary>
    /// Converts a list of domain groups to v3 paginated response.
    /// </summary>
    public GroupListResponseDtoV3 FromDomainList(
        IEnumerable<GroupDto> groups,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var groupDtos = groups.Select(g => FromDomain(g)).ToList();
        var baseUrl = GetBaseUrl();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new GroupListResponseDtoV3
        {
            Items = groupDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1,
            Links = BuildPaginationLinks(baseUrl, "/api/3.0/groups", pageNumber, pageSize, totalPages)
        };
    }

    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return string.Empty;
        }

        return $"{request.Scheme}://{request.Host}";
    }

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
