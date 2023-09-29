// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Singletone(Additional = typeof(DocumentBuilderTaskManagerHelperExtention))]
public class DocumentBuilderTaskManager
{
    private readonly object _synchRoot = new object();

    private readonly DistributedTaskQueue _queue;
    private readonly IServiceProvider _serviceProvider;

    public DocumentBuilderTaskManager(
        IDistributedTaskQueueFactory queueFactory,
        IServiceProvider serviceProvider)
    {
        _queue = queueFactory.CreateQueue(GetType());
        _serviceProvider = serviceProvider;
    }

    public DocumentBuilderTask<T> GetTask<T>(string taskId)
    {
        return _queue.PeekTask<DocumentBuilderTask<T>>(taskId);
    }

    public void TerminateTask<T>(string taskId)
    {
        var task = GetTask<T>(taskId);

        if (task != null)
        {
            _queue.DequeueTask(task.Id);
        }
    }

    private DocumentBuilderTask<T> StartTask<T>(DocumentBuilderTask<T> newTask)
    {
        lock (_synchRoot)
        {
            var task = GetTask<T>(newTask.Id);

            if (task != null && task.IsCompleted)
            {
                _queue.DequeueTask(task.Id);
                task = null;
            }

            if (task == null)
            {
                task = newTask;
                _queue.EnqueueTask(task);
            }

            return task;
        }
    }

    public DocumentBuilderTask<T> StartRoomIndexExport<T>(int tenantId, Guid userId, Folder<T> room)
    {
        var templateType = DocumentBuilderScriptHelper.TemplateType.RoomIndex;

        var data = new
        {
            Title = room.Title,
            Description = room.CreateOnString
        };

        var outputFileName = $"{room.Title}_index.xlsx";

        var (script, tempFileName) = DocumentBuilderScriptHelper.GetScript(templateType, data);

        var task = new DocumentBuilderTask<T>(_serviceProvider);

        task.Init(tenantId, userId, script, tempFileName, outputFileName);

        return StartTask<T>(task);
    }
}

public static class DocumentBuilderTaskManagerHelperExtention
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<DocumentBuilderTaskScope>();
    }
}