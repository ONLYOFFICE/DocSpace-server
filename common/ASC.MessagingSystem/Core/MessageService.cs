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
public class MessageService(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    MessageFactory messageFactory,
    DbMessageSender sender,
    MessagePolicy messagePolicy,
    ILogger<MessageService> logger)
{
    private bool? _enabled;
    private DbMessageSender Sender
    {
        get
        {
            _enabled ??= configuration["messaging:enabled"] == "true";
            return _enabled.Value ? sender : null;
        }
    }
    
    private HttpRequest Request => httpContextAccessor?.HttpContext?.Request;

    private static readonly JsonSerializerOptions _serializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    #region HttpRequest

    public async Task SendAsync(MessageAction action)
    {
        await SendRequestMessageAsync(action);
    }

    public async Task SendAsync(MessageAction action, string d1)
    {
        await SendRequestMessageAsync(action, description: d1);
    }

    public async Task SendAsync(MessageAction action, string d1, IEnumerable<string> d2)
    {
        await SendRequestMessageAsync(action, description: [d1, string.Join(", ", d2)]);
    }

    public async Task SendAsync(string loginName, MessageAction action)
    {
        await SendRequestMessageAsync(action, loginName: loginName);
    }

    #endregion

    #region HttpRequest & Target

    public async Task SendAsync(MessageAction action, MessageTarget target)
    {
        await SendRequestMessageAsync(action, target);
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, DateTime? dateTime, string d1)
    {
        await SendRequestMessageAsync(action, target, dateTime: dateTime, description: d1);
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, string d1)
    {
        await SendRequestMessageAsync(action, target, description: d1);
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, string d1, Guid userId)
    {
        if (TryAddNotificationParam(action, userId, out var parametr))
        {
            await SendRequestMessageAsync(action, target, description: [d1, parametr]);
        }
        else
        {
            await SendRequestMessageAsync(action, target, description: d1);
        }
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, string d1, string d2, IEnumerable<FilesAuditReference> references = null, DateTime? dateTime = null)
    {
        await SendRequestMessageAsync(action, target, description: [d1, d2], references: references, dateTime: dateTime);
    }
    
    public async Task SendAsync(MessageAction action, MessageTarget target, string[] description, IEnumerable<FilesAuditReference> references = null)
    {
        await SendRequestMessageAsync(action, target, description: description, references: references);
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, IEnumerable<string> d1)
    {
        await SendRequestMessageAsync(action, target, description: string.Join(", ", d1));
    }

    public async Task SendAsync(MessageAction action, MessageTarget target, IEnumerable<string> d1, List<Guid> userIds, EmployeeType userType)
    {
        if (TryAddNotificationParam(action, userIds, out var parametr, userType))
        {
            await SendRequestMessageAsync(action, target, description: [string.Join(", ", d1), parametr]);
        }
        else
        {
            await SendRequestMessageAsync(action, target, description: string.Join(", ", d1));
        }
    }

    public async Task SendAsync(string loginName, MessageAction action, MessageTarget target)
    {
        await SendRequestMessageAsync(action, target, loginName);
    }

    #endregion

    private async Task SendRequestMessageAsync(MessageAction action, MessageTarget target = null, string loginName = null, DateTime? dateTime = null, 
        IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        if (Sender == null)
        {
            return;
        }

        if (Request == null)
        {
            logger.DebugEmptyHttpRequest(action);

            return;
        }

        var message = await messageFactory.CreateAsync(Request, loginName, dateTime, action, target, references, description);
        if (!messagePolicy.Check(message))
        {
            return;
        }

        _ = Sender.SendAsync(message);
    }

    #region HttpHeaders

    public async Task SendHeadersMessageAsync(MessageAction action)
    {
        await SendRequestHeadersMessageAsync(action);
    }
    
    public async Task SendHeadersMessageAsync(MessageAction action, params string[] description)
    {
        await SendRequestHeadersMessageAsync(action, description: description);
    }

    public async Task SendHeadersMessageAsync(MessageAction action, MessageTarget target, IDictionary<string, StringValues> httpHeaders, string d1)
    {
        await SendRequestHeadersMessageAsync(action, target, httpHeaders, null, d1);
    }

    public async Task SendHeadersMessageAsync(MessageAction action, MessageTarget target, IDictionary<string, StringValues> httpHeaders, IEnumerable<string> d1, 
        IEnumerable<FilesAuditReference> references = null)
    {
        await SendRequestHeadersMessageAsync(action, target, httpHeaders, references, d1?.ToArray());
    }

    private async Task SendRequestHeadersMessageAsync(MessageAction action, MessageTarget target = null, IDictionary<string, StringValues> httpHeaders = null, 
        IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        if (Sender == null)
        {
            return;
        }

        if (httpHeaders == null && Request != null)
        {
            httpHeaders = Request.Headers.ToDictionary(k => k.Key, v => v.Value);
        }

        var message = await messageFactory.CreateAsync(httpHeaders, action, target, references, description);
        if (!messagePolicy.Check(message))
        {
            return;
        }

        _ = Sender.SendAsync(message);
    }

    #endregion

    #region Initiator

    public async Task SendAsync(MessageInitiator initiator, MessageAction action, params string[] description)
    {
        await SendInitiatorMessageAsync(initiator.ToString(), action, null, null, description);
    }

    #endregion

    #region Initiator & Target

    public async Task SendAsync(MessageInitiator initiator, MessageAction action, MessageTarget target, IEnumerable<FilesAuditReference> references = null,
        params string[] description)
    {
        await SendInitiatorMessageAsync(initiator.ToString(), action, target, references, description);
    }

    #endregion

    private async Task SendInitiatorMessageAsync(string initiator, MessageAction action, MessageTarget target, IEnumerable<FilesAuditReference> references = null, params string[] description)
    {
        if (Sender == null)
        {
            return;
        }

        var message = await messageFactory.CreateAsync(Request, initiator, null, action, target, references, description);
        if (!messagePolicy.Check(message))
        {
            return;
        }

        _ = Sender.SendAsync(message);
    }
    
    public async Task<int> SendLoginMessageAsync(MessageUserData userData, MessageAction action)
    {
        if (Sender == null)
        {
            return 0;
        }

        var message = await messageFactory.CreateAsync(Request, userData, action);
        if (!messagePolicy.Check(message))
        {
            return 0;
        }

        return await Sender.SendAsync(message);
    }

    private static bool TryAddNotificationParam(MessageAction action, Guid userId, out string parameter)
    {
        return TryAddNotificationParam(action, [userId], out parameter);
    }

    private static bool TryAddNotificationParam(MessageAction action, List<Guid> userIds, out string parameter, EmployeeType userType = 0)
    {
        parameter = "";

        switch (action)
        {
            case MessageAction.UsersUpdatedType:
                parameter = JsonSerializer.Serialize(new EventDescription<int>
                {
                    UserIds = userIds,
                    UserRole = (int)userType
                }, _serializerOptions);
                break;
            case MessageAction.UserCreated or MessageAction.UserUpdated:
                parameter = JsonSerializer.Serialize(new EventDescription<int>
                {
                    UserIds = userIds
                }, _serializerOptions);
                break;
            default:
                return false;
        }

        return true;
    }
}

public class EventDescription<T>
{
    public T RoomId { get; set; }
    public string RoomTitle { get; set; }
    public string RoomOldTitle { get; set; }
    public string RootFolderTitle { get; set; }
    public int UserRole { get; set; }
    public List<Guid> UserIds { get; set; }
    public Guid? CreateBy { get; set; }
    public T ParentId { get; set; }
    public string ParentTitle { get; set; }
    public int? ParentType { get; set; }
    public int? Type { get; set; }
    public string ToParentTitle { get; set; }
    public int? ToParentType { get; set; }
    public string FromParentTitle { get; set; }
    public int? FromParentType { get; set; }
    public int? FromFolderId { get; set; }
    
    public EventDescription() { }
}
