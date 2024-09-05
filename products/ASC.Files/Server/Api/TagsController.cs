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

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class TagsControllerInternal(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : TagsController<int>(fileStorageService, entryManager, folderDtoHelper, fileDtoHelper);

public class TagsControllerThirdparty(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : TagsController<string>(fileStorageService, entryManager, folderDtoHelper, fileDtoHelper);

public abstract class TagsController<T>(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Adds a file with the ID specified in the request to the "Recent" section.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <short>Add a file to the "Recent" section</short>
    /// <path>api/2.0/files/file/{fileId}/recent</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("file/{fileId}/recent")]
    public async Task<FileDto<T>> AddToRecentAsync(T fileId)
    {
        var file = await fileStorageService.GetFileAsync(fileId, -1).NotFoundIfNull("File not found");

        await entryManager.MarkAsRecent(file);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Changes the favorite status of the file with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Boolean, System" name="favorite" example="true">Specifies if this file is marked as favorite or not</param>
    /// <short>Change the file favorite status</short>
    /// <path>api/2.0/files/favorites/{fileId}</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true - the file is favorite, false - the file is not favorite", typeof(bool))]
    [HttpGet("favorites/{fileId}")]
    public async Task<bool> ToggleFileFavoriteAsync(T fileId, bool favorite)
    {
        return await fileStorageService.ToggleFileFavoriteAsync(fileId, favorite);
    }
}

public class TagsControllerCommon(FileStorageService fileStorageService,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Adds files and folders with the IDs specified in the request to the favorite list.
    /// </summary>
    /// <short>Add favorite files and folders</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <path>api/2.0/files/favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPost("favorites")]
    public async Task<bool> AddFavoritesAsync(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        await fileStorageService.AddToFavoritesAsync(folderIntIds, fileIntIds);
        await fileStorageService.AddToFavoritesAsync(folderStringIds, fileStringIds);

        return true;
    }

    /// <summary>
    /// Adds files with the IDs specified in the request to the template list.
    /// </summary>
    /// <short>Add template files</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.TemplatesRequestDto, ASC.Files.Core" name="inDto">Request parameters for adding files to the template list</param>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPost("templates")]
    public async Task<bool> AddTemplatesAsync(TemplatesRequestDto inDto)
    {
        await fileStorageService.AddToTemplatesAsync(inDto.FileIds);

        return true;
    }

    /// <summary>
    /// Removes files and folders with the IDs specified in the request from the favorite list. This method uses the body parameters.
    /// </summary>
    /// <short>Delete favorite files and folders (using body parameters)</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <path>api/2.0/files/favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpDelete("favorites")]
    [Consumes("application/json")]
    public async Task<bool> DeleteFavoritesFromBodyAsync([FromBody] BaseBatchRequestDto inDto)
    {
        return await DeleteFavoritesAsync(inDto);
    }

    /// <summary>
    /// Removes files and folders with the IDs specified in the request from the favorite list. This method uses the query parameters.
    /// </summary>
    /// <short>Delete favorite files and folders (using query parameters)</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <path>api/2.0/files/favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpDelete("favorites")]
    public async Task<bool> DeleteFavoritesFromQueryAsync([FromQuery][ModelBinder(BinderType = typeof(BaseBatchModelBinder))] BaseBatchRequestDto inDto)
    {
        return await DeleteFavoritesAsync(inDto);
    }

    /// <summary>
    /// Removes files with the IDs specified in the request from the template list.
    /// </summary>
    /// <short>Delete template files</short>
    /// <param type="System.Collections.Generic.IEnumerable{System.Int32}, System.Collections.Generic" name="fileIds" example="1234">List of file IDs</param>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpDelete("templates")]
    public async Task<bool> DeleteTemplatesAsync(IEnumerable<int> fileIds)
    {
        await fileStorageService.DeleteTemplatesAsync(fileIds);

        return true;
    }

    /// <summary>
    /// Removes files with the IDs specified in the request from the "Recent" section.
    /// </summary>
    /// <short>Delete recent files</short>
    /// <path>api/2.0/files/recent</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "No content", typeof(NoContentResult))]
    [HttpDelete("recent")]
    public async Task<NoContentResult> DeleteRecentAsync(FileBaseBatchRequestDto inDto)
    {
        var (fileIntIds, _) = FileOperationsManager.GetIds(inDto.FileIds);
        
        await fileStorageService.DeleteFromRecentAsync(fileIntIds, true);

        return NoContent();
    }

    private async Task<bool> DeleteFavoritesAsync(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        await fileStorageService.DeleteFavoritesAsync(folderIntIds, fileIntIds);
        await fileStorageService.DeleteFavoritesAsync(folderStringIds, fileStringIds);

        return true;
    }
}