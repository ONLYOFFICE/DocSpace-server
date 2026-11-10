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

namespace ASC.Files.Tests.Tests._07_Settings.Quota;

/// <summary>
/// How the user quota, the Standalone portal-wide quota and the room quota are enforced on file writes, moves and
/// the editor configuration. A new document from the template is between 4 KB and 8 KB (see
/// <see cref="QuotaTests"/>), so a 4 KB limit takes one soft overrun and refuses the next write.
/// </summary>
public class QuotaEnforcementTests(AspireAppFixture fixture) : QuotaTestBase(fixture)
{
    private const int FourKilobytes = 4 * 1024;

    [Fact]
    public async Task CreateFile_UserQuotaExceeded_AllowsOneOverrunThenReturns402()
    {
        // Arrange
        await EnableUserQuota(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var myFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        await CreateFile("first.docx", myFolderId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateFile("second.docx", myFolderId));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("User space quota exceeded (4 KB)");
    }

    [Fact]
    public async Task CreateFile_PortalQuotaExceeded_AllowsOneOverrunThenReturns402()
    {
        // Arrange
        await CapPortal(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var myFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        await CreateFile("first.docx", myFolderId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateFile("second.docx", myFolderId));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("The used storage size should not exceed");
    }

    [Fact]
    public async Task MoveFile_IntoRoomWithoutRoomForIt_IsRefusedWithoutOverrun()
    {
        // Arrange: creating the same document in the room would take the soft overrun; a move takes none
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("moved.docx", Owner);
        var room = await CreateVirtualRoom("Move quota room");

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            DestFolderId = new(room.Id),
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        var statuses = await WaitLongOperation();

        // Assert
        statuses.Should().ContainSingle(s => s.Error != null && s.Error.Contains("Room space quota exceeded"));
        (await GetFile(file.Id)).FolderId.Should().NotBe(room.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task MoveFile_IntoAnotherAccountsFullMyDocuments_IsRefusedAndLeavesItsQuota()
    {
        // Arrange: the owner shares a folder of My documents with an admin, who has a document of his own
        await _filesClient.Authenticate(Owner);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var sharedFolder = await CreateFolderInMy("shared with admin", Owner);
        await _sharingApi.SetFolderSecurityInfoAsync(
            sharedFolder.Id,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = admin.Id, Access = FileShare.ReadWrite }] },
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(admin);
        var file = await CreateFileInMy("admins.docx", admin);

        // a default of 1 byte: the owner's My documents has no room for the document
        await EnableUserQuota(1);

        // Act
        await _filesClient.Authenticate(admin);
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            DestFolderId = new(sharedFolder.Id),
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        var statuses = await WaitLongOperation();

        // Assert: the move is expected to be refused like a move into a room, but the receiver's custom quota is
        // raised by the file size instead, by a caller without the portal-settings right over it (Q2)
        await _peopleClient.Authenticate(Owner);
        var owner = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        owner.IsCustomQuota.Should().NotBe(true, "a move must not grow another account's limit");
        owner.QuotaLimit.Should().Be(1);
        statuses.Should().ContainSingle(s => s.Error != null && s.Error.Contains("User space quota exceeded"));
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task UpdateRoomsQuota_AbovePortalQuota_Returns400()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Above portal quota room");
        await CapPortal(1_048_576);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _quotaApi.UpdateRoomsQuotaAsync(
                new UpdateRoomsQuotaRequestDto([new(room.Id)], 2 * 1_048_576),
                TestContext.Current.CancellationToken));

        // Assert: refused with InvalidOperationException (403), the status of a room the caller cannot change (Q4)
        exception.ErrorContent!.ToString().Should().Contain("The entered value is greater than the total storage");
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task OpenEditFile_UserQuotaReached_OpensWithoutEditRightAndUserScope()
    {
        // Arrange
        await EnableUserQuota(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("over user quota.docx", Owner);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.QuotaExceededScope.Should().Be(QuotaScope.User);
        config.Document.Permissions.Edit.Should().BeFalse();
    }

    [Fact]
    public async Task OpenEditFile_RoomQuotaReached_OpensWithoutEditRightAndRoomScope()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Over room quota");
        var file = await CreateFile("over room quota.docx", room.Id);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.QuotaExceededScope.Should().Be(QuotaScope.Room);
        config.Document.Permissions.Edit.Should().BeFalse();
    }

    [Fact]
    public async Task OpenEditFile_PortalQuotaReached_OpensWithoutEditRightAndTenantScope()
    {
        // Arrange
        await CapPortal(FourKilobytes);
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("over portal quota.docx", Owner);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.QuotaExceededScope.Should().Be(QuotaScope.Tenant);
        config.Document.Permissions.Edit.Should().BeFalse();
    }

    /// <summary>
    /// Enables the portal's default user quota. <c>POST settings/userquotasettings</c> is hidden from the SDK
    /// (<c>IgnoreApi</c>), so it goes over raw HTTP.
    /// </summary>
    private async Task EnableUserQuota(long defaultQuota)
    {
        await _webApiClient.Authenticate(Owner);

        var json = JsonSerializer.Serialize(new { enableQuota = true, defaultQuota });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _webApiClient.PostAsync("api/2.0/settings/userquotasettings", content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Caps the test portal through the Standalone portal-wide quota.</summary>
    private async Task CapPortal(long quota)
    {
        await _webApiClient.Authenticate(Owner);

        await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestDto(TenantId, quota), TestContext.Current.CancellationToken);
    }
}
