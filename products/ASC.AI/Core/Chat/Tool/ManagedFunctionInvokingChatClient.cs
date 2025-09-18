// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Core.Chat.Tool;

public class ManagedFunctionInvokingChatClient(
    IChatClient innerClient,
    ToolHolder toolHolder,
    IToolPermissionRequester permissionRequester) : FunctionInvokingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                if (content is not FunctionCallContent functionCallContent)
                {
                    continue;
                }

                var properties = toolHolder.GetProperties(functionCallContent.Name);
                if (!properties.AutoInvoke)
                {
                    functionCallContent.MarkAsManaged();
                }

                if (properties.McpServerInfo is not null)
                {
                    functionCallContent.AddMcpServerData(properties.McpServerInfo);
                }
            }
            
            yield return update;
        }
    }

    protected override async ValueTask<object?> InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        if (!context.IsStreaming)
        {
            throw new NotSupportedException("Managing function invocations is not supported for non-streaming responses.");
        }

        var properties = toolHolder.GetProperties(context.CallContent.Name);
        if (properties.AutoInvoke)
        {
            return await base.InvokeFunctionAsync(context, cancellationToken);
        }

        if (properties.McpServerInfo == null)
        {
            throw new ArgumentException("McpServerData is not set for the tool.");
        }

        var callData = new CallData
        {
            ServerId = properties.McpServerInfo.ServerId,
            RoomId = properties.RoomId,
            CallId = context.CallContent.CallId, 
            Name = properties.Name
        };
        
        var decision = await permissionRequester.RequestPermissionAsync(callData, cancellationToken);
        if (decision is ToolExecutionDecision.Deny)
        {
            return new
            {
                Content = new List<AIContent>
                {
                    new TextContent("The user has chosen to disallow the tool call.")
                }
            };
        }

        if (decision is ToolExecutionDecision.AlwaysAllow)
        {
            properties.AutoInvoke = true;
        }
        
        return await base.InvokeFunctionAsync(context, cancellationToken);
    }
}