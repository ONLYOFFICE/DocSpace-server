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

namespace ASC.Files.Core.Services.NotifyService;

public interface IRoomNotifyQueue<T>
{
    void AddMessage(File<T> file);
    Task ProcessQueueAsync();
    void RegisterCallback(Action<string> callback);
}

public class RoomNotifyQueue<T> : IRoomNotifyQueue<T>, IDisposable
{
    protected readonly TenantManager _tenantManager;
    private readonly NotifyClient _notifyClient;
    private readonly FileSecurity _fileSecurity;
    private Timer _timer;

    protected readonly int _tenantId;
    private readonly Folder<T> _room;
    protected readonly Guid _currentAccountId;

    private readonly Queue<File<T>> _messages = new Queue<File<T>>();

    private Action<string> _removeCallback;
    public RoomNotifyQueue(int tenantId, Folder<T> room, NotifyClient notifyClient, Guid currentAccountId, TenantManager tenantManager, FileSecurity fileSecurity)
    {
        _room = room;
        _notifyClient = notifyClient;
        _currentAccountId = currentAccountId;
        _tenantManager = tenantManager;
        _tenantId = tenantId;
        _fileSecurity = fileSecurity;
    }

    public void RegisterCallback(Action<string> callback)
    {
        _removeCallback = callback;
    }

    public void AddMessage(File<T> file)
    {
        _messages.Enqueue(file);

        if (_timer == null)
        {
            _timer = new Timer(_ => ProcessQueueAsync().Wait(), null, TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        }
    }

    public async Task ProcessQueueAsync()
    {
        await _tenantManager.SetCurrentTenantAsync(_tenantId);
        var count = _messages.Count;
        if (count == 0)
        {
            return;
        }

        var whoCanRead = await _fileSecurity.WhoCanReadAsync(_room, true);

        if (count <= 3)
        {
            while (_messages.Count > 0)
            {
                await _notifyClient.SendDocumentUploadedToRoom(whoCanRead, _messages.Dequeue(), _room, _currentAccountId);
            }
        }
        else
        {
            await _notifyClient.SendDocumentsUploadedToRoom(whoCanRead, count, _room, _currentAccountId);
            _messages.Clear();
        }

        _removeCallback?.Invoke(_room.Id.ToString());
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
