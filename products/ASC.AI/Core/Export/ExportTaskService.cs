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

#nullable enable
namespace ASC.AI.Core.Export;

[Singleton(GenericArguments = [typeof(MessageExportTask), typeof(MessageExportTaskData)])]
[Singleton(GenericArguments = [typeof(ChatExportTask), typeof(ChatExportTaskData)])]
public class ExportTaskService<T, TData>(
    IDistributedTaskQueueFactory queueFactory) 
    where T : ExportTask<TData> 
    where TData : ExportTaskData
{
    private readonly DistributedTaskQueue<T> _queue = queueFactory.CreateQueue<T>();

    public Task StartAsync(T task)
    {
        return _queue.EnqueueTask(task);
    }

    public Task<string> StoreAsync(T task)
    {
        return _queue.PublishTask(task);
    }

    public async Task<T?> GetAsync(string id)
    {
        return await _queue.PeekTask(id);
    }

    public async Task<List<T>> GetTasksAsync()
    {
        return await _queue.GetAllTasks();
    }
    
    public async Task DeleteAsync(string id)
    {
        await _queue.DequeueTask(id);
    }
}