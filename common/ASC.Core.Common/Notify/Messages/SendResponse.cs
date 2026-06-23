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

namespace ASC.Notify.Messages;

public class SendResponse
{
    public SendResponse()
    {
        Result = SendResult.OK;
    }

    public SendResponse(INotifyAction action, IRecipient recipient, Exception exc)
    {
        Result = SendResult.Impossible;
        Exception = exc;
        Recipient = recipient;
        NotifyAction = action;
    }

    public SendResponse(INotifyAction action, string senderName, IRecipient recipient, Exception exc)
    {
        Result = SendResult.Impossible;
        SenderName = senderName;
        Exception = exc;
        Recipient = recipient;
        NotifyAction = action;
    }

    public SendResponse(INotifyAction action, string senderName, IRecipient recipient, SendResult sendResult)
    {
        SenderName = senderName;
        Recipient = recipient;
        Result = sendResult;
        NotifyAction = action;
    }

    public SendResponse(INoticeMessage message, string sender, SendResult result)
    {
        NoticeMessage = message;
        SenderName = sender;
        Result = result;
        if (message != null)
        {
            Recipient = message.Recipient;
            NotifyAction = message.Action;
        }
    }

    public SendResponse(INoticeMessage message, string sender, Exception exc)
    {
        NoticeMessage = message;
        SenderName = sender;
        Result = SendResult.Impossible;
        Exception = exc;
        if (message != null)
        {
            Recipient = message.Recipient;
            NotifyAction = message.Action;
        }
    }

    public INoticeMessage NoticeMessage { get; internal set; }
    public INotifyAction NotifyAction { get; internal set; }
    public SendResult Result { get; set; }
    public Exception Exception { get; init; }
    public string SenderName { get; internal set; }
    public IRecipient Recipient { get; internal set; }
}