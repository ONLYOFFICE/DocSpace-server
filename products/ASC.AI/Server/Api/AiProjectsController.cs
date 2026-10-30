// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.AI.Api;

[Scope]
[ApiEndpoint("ai", Internal = true)]
[AiFeature]
public class AiProjectsController(
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper,
    FolderContentDtoHelper folderContentDtoHelper,
    FolderDtoHelper folderDtoHelper)
    : ControllerBase
{
    /// <remarks>
    /// Returns the list of AI projects.
    /// </remarks>
    /// <summary>Get AI projects</summary>
    /// <path>api/2.0/ai/projects</path>
    [Tags("AI / Projects")]
    [SwaggerResponse(200, "List of AI projects", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or the `startIndex` is negative")]
    [SwaggerResponse(500, "`tags` is not a JSON array of strings")]
    [HttpGet("projects")]
    public async Task<FolderContentDto<int>> GetProjects(GetAiProjectListRequestDto inDto)
    {
        var parentId = await globalFolderHelper.GetFolderAiProjectsAsync();

        HashSet<FilterType> filter = [FilterType.AiProjectRooms];

        var tagNames = !string.IsNullOrEmpty(inDto.Tags)
            ? JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags)
            : null;

        OrderBy? orderBy = null;
        if (SortedByTypeExtensions.TryParse(inDto.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, inDto.SortOrder == SortOrder.Ascending);
        }

        var content = await fileStorageService.GetFolderItemsAsync(
            parentId,
            inDto.StartIndex,
            inDto.Count,
            filter,
            false,
            inDto.SubjectId,
            Guid.Empty,
            inDto.Text,
            [],
            true,
            false,
            orderBy,
            SearchArea.AiProjects,
            0,
            inDto.WithoutTags ?? false,
            tagNames,
            inDto.ExcludeSubject ?? false,
            ProviderFilter.None,
            inDto.SubjectOwnerId,
            quotaFilter: QuotaFilter.All,
            storageFilter: StorageFilter.None);

        var dto = await folderContentDtoHelper.GetAsync(parentId, content, inDto.StartIndex, inDto.Text);

        return dto.NotFoundIfNull();
    }

    /// <remarks>
    /// Creates an AI project.
    /// </remarks>
    /// <summary>Create an AI project</summary>
    /// <path>api/2.0/ai/projects</path>
    [Tags("AI / Projects")]
    [SwaggerResponse(200, "Created project", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, `title` is blank or longer than 170 characters, `color` is longer than 6 characters, `cover` is longer than 50 characters or not a known cover, `logo` has no `tmpFile`, a position outside 0-1280, a size outside 1-1280 or a position outside the uploaded picture, or a tag name is empty")]
    [SwaggerResponse(402, "The logo does not fit into the portal storage quota")]
    [SwaggerResponse(403, "The caller is a user or a guest, or `logo.tmpFile` is not a picture the caller uploaded")]
    [SwaggerResponse(404, "`logo.tmpFile` names no uploaded picture, or one already used")]
    [SwaggerResponse(500, "`logo.width` or `logo.height` is larger than 2147483647")]
    [HttpPost("projects")]
    public async Task<FolderDto<int>> CreateProject(CreateAiProjectRequestDto inDto)
    {
        var project = await fileStorageService.CreateAiProjectAsync(inDto.Title, inDto.Color, inDto.Cover, inDto.Tags, inDto.Logo);

        return await folderDtoHelper.GetAsync(project);
    }
}
