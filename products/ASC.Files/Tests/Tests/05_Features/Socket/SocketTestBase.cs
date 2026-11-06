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

namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// Shared by the suites that watch the real socket service through a <see cref="PortalSocket"/>: what the client
/// subscribes to and what it expects, in the terms of the contract with the frontend (<c>SocketEvents</c> in the
/// client's ui-kit).
/// </summary>
public abstract class SocketTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string ModifyFolder = "s:modify-folder";

    /// <summary>How much later a second recipient could get what the first one already has.</summary>
    protected static readonly TimeSpan _noDeliveryWindow = TimeSpan.FromSeconds(2);

    protected static string Dir(int folderId)
    {
        return $"DIR-{folderId}";
    }

    /// <summary>Connects as the user and subscribes to the folder rooms the way the client does, individual rooms too.</summary>
    protected async Task<PortalSocket> ConnectAndWatch(User user, params int[] folderIds)
    {
        var socket = await ConnectSocket(user);

        await socket.SubscribeAsync(true, TestContext.Current.CancellationToken, folderIds.Select(Dir).ToArray());

        return socket;
    }

    /// <summary>A file or folder change: <c>{ cmd, id, type, data }</c>.</summary>
    protected static Func<JsonElement, bool> Modified(string cmd, string type, int id)
    {
        return payload => payload.GetProperty("cmd").GetString() == cmd
                          && payload.GetProperty("type").GetString() == type
                          && payload.GetProperty("id").GetRawText() == id.ToString(CultureInfo.InvariantCulture);
    }

    protected static Task<SocketEvent> WaitForModified(PortalSocket socket, string cmd, string type, int id)
    {
        return socket.WaitForAsync(ModifyFolder, Modified(cmd, type, id), cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>The <c>data</c> of a file or folder change: the entry, serialized by the server into a string.</summary>
    protected static JsonDocument DataOf(SocketEvent modified)
    {
        return JsonDocument.Parse(modified.Payload.GetProperty("data").GetString()!);
    }
}
