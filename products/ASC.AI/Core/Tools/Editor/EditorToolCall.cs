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

namespace ASC.AI.Core.Tools.Editor;


public class EditorCallData : CallData
{
    public int ResultStorageId { get; init; }
    public required string Title { get; init; }
    public required string Extension { get; init; }
    public required EditorToolCallState State { get; init; }
}


[ProtoContract]
public class EditorToolResult
{
    [ProtoMember(1)]
    public int FileId { get; init; }

    [ProtoMember(2)]
    public string? Title { get; init; }

    [ProtoMember(3)]
    public string? Extension { get; init; }

    [ProtoMember(4)]
    public string? Error { get; init; }

    public ToolResponse<GeneratedFileResult> ToToolResponse()
    {
        return string.IsNullOrEmpty(Error)
            ? new ToolResponse<GeneratedFileResult>
            {
                Data = new GeneratedFileResult { Id = FileId, Title = Title!, Extension = Extension! }
            }
            : new ToolResponse<GeneratedFileResult> { Error = Error };
    }
}


public sealed record EditorToolRegistration(AIFunction Tool, Func<FunctionCallContent, CancellationToken, Task> OnToolCall);


internal static class EditorToolInvoker
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(60);

    public static async Task SubscribeAsync(
        IToolCallReceiver receiver,
        ToolCallWaiterRegistry registry,
        int resultStorageId,
        Guid userId,
        string extension,
        FunctionCallContent call,
        Func<IDictionary<string, object?>?, EditorToolCallState> stateFactory,
        CancellationToken cancellationToken)
    {
        var callData = new EditorCallData
        {
            CallId = call.CallId,
            UserId = userId,
            ResultStorageId = resultStorageId,
            Title = $"{GetArg(call.Arguments, "fileName")}{extension}",
            Extension = extension,
            State = stateFactory(call.Arguments)
        };

        var waiter = await receiver.SubscribeAsync<EditorToolResult>(callData, cancellationToken);
        registry.Add(call.CallId, waiter);
    }

    public static async Task<ToolResponse<GeneratedFileResult>> AwaitResultAsync(
        ToolCallWaiterRegistry registry,
        CancellationToken cancellationToken)
    {
        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        var waiter = callId is null ? null : registry.Take<EditorToolResult>(callId);
        if (waiter is null)
        {
            return new ToolResponse<GeneratedFileResult> { Error = "Tool call subscription was not established." };
        }

        await using (waiter)
        {
            try
            {
                var result = await waiter.WaitAsync(_waitTimeout, cancellationToken);
                return result.ToToolResponse();
            }
            catch (TimeoutException)
            {
                return new ToolResponse<GeneratedFileResult> { Error = "The user did not approve creating the file." };
            }
        }
    }

    public static string? GetArg(IDictionary<string, object?>? arguments, string name)
    {
        return arguments is not null && arguments.TryGetValue(name, out var value) ? value?.ToString() : null;
    }
}
