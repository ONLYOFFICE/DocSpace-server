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

using ASC.Core.Common.Notify.Push.Dao;

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Core.Common.Notify;

public class PushSenderSink(INotifySender sender) : Sink
{
    private static readonly string _senderName = Constants.NotifyPushSenderSysName;
    private readonly INotifySender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override async Task<SendResponse> ProcessMessage(INoticeMessage message, IServiceScope scope)
    {
        try
        {

            var result = SendResult.OK;
            var m = await scope.ServiceProvider.GetRequiredService<PushSenderSinkMessageCreator>().CreateNotifyMessage(message, _senderName);
            if (string.IsNullOrEmpty(m.Reciever))
            {
                result = SendResult.IncorrectRecipient;
            }
            else
            {
                await _sender.SendAsync(m);
            }

            return new SendResponse(message, Constants.NotifyPushSenderSysName, result);
        }
        catch (Exception error)
        {
            return new SendResponse(message, Constants.NotifyPushSenderSysName, error);
        }
    }
}

[Scope]
public class PushSenderSinkMessageCreator(UserManager userManager, TenantManager tenantManager, CoreSettings coreSettings) : SinkMessageCreator
{
    public override async Task<NotifyMessage> CreateNotifyMessage(INoticeMessage message, string senderName)
    {
        var tenant = tenantManager.GetCurrentTenant(false);
        if (tenant == null)
        {
            await tenantManager.SetCurrentTenantAsync(Tenant.DefaultTenant);
            tenant = tenantManager.GetCurrentTenant(false);
        }

        var user = await userManager.GetUsersAsync(new Guid(message.Recipient.ID));
        var username = user.UserName;

        var fromTag = message.Arguments.FirstOrDefault(x => x.Tag.Equals("MessageFrom"));
        var productID = message.Arguments.FirstOrDefault(x => x.Tag.Equals("__ProductID"));
        var originalUrl = message.Arguments.FirstOrDefault(x => x.Tag.Equals("DocumentURL"));

        var folderId = message.Arguments.FirstOrDefault(x => x.Tag.Equals("FolderID"));
        var rootFolderId = message.Arguments.FirstOrDefault(x => x.Tag.Equals("FolderParentId"));
        var rootFolderType = message.Arguments.FirstOrDefault(x => x.Tag.Equals("FolderRootFolderType"));


        var notifyData = new NotifyData
        {
            Email = user.Email,
            Portal = tenant.GetTenantDomain(coreSettings),
            OriginalUrl = originalUrl is { Value: not null } ? originalUrl.Value.ToString() : "",
            Folder = new NotifyFolderData
            {
                Id = folderId is { Value: not null } ? folderId.Value.ToString() : "",
                ParentId = rootFolderId is { Value: not null } ? rootFolderId.Value.ToString() : "",
                RootFolderType = rootFolderType is { Value: not null } ? (int)rootFolderType.Value : 0
            }
        };

        var msg = (NoticeMessage)message;

        if (msg.ObjectID.StartsWith("file_"))
        {
            var documentTitle = message.Arguments.FirstOrDefault(x => x.Tag.Equals("DocumentTitle"));
            var documentExtension = message.Arguments.FirstOrDefault(x => x.Tag.Equals("DocumentExtension"));

            notifyData.File = new NotifyFileData
            {
                Id = msg.ObjectID[5..],
                Title = documentTitle is { Value: not null } ? documentTitle.Value.ToString() : "",
                Extension = documentExtension is { Value: not null } ? documentExtension.Value.ToString() : ""

            };
        }

        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonNotifyData = JsonSerializer.Serialize(notifyData, serializeOptions);

        var m = new NotifyMessage
        {
            TenantId = tenant.Id,
            Reciever = username,
            Subject = fromTag is { Value: not null } ? fromTag.Value.ToString() : message.Subject,
            ContentType = message.ContentType,
            Content = message.Body,
            Sender = Constants.NotifyPushSenderSysName,
            CreationDate = DateTime.UtcNow,
            ProductID = fromTag is { Value: not null } ? productID.Value.ToString() : null,
            Data = jsonNotifyData
        };


        return m;
    }
}