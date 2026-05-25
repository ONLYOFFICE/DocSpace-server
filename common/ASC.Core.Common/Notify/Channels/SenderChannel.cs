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

namespace ASC.Notify.Channels;

public class SenderChannel : ISenderChannel
{
    private readonly ISink _firstSink;
    private readonly ISink _senderSink;

    public string SenderName { get; private set; }


    public SenderChannel(DispatchEngine dispatchEngine, string senderName, ISink decorateSink, ISink senderSink)
    {
        SenderName = senderName ?? throw new ArgumentNullException(nameof(senderName));
        _firstSink = decorateSink;
        _senderSink = senderSink ?? throw new ApplicationException($"channel with tag {senderName} not created sender sink");


        var dispatcherSink = new DispatchSink(SenderName, dispatchEngine);
        _firstSink = AddSink(_firstSink, dispatcherSink);
    }

    public async Task SendAsync(INoticeMessage message, IServiceScope serviceScope)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _firstSink.ProcessMessageAsync(message, serviceScope);
    }

    public async Task<SendResponse> DirectSend(INoticeMessage message, IServiceScope serviceScope)
    {
        return await _senderSink.ProcessMessage(message, serviceScope);
    }

    private ISink AddSink(ISink firstSink, ISink addedSink)
    {
        if (firstSink == null)
        {
            return addedSink;
        }

        if (addedSink == null)
        {
            return firstSink;
        }

        var current = firstSink;
        while (current.NextSink != null)
        {
            current = current.NextSink;
        }
        current.NextSink = addedSink;

        return firstSink;
    }
}