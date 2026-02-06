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

namespace ASC.AI.Core.Chat.Deletion;

[Transient]
public class ChatDeletionTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory = null!;
    private int _tenantId;
    private Guid _userId;
    private Guid _chatId;

    public ChatDeletionTask() { }

    public ChatDeletionTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid userId, Guid chatId)
    {
        _tenantId = tenantId;
        _userId = userId;
        _chatId = chatId;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatDeletionTask>>();

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<int>();
            var chatDao = scope.ServiceProvider.GetRequiredService<ChatDao>();

            await foreach (var fileId in chatDao.GetAttachmentFileIdsAsync(_tenantId, _chatId))
            {
                try
                {
                    await fileDao.DeleteFileAsync(fileId);
                }
                catch (Exception ex)
                {
                    logger.WarningWithException(ex);
                }
            }

            await chatDao.HardDeleteChatAsync(_tenantId, _chatId, _userId);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }
}
