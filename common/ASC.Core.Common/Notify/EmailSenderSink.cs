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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Core.Notify;

public class EmailSenderSink(INotifySender sender) : Sink
{
    private static readonly string _senderName = Constants.NotifyEMailSenderSysName;
    private readonly INotifySender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override async Task<SendResponse> ProcessMessage(INoticeMessage message, IServiceScope serviceScope)
    {
        if (message.Recipient.Addresses == null || message.Recipient.Addresses.Length == 0)
        {
            return new SendResponse(message, _senderName, SendResult.IncorrectRecipient);
        }

        var response = new SendResponse(message, _senderName, default(SendResult));
        try
        {
            var m = await serviceScope.ServiceProvider.GetRequiredService<EmailSenderSinkMessageCreator>().CreateNotifyMessage(message, _senderName);
            var result = await _sender.SendAsync(m);

            response.Result = result switch
            {
                NoticeSendResult.TryOnceAgain => SendResult.Inprogress,
                NoticeSendResult.MessageIncorrect => SendResult.IncorrectRecipient,
                NoticeSendResult.SendingImpossible => SendResult.Impossible,
                _ => SendResult.OK
            };

            return response;
        }
        catch (Exception e)
        {
            return new SendResponse(message, _senderName, e);
        }
    }
}

[Scope]
public class EmailSenderSinkMessageCreator(TenantManager tenantManager, CoreConfiguration coreConfiguration,
        ILoggerProvider options)
    : SinkMessageCreator
{
    private readonly ILogger _logger = options.CreateLogger("ASC.Notify");

    public override async Task<NotifyMessage> CreateNotifyMessage(INoticeMessage message, string senderName)
    {
        var m = new NotifyMessage
        {
            Subject = message.Subject.Trim(' ', '\t', '\n', '\r'),
            ContentType = message.ContentType,
            Content = message.Body,
            SenderType = senderName,
            CreationDate = DateTime.UtcNow
        };

        var tenant = tenantManager.GetCurrentTenant(false);
        m.TenantId = tenant?.Id ?? Tenant.DefaultTenant;

        var settings = await coreConfiguration.GetDefaultSmtpSettingsAsync();
        var from = MailAddressUtils.Create(settings.SenderAddress, settings.SenderDisplayName);
        var fromTag = message.Arguments.FirstOrDefault(x => x.Tag.Equals("MessageFrom"));
        if ((settings.IsDefaultSettings || string.IsNullOrEmpty(settings.SenderDisplayName)) &&
            fromTag is { Value: not null })
        {
            try
            {
                from = MailAddressUtils.Create(from.Address, fromTag.Value.ToString());
            }

            catch { }
        }
        m.Sender = from.ToString();
        var to = message.Recipient.Addresses.Select(address => MailAddressUtils.Create(address, message.Recipient.Name).ToString()).ToArray();
        m.Reciever = string.Join("|", to);

        var replyTag = message.Arguments.FirstOrDefault(x => x.Tag == "replyto");
        if (replyTag is { Value: string replyTagValue })
        {
            try
            {
                m.ReplyTo = MailAddressUtils.Create(replyTagValue).ToString();
            }
            catch (Exception e)
            {
                _logger.ErrorCreatingTag(replyTagValue, e);
            }
        }

        var priority = message.Arguments.FirstOrDefault(a => a.Tag == "Priority");
        if (priority != null)
        {
            m.Priority = Convert.ToInt32(priority.Value);
        }

        var attachmentTag = message.Arguments.FirstOrDefault(x => x.Tag == "EmbeddedAttachments");
        if (attachmentTag is { Value: not null })
        {
            m.Attachments = attachmentTag.Value as NotifyMessageAttachment[];
        }

        var autoSubmittedTag = message.Arguments.FirstOrDefault(x => x.Tag == "AutoSubmitted");
        if (autoSubmittedTag is { Value: string autoSubmittedTagValue })
        {
            m.AutoSubmitted = autoSubmittedTagValue;
        }

        return m;
    }
}