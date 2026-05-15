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

namespace ASC.Files.Core.Services.NotifyService;

public interface IRoomNotifyQueue<T>
{
    void AddMessage(File<T> file);
    Task ProcessQueueAsync();
    void RegisterCallback(Action<string> callback);
}

public class RoomNotifyQueue<T>(int tenantId, Folder<T> room, NotifyClient notifyClient, Guid currentAccountId, TenantManager tenantManager, FileSecurity fileSecurity)
    : IRoomNotifyQueue<T>
{
    private readonly Queue<File<T>> _messages = new();

    private Action<string> _removeCallback;

    public void RegisterCallback(Action<string> callback)
    {
        _removeCallback = callback;
    }

    public void AddMessage(File<T> file)
    {
        _messages.Enqueue(file);
    }

    public async Task ProcessQueueAsync()
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);
        var count = _messages.Count;
        if (count == 0)
        {
            return;
        }
        var userIDs = (await fileSecurity.WhoCanReadAsync(room, true)).ToList();

        if (room.CreateBy != currentAccountId)
        {
            userIDs.Add(room.CreateBy);
        }

        if (count <= 3)
        {
            while (_messages.Count > 0)
            {
                await notifyClient.SendDocumentUploadedToRoom(userIDs, _messages.Dequeue(), room, currentAccountId);
            }
        }
        else
        {
            await notifyClient.SendDocumentsUploadedToRoom(userIDs, count, room, currentAccountId);
            _messages.Clear();
        }

        _removeCallback?.Invoke(room.Id.ToString());
    }
}
