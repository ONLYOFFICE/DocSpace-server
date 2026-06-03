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

namespace ASC.AI.Core.Chat.Tool;

public class ManagedFunctionInvokingChatClient(
    IChatClient innerClient,
    ToolHolder toolHolder,
    IToolCallReceiver toolCallReceiver,
    Guid userId) : FunctionInvokingChatClient(innerClient)
{
    private static readonly TimeSpan _permissionTimeout = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, IToolCallWaiter<ToolExecutionDecision>> _permissionRequests = [];
    private readonly ConcurrentDictionary<string, byte> _handledCalls = [];

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

                var context = toolHolder.GetContext(functionCallContent.Name);

                if (context.OnToolCallReceived is not null && _handledCalls.TryAdd(functionCallContent.CallId, 0))
                {
                    await context.OnToolCallReceived(functionCallContent, cancellationToken);
                }

                if (!context.AutoInvoke)
                {
                    functionCallContent.MarkAsManaged();
                    var callData = new PermissionCallData
                    {
                        ServerId = context.McpServerInfo!.ServerId,
                        RoomId = context.RoomId,
                        CallId = functionCallContent.CallId,
                        Name = context.Name,
                        UserId = userId
                    };

                    var waiter = await toolCallReceiver.SubscribeAsync<ToolExecutionDecision>(callData, cancellationToken);

                    _permissionRequests.TryAdd(functionCallContent.CallId, waiter);
                }

                if (context.McpServerInfo is not null)
                {
                    functionCallContent.AddMcpServerData(context.McpServerInfo);
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

        var toolContext = toolHolder.GetContext(context.CallContent.Name);
        if (toolContext.AutoInvoke)
        {
            return await base.InvokeFunctionAsync(context, cancellationToken);
        }

        if (!_permissionRequests.TryRemove(context.CallContent.CallId, out var waiter))
        {
            throw new ArgumentException("Permission request is not set for the tool.");
        }

        ToolExecutionDecision decision;
        await using (waiter)
        {
            try
            {
                decision = await waiter.WaitAsync(_permissionTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                decision = ToolExecutionDecision.Deny;
            }
        }

        if (decision is ToolExecutionDecision.Deny)
        {
            return ToFunctionTextResult("The user has chosen to disallow the tool call.");
        }

        if (decision is ToolExecutionDecision.AlwaysAllow)
        {
            toolContext.AutoInvoke = true;
        }

        return await base.InvokeFunctionAsync(context, cancellationToken);
    }

    protected override IList<ChatMessage> CreateResponseMessages(
        ReadOnlySpan<FunctionInvocationResult> results)
    {
        var contents = new List<AIContent>(results.Length);
        foreach (var t in results)
        {
            contents.Add(CreateFunctionResultContent(t));
        }

        return [new ChatMessage(ChatRole.Tool, contents)];

        FunctionResultContent CreateFunctionResultContent(FunctionInvocationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            object? functionResult;
            if (result.Status == FunctionInvocationStatus.RanToCompletion)
            {
                functionResult = result.Result ?? ToFunctionTextResult("Success: Function completed.");
            }
            else
            {
                var message = result.Status switch
                {
                    FunctionInvocationStatus.NotFound => $"Error: Requested function \"{result.CallContent.Name}\" not found.",
                    FunctionInvocationStatus.Exception => "Error: Function failed.",
                    _ => "Error: Unknown error."
                };

                if (IncludeDetailedErrors && result.Exception is not null)
                {
                    message = $"{message} Exception: {result.Exception.Message}";
                }

                functionResult = new ToolResponse<object> { Error = message };
            }

            if (functionResult is TextContent text)
            {
                functionResult = new { Content = new List<AIContent> { text } };
            }

            return new FunctionResultContent(result.CallContent.CallId, functionResult) { Exception = result.Exception };
        }
    }

    private static object ToFunctionTextResult(string message)
    {
        return new
        {
            Content = new List<AIContent>
            {
                new TextContent(message)
            }
        };
    }
}
