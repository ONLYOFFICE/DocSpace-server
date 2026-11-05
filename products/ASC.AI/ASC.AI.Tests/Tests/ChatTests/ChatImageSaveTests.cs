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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Where a generated picture is saved: "into the current section (where the user may write) or else into My
/// Documents". The landing folder is matched on file ids taken before the request, not on a name: the picture's file
/// name is the server's to choose.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatImageSaveTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string ImageExtension = @"\.(png|jpe?g|webp|gif)$";

    [Fact]
    [Trait("Bug", "82861")]
    public async Task SendWithStream_PictureInARoomTheUserMayWriteTo_IsSavedIntoThatRoom()
    {
        var (chat, chatProfile, image, _) = await ArrangeDrawingAsync();
        var roomId = (await CreateRoomOfTypeAsync(RoomType.CustomRoom)).Id;
        var myDocsId = await MyDocumentsOfAsync(Owner);

        var roomBefore = (await ListFilesAsync(Owner, roomId)).Select(f => f.Id).ToHashSet();
        var myDocsBefore = await StableFileIdsAsync(Owner, myDocsId);

        await RequestPictureAsync(chat, image, chatProfile.Id, roomId);

        var landed = await WaitForNewFileAsync(Owner, roomId, roomBefore);
        landed.Should().NotBeNull($"the picture is saved into the room the chat is in\n{image.Describe()}");
        landed!.Title.Should().MatchRegex(ImageExtension);

        (await ListFilesAsync(Owner, myDocsId)).Where(f => !myDocsBefore.Contains(f.Id)).Select(f => f.Title)
            .Should().BeEmpty("the writable section was used, not the personal fallback");
    }

    [Fact]
    [Trait("Bug", "82861")]
    public async Task SendWithStream_PictureByAMemberWhoCannotWriteToTheRoom_FallsBackToTheirMyDocuments()
    {
        // The "(where the user may write)" half: a Read member is the one case where the current section is not a
        // legal destination, so the fallback is the whole of what is under test. Was BUG 82861: the picture landed
        // nowhere.
        var (chat, chatProfile, image, _) = await ArrangeDrawingAsync();
        var roomId = (await CreateRoomOfTypeAsync(RoomType.CustomRoom)).Id;
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.Read);

        var roomBefore = (await ListFilesAsync(Owner, roomId)).Select(f => f.Id).ToHashSet();
        var memberDocsId = await MyDocumentsOfAsync(member);
        var memberDocsBefore = await StableFileIdsAsync(member, memberDocsId);

        await _aiChatClient.Authenticate(member);
        await RequestPictureAsync(chat, image, chatProfile.Id, roomId);

        var landed = await WaitForNewFileAsync(member, memberDocsId, memberDocsBefore);
        landed.Should().NotBeNull($"the picture falls back to the member's own documents\n{image.Describe()}");
        landed!.Title.Should().MatchRegex(ImageExtension);

        (await ListFilesAsync(Owner, roomId)).Where(f => !roomBefore.Contains(f.Id)).Select(f => f.Title)
            .Should().BeEmpty("nothing was written into the room the member cannot write to");
    }

    /// <summary>
    /// Asks for a picture in a thread of its own, opened in <paramref name="folderId"/> as whoever
    /// <see cref="BaseTest._aiChatClient"/> is signed in as, and proves the drawing really was attempted — without
    /// that, "no file appeared" proves nothing.
    /// </summary>
    private async Task RequestPictureAsync(StubModelProvider chat, StubModelProvider image, string profileId, int folderId)
    {
        var entityId = folderId.ToString();
        var thread = await CreateThreadAsync(profileId: profileId, entityId: entityId);
        chat.Enqueue(StubModelProvider.ToolCall(GenerateImageTool, DrawingArguments), StubModelProvider.Text("Here is your picture."));

        var round = await SendWithStreamAsync(thread.ThreadId, PictureRequest, profileId, entityId);
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssistantMessages(messages).SelectMany(ToolCallsOf).Select(c => c.Name)
            .Should().Contain(GenerateImageTool, $"the drawing was attempted\n{chat.Describe()}\n{image.Describe()}");
    }
}
