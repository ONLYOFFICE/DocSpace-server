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

namespace ASC.Notify;

[Singleton]
public sealed class Context : INotifyRegistry
{
    public const string SysRecipient = "_#" + SysRecipientId + "#_";
    internal const string SysRecipientId = "SYS_RECIPIENT_ID";
    internal const string SysRecipientName = "SYS_RECIPIENT_NAME";
    internal const string SysRecipientAddress = "SYS_RECIPIENT_ADDRESS";

    private readonly Dictionary<string, ISenderChannel> _channels = new(2);

    public void RegisterSender(DispatchEngine dispatchEngine, string senderName, ISink senderSink)
    {
        lock (_channels)
        {
            _channels[senderName] = new SenderChannel(dispatchEngine, senderName, null, senderSink);
        }
    }

    public void UnregisterSender(string senderName)
    {
        lock (_channels)
        {
            _channels.Remove(senderName);
        }
    }

    public ISenderChannel GetSender(string senderName)
    {
        lock (_channels)
        {
            _channels.TryGetValue(senderName, out var channel);

            return channel;
        }
    }
}