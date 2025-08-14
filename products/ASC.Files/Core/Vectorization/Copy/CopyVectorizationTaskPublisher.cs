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

namespace ASC.Files.Core.Vectorization.Copy;

[Scope]
public class CopyVectorizationTaskPublisher(
    TenantManager tenantManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    IDaoFactory daoFactory,
    VectorizationSettings vectorizationSettings,
    VectorizationTaskService<CopyVectorizationTask, CopyVectorizationTaskData> copyVectorizationTaskService)
{
    public async Task<CopyVectorizationTask> PublishAsync(int knowledgeFolderId, IEnumerable<JsonElement> files)
    {
        if (knowledgeFolderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(knowledgeFolderId), @"Knowledge folder id must be greater than 0");
        }
        
        var (fileIds, thirdPartyFileIds) = FileOperationsManager.GetIds(files);
        if (fileIds.Count == 0 && thirdPartyFileIds.Count == 0)
        {
            throw new ArgumentException(@"Files must not be empty", nameof(files));
        }
        
        await CheckFilesAsync(fileIds);
        await CheckFilesAsync(thirdPartyFileIds);
        
        var task = serviceProvider.GetRequiredService<CopyVectorizationTask>();

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var data = new CopyVectorizationTaskData
        {
            KnowledgeFolderId = knowledgeFolderId,
            FileIds = fileIds,
            ThirdPartyFileIds = thirdPartyFileIds
        };
        
        task.Init(tenantId, userId, data);

        var taskId= await copyVectorizationTaskService.StoreAsync(task);

        await eventBus.PublishAsync(new CopyVectorizationIntegrationEvent(userId, tenantId) 
        { 
            TaskId = taskId, 
            Data = data 
        });
        
        return (await copyVectorizationTaskService.GetAsync(taskId))!;
    }

    private async Task CheckFilesAsync<T>(IEnumerable<T> files)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        await foreach (var file in fileDao.GetFilesAsync(files))
        {
            if (file.ContentLength > vectorizationSettings.MaxContentLength)
            {
                throw FileSizeComment.GetFileSizeException(vectorizationSettings.MaxContentLength);
            }
            
            if (!vectorizationSettings.SupportedFormats.Contains(FileUtility.GetFileExtension(file.Title)))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_NotSupportedFormat);
            }
        }
    }
}