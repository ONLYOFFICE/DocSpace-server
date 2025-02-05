﻿// (c) Copyright Ascensio System SIA 2009-2024
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


namespace ASC.Files.Core.Services.NotifyService;

public interface INotifyQueueManager<T>
{
    IRoomNotifyQueue<T> GetOrCreateRoomQueue(int tenantId, Folder<T> room, Guid currentAccountId);
    ConcurrentDictionary<string, IRoomNotifyQueue<T>> GetQueues();
}

public class RoomNotifyQueueManager<T> : INotifyQueueManager<T>
{
    private readonly ConcurrentDictionary<string, IRoomNotifyQueue<T>> _queues = new ConcurrentDictionary<string, IRoomNotifyQueue<T>>();

    private readonly NotifyClient _notifyClient;
    private readonly TenantManager _tenantManager;
    private readonly FileSecurity _fileSecurity;
    

    public RoomNotifyQueueManager(NotifyClient notifyClient, TenantManager tenantManager, FileSecurity fileSecurity)
    {
        _notifyClient = notifyClient;
        _tenantManager = tenantManager;
        _fileSecurity = fileSecurity;
    }

    public IRoomNotifyQueue<T> GetOrCreateRoomQueue(int tenantId, Folder<T> room, Guid currentAccountId)
    {
        return _queues.GetOrAdd(room.Id.ToString(), _ => {
            var roomNotifyQueue = new RoomNotifyQueue<T>(tenantId, room, _notifyClient, currentAccountId, _tenantManager, _fileSecurity);
            roomNotifyQueue.RegisterCallback(RemoveRoomQueue);
            return roomNotifyQueue;
        });
    }

    public void RemoveRoomQueue(string roomId)
    {
        _queues.TryRemove(roomId, out var roomNotifyQueue);
    }

    public ConcurrentDictionary<string, IRoomNotifyQueue<T>> GetQueues()
    {
        return _queues;
    }
}