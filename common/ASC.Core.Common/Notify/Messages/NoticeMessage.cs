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

public class NoticeMessage : INoticeMessage
{
    [NonSerialized]
    private readonly List<ITagValue> _arguments = [];

    public NoticeMessage() { }

    public NoticeMessage(IDirectRecipient recipient, INotifyAction action, string objectID)
    {
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        Action = action;
        ObjectID = objectID;
    }

    public NoticeMessage(IDirectRecipient recipient, INotifyAction action, string objectID, IPattern pattern)
    {
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        Action = action;
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        ObjectID = objectID;
        ContentType = pattern.ContentType;
    }

    public NoticeMessage(IDirectRecipient recipient, string subject, string body, string contentType)
    {
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        Subject = subject;
        Body = body ?? throw new ArgumentNullException(nameof(body));
        ContentType = contentType;
    }

    public string ObjectID { get; private set; }

    public IDirectRecipient Recipient { get; private set; }

    [field: NonSerialized]
    public IPattern Pattern { get; internal set; }

    public INotifyAction Action { get; private set; }

    public ITagValue[] Arguments => _arguments.ToArray();

    public void AddArgument(params ITagValue[] tagValues)
    {
        ArgumentNullException.ThrowIfNull(tagValues);

        Array.ForEach(tagValues,
            tagValue =>
            {
                if (!_arguments.Exists(tv => Equals(tv.Tag, tagValue.Tag)))
                {
                    _arguments.Add(tagValue);
                }
            });
    }

    public ITagValue GetArgument(string tag)
    {
        return _arguments.Find(r => r.Tag == tag);
    }

    public string Subject { get; set; }
    public string Body { get; set; }
    public string ContentType { get; internal set; }
}