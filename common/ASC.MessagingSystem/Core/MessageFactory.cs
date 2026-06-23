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

namespace ASC.MessagingSystem.Core;

[Scope]
public class MessageFactory(
    AuthContext authContext,
    TenantManager tenantManager,
    ILogger<MessageFactory> logger,
    IHttpContextAccessor httpContextAccessor)
{
    public EventMessage Create(HttpRequest request, string initiator, DateTime? dateTime, MessageAction action, MessageTarget target, IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        try
        {
            return new EventMessage
            {
                Ip = MessageSettings.GetIP(request),
                Initiator = initiator,
                Date = dateTime ?? DateTime.UtcNow,
                TenantId = tenantManager.GetCurrentTenantId(),
                UserId = authContext.CurrentAccount.ID,
                Page = MessageSettings.GetReferer(request) ?? MessageSettings.GetRequestPath(request),
                Action = action,
                Description = description?.Select(s => s ?? "").ToArray(),
                Target = target,
                UaHeader = MessageSettings.GetUAHeader(request),
                References = references
            };
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParseHttpRequest(action, ex);

            return null;
        }
    }

    public EventMessage Create(IDictionary<string, StringValues> headers, MessageAction action, MessageTarget target, IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        try
        {
            var message = new EventMessage
            {
                Date = DateTime.UtcNow,
                TenantId = tenantManager.GetCurrentTenantId(),
                UserId = authContext.CurrentAccount.ID,
                Action = action,
                Description = description?.Select(s => s ?? "").ToArray(),
                Target = target,
                References = references
            };

            if (headers == null)
            {
                return message;
            }

            var userAgent = MessageSettings.GetUAHeader(headers);
            var referer = MessageSettings.GetReferer(headers);

            message.Ip = httpContextAccessor?.HttpContext != null
                ? MessageSettings.GetIP(httpContextAccessor.HttpContext.Request)
                : MessageSettings.GetIP(headers);

            message.UaHeader = userAgent;
            message.Page = referer;

            return message;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParseHttpMessage(action, ex);

            return null;
        }
    }

    public EventMessage Create(HttpRequest request, MessageUserData userData, MessageAction action, string initiator, params string[] description)
    {
        try
        {
            var message = new EventMessage
            {
                Date = DateTime.UtcNow,
                TenantId = userData?.TenantId ?? tenantManager.GetCurrentTenantId(),
                UserId = userData?.UserId ?? authContext.CurrentAccount.ID,
                Action = action,
                Active = true,
                Initiator = initiator,
                Description = description?.Select(s => s ?? "").ToArray()
            };

            if (request != null)
            {
                var ip = MessageSettings.GetIP(request);
                var userAgent = MessageSettings.GetUAHeader(request);
                var referer = MessageSettings.GetReferer(request) ?? MessageSettings.GetRequestPath(request);

                message.Ip = ip;
                message.UaHeader = userAgent;
                message.Page = referer;
            }

            return message;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParseInitiatorMessage(action, ex);
            return null;
        }
    }
}