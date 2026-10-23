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

/// <summary>
/// A socket event an integration module publishes, as JSON, to the <c>asc_plugin_events</c> exchange
/// (routing key <c>socket</c>). This is a public contract for modules in any language, separate from the
/// internal event bus. The module is the sender's RabbitMQ user, never a field of the event; clients receive
/// <see cref="Event"/> as <c>plugin:{module}:{Event}</c>.
/// </summary>
/// <remarks>
/// Exactly one addressee: <see cref="Room"/> reaches everyone in the portal subscribed to
/// <c>plugin-{module}-{Room}</c>; <see cref="User"/> (an e-mail, the identity shared through the single
/// sign-on) reaches only that user's sessions, through a room the socket service joins on its own.
/// </remarks>
public record PluginSocketEvent
{
    public int TenantId { get; init; }

    public string Room { get; init; }

    public string User { get; init; }

    public string Event { get; init; }

    public string Message { get; init; }
}
