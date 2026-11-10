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

namespace ASC.Core.Notify.Socket;

[Scope]
public class PluginSocketManager(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration)
    : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    /// <param name="tenantId">The portal, already checked against the sender's allowed portals.</param>
    /// <param name="module">The sender, which namespaces the event and the room.</param>
    /// <param name="room">The portal room; null when the event is addressed to <paramref name="userId"/>.</param>
    /// <param name="userId">The addressee, resolved inside <paramref name="tenantId"/>; null for a room event.</param>
    /// <param name="eventName">The event name without the namespace.</param>
    /// <param name="message">Passed to the client as is.</param>
    public async Task SendAsync(int tenantId, string module, string room, Guid? userId, string eventName, string message)
    {
        // one source for the tenant: the room prefix and the tariff check both use the same tenantId
        await MakeRequest("plugin-event", new { tenantId, module, room, userId, @event = eventName, message }, tenantId);
    }
}
