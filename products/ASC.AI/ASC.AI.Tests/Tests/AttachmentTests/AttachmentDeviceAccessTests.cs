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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// Who can store a device file inside an AI room. These are the permission facts the client's destination choice rests
/// on: which members the area refuses, and whether the fallback target (their own My Documents) exists for them. An AI room
/// grants only Read, Content Creator and Room Manager, so Viewer is the only level without the right to create files.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentDeviceAccessTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    public async Task Upload_ViewerInTheAiRoom_IsRefusedThereButCanWriteToTheirOwnMyDocuments()
    {
        var roomId = await CreateAiRoomAsync();
        var chatOutputsId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs);

        // Positive control for the 403 below: this folder does accept uploads.
        await UploadTextAsync(Owner, chatOutputsId.ToString(), $"autotest-owner-control-{Suffix()}.txt", DeviceText);

        var member = await AddMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.Read);

        // Premise: the invitation took effect. Without it the 403 could just as well be "not a member at all".
        await FilesHttpClient.Authenticate(member);
        (await FoldersApi.GetFoldersWithHttpInfoAsync(roomId, TestContext.Current.CancellationToken)).StatusCode
            .Should().Be(HttpStatusCode.OK, "a Viewer can see the AI room");

        var fileName = $"autotest-device-{Suffix()}.txt";
        var content = System.Text.Encoding.UTF8.GetBytes(DeviceText);

        var (status, _) = await UploadDeviceFileAsync(member, chatOutputsId.ToString(), fileName, content, "text/plain");

        status.Should().Be(HttpStatusCode.Forbidden, "a Viewer storing a device file inside the AI room");
        (await ListFilesAsync(Owner, chatOutputsId)).Select(entry => entry.Title)
            .Should().NotContain(fileName, "nothing was created behind the refusal");

        var memberFolderId = await MyDocumentsOfAsync(member);
        var stored = await UploadStoredAsync(member, "@my", fileName, content, "text/plain");
        stored.FolderId.Should().Be(memberFolderId, "the target a client would fall back to exists and is writable");
    }

    [Fact]
    public async Task ShareRoom_AiRoomDoesNotOfferEditing_SoViewerIsTheOnlyLevelWithoutTheRightToCreate()
    {
        // In an ordinary room the levels that cannot create a file are Viewer and Editor; an AI room refuses to grant
        // Editing at all, to a plain User as much as to a Guest. A matrix copied from another room type would test a state
        // that cannot exist.
        var roomId = await CreateAiRoomAsync();
        var member = await AddMemberAsync(EmployeeType.User);

        // Controls first: the request shape is accepted for the levels the room does support, so the refusal below is
        // about `Editing` and nothing else.
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.Read);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.ContentCreator);

        var refused = await Assert.ThrowsAsync<ApiException>(
            async () => await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.Editing));

        refused.ErrorCode.Should().Be(403, "granting Editing");
    }

    [Fact]
    public async Task Upload_ContentCreatorInTheAiRoom_StoresADeviceFileThere()
    {
        // The positive half of the matrix: with the right to create files the member's file stays inside the room, so the
        // refusals above are about the access level and not about members being shut out of an AI room altogether.
        var roomId = await CreateAiRoomAsync();
        var chatOutputsId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs);
        var member = await AddMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.ContentCreator);
        var fileName = $"autotest-device-{Suffix()}.txt";

        var file = await UploadTextAsync(member, chatOutputsId.ToString(), fileName, DeviceText);

        file.FolderId.Should().Be(chatOutputsId);
        (await ListFilesAsync(Owner, chatOutputsId)).Select(entry => entry.Title)
            .Should().Contain(fileName, "the member's device file is inside the AI room");
    }

    [Fact]
    public async Task Upload_GuestInTheAiRoom_HasNowhereAtAllToPutADeviceFile()
    {
        // A Guest can only ever hold Read in an AI room, and Read cannot create files — so the room is closed to them. The
        // target a client would fall back to does not exist either: a Guest has no personal folder.
        var roomId = await CreateAiRoomAsync();
        var chatOutputsId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs);

        // Positive control: the target folder is writable, so the Guest's 403 is about the Guest.
        await UploadTextAsync(Owner, chatOutputsId.ToString(), $"autotest-owner-control-{Suffix()}.txt", DeviceText);

        var guest = await AddMemberAsync(EmployeeType.Guest);
        await ShareRoomAsync(roomId, guest, DocSpace.API.SDK.Model.FileShare.Read);
        await FilesHttpClient.Authenticate(guest);
        (await FoldersApi.GetFoldersWithHttpInfoAsync(roomId, TestContext.Current.CancellationToken)).StatusCode
            .Should().Be(HttpStatusCode.OK, "the Guest can see the AI room");

        var fileName = $"autotest-guest-{Suffix()}.txt";
        var content = System.Text.Encoding.UTF8.GetBytes(DeviceText);

        var (inRoom, _) = await UploadDeviceFileAsync(guest, chatOutputsId.ToString(), fileName, content, "text/plain");
        inRoom.Should().Be(HttpStatusCode.Forbidden, "a Guest storing a device file inside the AI room");

        var myFolder = await Assert.ThrowsAsync<ApiException>(
            async () => await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken));
        myFolder.ErrorCode.Should().Be(404, "a Guest has no My Documents");

        var (fallback, _) = await UploadDeviceFileAsync(guest, "@my", fileName, content, "text/plain");
        fallback.Should().NotBe(HttpStatusCode.OK, "a Guest falling back to a My Documents that does not exist");
    }
}
