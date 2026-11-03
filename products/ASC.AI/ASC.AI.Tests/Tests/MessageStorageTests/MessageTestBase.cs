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


namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// The message routes of the public API (<c>/api/2.0/ai/threads/*-message*</c>, served by the Node chat service).
/// A thread there needs a model to run against, so every thread of a test is bound to one profile on the stub
/// provider, created on first use.
/// </summary>
public abstract class MessageTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string AppendUserMessagePath = "/api/2.0/ai/threads/append-user-message";
    protected const string UpdateMessagePath = "/api/2.0/ai/threads/update-message";

    private string? _profileId;

    /// <summary>Creates a thread bound to this test's profile; returns its id.</summary>
    protected async Task<string> CreateMessageThreadAsync(string? title = null)
    {
        _profileId ??= (await CreateProfileAsync()).Id;

        var thread = await CreateThreadAsync(title, _profileId);

        return thread.ThreadId;
    }

    /// <summary>Stores a user message through <c>append-user-message</c>; returns the new message id.</summary>
    /// <remarks>The route answers with the whole stored message under <c>messageId</c>; its id is the message id.</remarks>
    protected async Task<string> AppendAsync(string threadId, string? text = null)
    {
        var result = await _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: threadId, message: BuildUserMessage(text)),
            TestContext.Current.CancellationToken);

        result.MessageId.Should().NotBeNull();
        result.MessageId.Id.Should().NotBeNullOrEmpty();

        return result.MessageId.Id;
    }

    /// <summary>
    /// The keyset cursor of <c>read-messages</c> after <paramref name="message"/>: its JSON-encoded sort key, the
    /// way the chat widget builds it from the last message of a page.
    /// </summary>
    protected static string CursorAfter(AiThreadMessageLike message) =>
        JsonSerializer.Serialize(new { createdAt = message.CreatedAt, id = message.Id });

    protected Task<List<AiThreadMessageLike>> ReadMessagesAsync(
        string threadId,
        int? count = null,
        string? cursor = null,
        string? direction = null) =>
        _aiThreads.AiThreadsReadMessagesAsync(threadId, count, cursor, direction, TestContext.Current.CancellationToken);

    protected Task<AiThreadMessageLike> GetMessageAsync(string messageId) =>
        _aiThreads.AiThreadsGetMessageByIdAsync(messageId, TestContext.Current.CancellationToken);
}
