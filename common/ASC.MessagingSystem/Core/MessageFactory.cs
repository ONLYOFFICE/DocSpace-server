// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.MessagingSystem.Core;

[Scope]
public class MessageFactory(AuthContext authContext,
        TenantManager tenantManager,
        ILogger<MessageFactory> logger,
        IHttpContextAccessor httpContextAccessor)
    {
    public async Task<EventMessage> CreateAsync(HttpRequest request, string initiator, DateTime? dateTime, MessageAction action, MessageTarget target,
        IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        try
        {
            return new EventMessage
            {
                Ip = MessageSettings.GetIP(request),
                Initiator = initiator,
                Date = dateTime ?? DateTime.UtcNow,
                TenantId = await tenantManager.GetCurrentTenantIdAsync(),
                UserId = authContext.CurrentAccount.ID,
                Page = MessageSettings.GetReferer(request),
                Action = action,
                Description = description,
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

    public async Task<EventMessage> CreateAsync(IDictionary<string, StringValues> headers, MessageAction action, MessageTarget target, 
        IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        try
        {
            var message = new EventMessage
            {
                Date = DateTime.UtcNow,
                TenantId = await tenantManager.GetCurrentTenantIdAsync(),
                UserId = authContext.CurrentAccount.ID,
                Action = action,
                Description = description,
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

    public async Task<EventMessage> CreateAsync(string initiator, MessageAction action, MessageTarget target, params string[] description)
    {
        try
        {
            return new EventMessage
            {
                Initiator = initiator,
                Date = DateTime.UtcNow,
                TenantId = await tenantManager.GetCurrentTenantIdAsync(),
                Action = action,
                Description = description,
                Target = target,
            };
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParseInitiatorMessage(action, ex);

            return null;
        }
    }

    public async Task<EventMessage> CreateAsync(HttpRequest request, MessageUserData userData, MessageAction action)
    {
        try
        {
            var message = new EventMessage
            {
                Date = DateTime.UtcNow,
                TenantId = userData?.TenantId ?? await tenantManager.GetCurrentTenantIdAsync(),
                UserId = userData?.UserId ?? authContext.CurrentAccount.ID,
                Action = action,
                Active = true
            };

            if (request != null)
            {
                var ip = MessageSettings.GetIP(request);
                var userAgent = MessageSettings.GetUAHeader(request);
                var referer = MessageSettings.GetReferer(request);

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
