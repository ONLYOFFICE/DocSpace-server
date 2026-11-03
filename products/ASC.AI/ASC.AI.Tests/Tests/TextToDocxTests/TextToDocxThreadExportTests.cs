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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// "Export this chat" end to end. There is no thread-export route: the client reads the thread, renders the turns as
/// markdown and posts the result to the generic exporter. Ported from messages.spec.ts, "AI Messages - exporting a
/// thread"; the conversation is scripted on a stub provider instead of a real model, and the agent the TS suite
/// created is an AI room here.
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxThreadExportTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    private readonly StubModelProvider _provider = new();

    public override async ValueTask DisposeAsync()
    {
        _provider.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Export_TwoTurnConversation_LandsInMyDocumentsInOrder()
    {
        const string firstQuestion = "Reply with exactly the word MERCURY.";
        const string secondQuestion = "Now reply with exactly the word VENUS.";

        var profile = await CreateProfileOnAsync(_provider);
        var agentId = (await CreateRoomAsync("Autotest Export Agent")).ToString();
        var thread = await CreateThreadAsync("Autotest export thread", profile.Id, agentId);

        _provider.Enqueue(StubModelProvider.Text("MERCURY"), StubModelProvider.Text("VENUS"));

        var first = await SendWithStreamAsync(thread.ThreadId, firstQuestion, profile.Id, agentId);
        first.StatusCode.Should().Be(HttpStatusCode.OK, first.ToString());
        first.StreamError.Should().BeNull(first.ToString());

        var second = await SendWithStreamAsync(thread.ThreadId, secondQuestion, profile.Id, agentId);
        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());
        second.StreamError.Should().BeNull(second.ToString());

        // A transcript is only worth exporting if the conversation really happened: two questions, two answers.
        var messages = await WaitForMessagesAsync(thread.ThreadId, 4);
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.User).Select(TextOf).Should().Equal(firstQuestion, secondQuestion);
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).Select(TextOf).Should().Equal("MERCURY", "VENUS");

        var transcript = RenderTranscript(messages);
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Autotest Chat Export {RandomSuffix()}";

        await ExportAcceptedAsync(title, transcript, folderId);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents");
        exported!.FileExst.Should().Be(".docx");
        exported.PureContentLength.Should().BeGreaterThan(0);

        // Every turn is in the document, still in the order it happened.
        var text = await ReadDocxTextAsync(exported.Id);
        string[] turns = [firstQuestion, "MERCURY", secondQuestion, "VENUS"];

        foreach (var turn in turns)
        {
            text.Should().Contain(turn);
        }

        text.IndexOf(firstQuestion, StringComparison.Ordinal).Should().BeLessThan(text.IndexOf(secondQuestion, StringComparison.Ordinal));
        text.IndexOf("VENUS", StringComparison.Ordinal).Should().BeGreaterThan(text.IndexOf(secondQuestion, StringComparison.Ordinal));
    }

    /// <summary>
    /// An agent is a room, so its id is a legal export target — but not the folder the document ends up in: everything
    /// the agent produces is filed under its Chat outputs folder, exports included.
    /// </summary>
    [Fact]
    public async Task Export_ToAnAgent_LandsInItsChatOutputsNotInTheRoot()
    {
        const string question = "Reply with exactly the word SATURN.";

        var profile = await CreateProfileOnAsync(_provider);
        var roomId = await CreateRoomAsync("Autotest Export Agent");
        var thread = await CreateThreadAsync("Autotest export thread", profile.Id, roomId.ToString());

        _provider.Enqueue(StubModelProvider.Text("SATURN"));

        var sent = await SendWithStreamAsync(thread.ThreadId, question, profile.Id, roomId.ToString());
        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());

        var transcript = RenderTranscript(await WaitForMessagesAsync(thread.ThreadId, 2));
        var title = $"Autotest Agent Export {RandomSuffix()}";

        await ExportAcceptedAsync(title, transcript, roomId);

        await FilesHttpClient.Authenticate(Owner);
        var room = (await FoldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var chatOutputs = room.Folders.OfType<FolderDto>().FirstOrDefault(folder => folder.Type == FolderType.ChatOutputs);
        chatOutputs.Should().NotBeNull("the agent's Chat outputs folder");

        var exported = await WaitForExportedFileAsync(chatOutputs!.Id, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in the agent's Chat outputs");

        // Nothing was left in the room root the export was addressed to.
        (await ListFolderFilesAsync(roomId)).Select(file => file.Title).Should().NotContain($"{title}.docx");

        (await ReadDocxTextAsync(exported!.Id)).Should().Contain(question);
    }

    /// <summary>
    /// A thread of forty turns of about 4 KB each is over the 128 KB body the route used to refuse with a bodyless
    /// 413, and there is no paged or server-side export to fall back on. Fixed: the size guard is gone and a thread
    /// this size exports in one call. Built with append-user-message — the length under test is the store's.
    /// </summary>
    [Fact]
    [Trait("Bug", "83096")]
    public async Task Export_LongThread_ExportsWhole()
    {
        var profile = await CreateProfileAsync();
        var agentId = (await CreateRoomAsync("Autotest Export Agent")).ToString();
        var thread = await CreateThreadAsync("Autotest long export thread", profile.Id, agentId);

        var texts = Enumerable.Range(1, 40).Select(i => $"Autotest turn {i}: {new string('x', 4000)}").ToList();

        foreach (var text in texts)
        {
            await AppendUserMessageAsync(thread.ThreadId, text);
        }

        // The premise: the thread really is that long, and the read gives all of it.
        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Select(TextOf).Should().Equal(texts, "the store kept the whole thread");

        var transcript = RenderTranscript(stored);
        System.Text.Encoding.UTF8.GetByteCount(transcript).Should().BeGreaterThan(131072, "the thread under test is bigger than the old body limit");

        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Autotest Long Export {RandomSuffix()}";

        await ExportAcceptedAsync(title, transcript, folderId);

        (await WaitForExportedFileAsync(folderId, $"{title}.docx")).Should().NotBeNull();
    }

    /// <summary>
    /// <c>update-message</c> used to stamp <c>createdAt</c> with the time of the edit, and since <c>read-messages</c>
    /// is ordered by <c>createdAt</c>, an edited earlier turn moved to the end of the thread — and of the exported
    /// document. Fixed: the edit keeps the message where it was.
    /// </summary>
    [Fact]
    [Trait("Bug", "83037")]
    public async Task Export_EditedTurn_IsExportedInPlace()
    {
        var profile = await CreateProfileAsync();
        var agentId = (await CreateRoomAsync("Autotest Export Agent")).ToString();
        var thread = await CreateThreadAsync("Autotest export thread with an edit", profile.Id, agentId);

        var firstId = await AppendUserMessageAsync(thread.ThreadId, "Autotest first FIRSTWORD");
        await AppendUserMessageAsync(thread.ThreadId, "Autotest second SECONDWORD");

        var edited = await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: firstId, message: BuildUserMessage("Autotest first EDITEDWORD")),
            TestContext.Current.CancellationToken);
        edited.Success.Should().BeTrue();

        // The edit landed, and on the same message — otherwise the order below would be about something else.
        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Should().HaveCount(2);
        stored.Select(TextOf).Should().Contain("Autotest first EDITEDWORD", "the edit was stored");
        stored.Select(message => message.Id).Should().Contain(firstId);

        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Autotest Edited Export {RandomSuffix()}";

        await ExportAcceptedAsync(title, RenderTranscript(stored), folderId);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents");

        var text = await ReadDocxTextAsync(exported!.Id);
        text.Should().Contain("EDITEDWORD");
        text.Should().Contain("SECONDWORD");
        text.IndexOf("EDITEDWORD", StringComparison.Ordinal).Should().BeLessThan(
            text.IndexOf("SECONDWORD", StringComparison.Ordinal),
            "the edited turn has to be exported where it belongs, before the turn that followed it");
    }

    /// <summary>
    /// The empty end of the range: after <c>clear-messages</c> the transcript is empty, and the exporter refuses it
    /// instead of filing an empty document.
    /// </summary>
    [Fact]
    public async Task Export_ClearedThread_IsRefusedAndFilesNothing()
    {
        var profile = await CreateProfileAsync();
        var agentId = (await CreateRoomAsync("Autotest Export Agent")).ToString();
        var thread = await CreateThreadAsync("Autotest cleared export thread", profile.Id, agentId);

        await AppendUserMessageAsync(thread.ThreadId, "Autotest one");
        await AppendUserMessageAsync(thread.ThreadId, "Autotest two");

        // Positive control: the thread had a history, so the empty read below is the clear.
        (await ReadMessagesAsync(thread.ThreadId)).Should().HaveCount(2);

        (await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Should().BeEmpty();

        var transcript = RenderTranscript(stored);
        transcript.Should().BeEmpty();

        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Autotest Cleared Export {RandomSuffix()}";

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport(title, transcript, folderId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        ErrorOf(exception.ErrorContent?.ToString()).Should().NotBeNullOrEmpty();

        // The 400 is synchronous: nothing was queued, so the listing is final at once.
        (await ListFolderFilesAsync(folderId)).Select(file => file.Title).Should().NotContain($"{title}.docx", "no empty document was filed");
    }

    /// <summary>
    /// Both halves of the flow are per-user: a plain User reads their own thread in an agent they were invited to and
    /// exports it into their own My Documents.
    /// </summary>
    [Fact]
    public async Task Export_MemberThread_LandsInTheMembersMyDocuments()
    {
        var profile = await CreateProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Export Agent");

        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(
                invitations: [new RoomInvitation(member.Id, DocSpace.API.SDK.Model.FileShare.ContentCreator)],
                notify: false),
            TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(member);

        var thread = await CreateThreadAsync("Autotest member export thread", profile.Id, roomId.ToString());
        string[] texts = ["Autotest member MERCURYWORD", "Autotest member VENUSWORD"];

        foreach (var text in texts)
        {
            await AppendUserMessageAsync(thread.ThreadId, text);
        }

        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Select(TextOf).Should().Equal(texts, "the member reads their whole thread");

        await FilesHttpClient.Authenticate(member);
        var folderId = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
        var title = $"Autotest Member Export {RandomSuffix()}";

        await ExportAcceptedAsync(title, RenderTranscript(stored), folderId);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in the member's My Documents");
        exported!.FileExst.Should().Be(".docx");

        var exportedText = await ReadDocxTextAsync(exported.Id);

        foreach (var turn in texts)
        {
            exportedText.Should().Contain(turn);
        }

        exportedText.IndexOf(texts[0], StringComparison.Ordinal).Should().BeLessThan(exportedText.IndexOf(texts[1], StringComparison.Ordinal));
    }
}
