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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// What a private room accepts: ciphertext under a generated name, whichever way a file arrives. The rest of the
/// suite creates such files through <see cref="PrivacyRoomTestBase.UploadEncryptedFile"/>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomContentRulesTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<FolderDto> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);

        return await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
    }

    private async Task<string> OpenSession(int folderId, string name, bool encrypted)
    {
        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, 1024, encrypted: encrypted),
            TestContext.Current.CancellationToken)).Response;

        return session.Id;
    }

    [Fact]
    public async Task CreateFile_InTheRoom_IsRefused()
    {
        var room = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Plain.docx"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateFile_InASubfolderOfTheRoom_IsRefused()
    {
        var room = await CreateRoomAsOwner();
        var subfolder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.CreateFileAsync(subfolder.Id, new CreateFileRequest("Plain.docx"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Session_WithoutTheEncryptedFlag_IsRefused()
    {
        var room = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(() => OpenSession(room.Id, Guid.NewGuid() + ".docx", encrypted: false));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData("Report.docx")]
    [InlineData("0E8B7C3A-1F2D-4A5B-9C6D-7E8F9A0B1C2D.docx")]
    [InlineData("0e8b7c3a-1f2d-4a5b-9c6d-7e8f9a0b1c2d.DOCX")]
    [InlineData("0e8b7c3a-1f2d-4a5b-9c6d-7e8f9a0b1c2d.report.docx")]
    public async Task Session_WithAReadableName_IsABadRequest(string name)
    {
        var room = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(() => OpenSession(room.Id, name, encrypted: true));

        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData("0e8b7c3a-1f2d-4a5b-9c6d-7e8f9a0b1c2d.docx")]
    [InlineData("0e8b7c3a-1f2d-4a5b-9c6d-7e8f9a0b1c2d")]
    public async Task Session_WithAGeneratedName_IsAccepted(string name)
    {
        var room = await CreateRoomAsOwner();

        (await OpenSession(room.Id, name, encrypted: true)).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OrdinaryRoom_KeepsAcceptingOpenFilesAndAnyName()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest room");

        (await CreateFile("Plain.docx", room.Id)).Title.Should().Be("Plain.docx");
        (await OpenSession(room.Id, "Report.docx", encrypted: false)).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EmptyKeySet_CountsAsNoKeys()
    {
        await _filesClient.Authenticate(Owner);
        var key = await SetFakeKeys();
        await _privacyRoomApi.DeleteKeysAsync(key.Id, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest", roomType: RoomType.CustomRoom, @private: true),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task AbortSession_ThroughAnotherFolder_IsNotFoundAndKeepsTheSession()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest room");
        var other = await CreateCustomRoom("Autotest other room");

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest("Report.docx", 1024),
            TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesOperationsApi.AbortUploadSessionAsync(session.Id, other.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        await _filesOperationsApi.AbortUploadSessionAsync(session.Id, room.Id, TestContext.Current.CancellationToken);
    }
}
