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
/// What a private room keeps away from the rest of the portal: the end-to-end encrypted rooms app switches all of it
/// off and on, no Forms area and no Favorites list holds a private room, and an encrypted file offers no action and no
/// AI that would need its content.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomContentGatesTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<FolderDto> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        return await CreatePrivateRoom($"Autotest Private {Guid.NewGuid():N}", RoomType.CustomRoom);
    }

    private async Task SetApp(bool enabled)
    {
        await _webApiClient.Authenticate(Owner);
        var apps = new DocSpace.API.SDK.Api.Apps.AppsApi(_webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
        await apps.SetEnabledAsync("e2e-rooms", new SetAppEnabledRequest(enabled), TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> RoomTitles(SearchArea area, RoomPrivacyFilter? privacyFilter = null)
    {
        var rooms = (await _roomsApi.GetRoomsFolderAsync(searchArea: area, privacyFilter: privacyFilter, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. rooms.Folders.Select(f => f.Title)];
    }

    /// <summary>Sets the private flag of a room in the database, as a room saved before a refusal existed has it.</summary>
    private async Task MarkPrivateInDatabase(int roomId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE files_room_settings SET private = 1 WHERE room_id = @id;";
        command.Parameters.AddWithValue("@id", roomId);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    private static async Task<int> StatusOf(Func<Task> call)
    {
        try
        {
            await call();

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    [Fact]
    public async Task AppOff_NoRoomListHoldsAPrivateRoom_AndItComesBackWhenTheAppIsOn()
    {
        var room = await CreateRoomAsOwner();
        (await RoomTitles(SearchArea.PrivateRooms)).Should().Contain(room.Title);

        await SetApp(false);
        await _filesClient.Authenticate(Owner);
        (await RoomTitles(SearchArea.PrivateRooms)).Should().NotContain(room.Title);
        (await RoomTitles(SearchArea.Active, RoomPrivacyFilter.Private)).Should().NotContain(room.Title);
        (await RoomTitles(SearchArea.Any)).Should().NotContain(room.Title);

        await SetApp(true);
        await _filesClient.Authenticate(Owner);
        (await RoomTitles(SearchArea.PrivateRooms)).Should().Contain(room.Title);
    }

    [Fact]
    public async Task AppOff_CreatingAPrivateRoomIsForbidden()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        await SetApp(false);

        await _filesClient.Authenticate(Owner);
        var status = await StatusOf(() => _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Private {Guid.NewGuid():N}", roomType: RoomType.CustomRoom, @private: true),
            TestContext.Current.CancellationToken));

        status.Should().Be(403);
    }

    [Fact]
    public async Task AppOff_PrivateEntriesAnswerAsMissingToAMember_AndOwnKeysStayReadable()
    {
        var member = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        var folder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, member, FileShare.RoomManager);

        await _filesClient.Authenticate(member);
        (await StatusOf(() => _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken))).Should().Be(200);

        await SetApp(false);
        await _filesClient.Authenticate(member);

        (await StatusOf(() => _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken))).Should().Be(404);
        (await StatusOf(() => _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken))).Should().Be(404);
        (await StatusOf(() => _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken))).Should().Be(404);
        (await StatusOf(() => _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken))).Should().Be(404);
        (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AppOff_MembersKeysAndDownloadAreRefusedToAMember()
    {
        // Each call is seen to work for the member first, so a refusal after the switch is the gate's.
        var member = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, member, FileShare.RoomManager);
        var ct = TestContext.Current.CancellationToken;

        Func<Task>[] calls =
        [
            () => _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: ct),
            () => _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = member.Id, Access = FileShare.RoomManager }], Notify = false }, cancellationToken: ct),
            () => _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, ct),
            () => _sharingApi.GetEncryptionAccessAsync(fileId, ct),
            () => _filesApi.GetPresignedFileUriAsync(fileId, ct)
        ];

        await _filesClient.Authenticate(member);
        foreach (var call in calls)
        {
            (await StatusOf(call)).Should().Be(200);
        }

        await SetApp(false);
        await _filesClient.Authenticate(member);

        foreach (var call in calls)
        {
            (await StatusOf(call)).Should().BeOneOf(403, 404);
        }
    }

    [Fact]
    public async Task FormsAreas_LeaveOutAPrivateFormRoomFromBefore()
    {
        await _filesClient.Authenticate(Owner);
        var formRoom = await CreateFillingFormsRoom($"Autotest Forms {Guid.NewGuid():N}");
        (await RoomTitles(SearchArea.Forms)).Should().Contain(formRoom.Title);

        await MarkPrivateInDatabase(formRoom.Id);

        (await RoomTitles(SearchArea.Forms)).Should().NotContain(formRoom.Title);
        (await RoomTitles(SearchArea.FormTemplates)).Should().NotContain(formRoom.Title);
    }

    [Fact]
    public async Task Favorites_SkipAPrivateRoomAndItsFolder()
    {
        var room = await CreateRoomAsOwner();
        var folder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;

        var answer = (await _filesOperationsApi.AddFavoritesAsync(
            new BaseBatchRequestDto { FolderIds = [new(room.Id), new(folder.Id)] },
            TestContext.Current.CancellationToken)).Response;

        answer.Should().BeTrue();
        var favorites = (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        favorites.Folders.OfType<FolderDto>().Select(f => f.Id).Should().NotContain([room.Id, folder.Id]);
    }

    [Fact]
    public async Task Favorites_HideARoomMarkedBeforeItWasPrivate()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room {Guid.NewGuid():N}");
        await _filesOperationsApi.AddFavoritesAsync(new BaseBatchRequestDto { FolderIds = [new(room.Id)] }, TestContext.Current.CancellationToken);
        (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response
            .Folders.OfType<FolderDto>().Select(f => f.Id).Should().Contain(room.Id);

        await MarkPrivateInDatabase(room.Id);

        (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response
            .Folders.OfType<FolderDto>().Select(f => f.Id).Should().NotContain(room.Id);
    }

    [Fact]
    public async Task EncryptedFile_OffersTheOwnerNoActionThatNeedsItsContent()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");

        var security = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Security;

        security.Rename.Should().BeFalse();
        security.ReadHistory.Should().BeFalse();
        security.EditHistory.Should().BeFalse();
        security.Lock.Should().BeFalse();
        security.CopyLink.Should().BeFalse();
        security.CustomFilter.Should().BeFalse();
        security.SubmitToFormGallery.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptedFile_OwnerCannotCommentAVersion()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");

        var status = await StatusOf(() => _filesOperationsApi.UpdateFileCommentAsync(fileId, new UpdateCommentRequest(1, "a comment"), TestContext.Current.CancellationToken));

        status.Should().Be(403);
    }

    [Fact]
    public async Task PrivateRoom_HasNoAi_InTheRoomOrTheEditor()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");

        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var config = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        roomInfo.Security.CanUseAi.Should().BeFalse();
        config.EditorConfig.Customization.Ai.Disabled.Should().BeTrue();
    }

    [Fact]
    public async Task OrdinaryRoom_KeepsAiForTheSameCaller()
    {
        // The counterpart of the test above, so that a portal with AI off cannot make that one pass.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room {Guid.NewGuid():N}");
        var file = await CreateFile("Autotest Open.docx", room.Id);

        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var config = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        roomInfo.Security.CanUseAi.Should().BeTrue();
        config.EditorConfig.Customization.Ai.Disabled.Should().BeFalse();
    }
}
