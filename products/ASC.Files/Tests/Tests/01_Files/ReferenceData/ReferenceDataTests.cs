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

namespace ASC.Files.Tests.Tests._01_Files.ReferenceData;

/// <summary>
/// <c>POST /files/file/referencedata</c> - functional coverage. The endpoint always answers with
/// HTTP 200 and a <see cref="FileReference"/>: <see cref="FileStorageService.GetReferenceDataAsync{T}"/>
/// (products/ASC.Files/Core/Core/FileStorageService.cs) never throws for a file it cannot resolve or
/// read - it returns a <see cref="FileReference"/> whose <c>Error</c> is set instead. Permission
/// coverage, including the two cases where that no-throw behaviour is a genuine bug, lives in
/// <see cref="ReferenceDataPermissionsTests"/>.
/// </summary>
[Trait("Category", "Files")]
public class ReferenceDataTests(
    AspireAppFixture fixture)
    : ReferenceDataTestBase(fixture)
{
    [Fact]
    public async Task GetReferenceData_UsingKeysFromOpenEditFile_ReturnsUrlKeyAndReferenceData()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest ReferenceData Room", "Autotest ReferenceData File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.Key.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReferenceData_Roundtrip_ResponseReferenceDataMatchesRequestKeys()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest ReferenceData Roundtrip Room", "Autotest ReferenceData Roundtrip File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.Key.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.FileKey.Should().Be(fileKey);
        result.Response.ReferenceData.InstanceId.Should().Be(instanceId);
    }

    [Fact]
    public async Task GetReferenceData_WithSourceFileId_StillResolvesByFileKey()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest ReferenceData SourceId Room", "Autotest ReferenceData SourceId File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId, sourceFileId: file.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.Key.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.FileKey.Should().Be(fileKey);
        result.Response.ReferenceData.InstanceId.Should().Be(instanceId);
    }

    [Fact]
    public async Task GetReferenceData_ReadOnlyRoomMember_CanEditRoomIsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest RefData ReadOnly Room", "Autotest RefData ReadOnly File.docx");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        await _filesClient.Authenticate(user);
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.Key.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.CanEditRoom.Should().BeFalse();
    }

    [Fact]
    public async Task GetReferenceData_AllOptionalFieldsTogether_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest RefData AllFields Room", "Autotest RefData AllFields File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId, sourceFileId: file.Id, path: "Sheet1!A1", link: link.SharedLink.ShareLink),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.Key.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.Should().NotBeNull();
    }

    /// <summary>
    /// A <c>fileKey</c>/<c>instanceId</c> pair that was never minted by <c>OpenEditFile</c> does not
    /// resolve to any file. Per <c>FileStorageService.GetReferenceDataAsync</c> this is not a 404: the
    /// endpoint answers 200 with <c>FileReference.Error</c> set instead of throwing, so this is a
    /// contract check on that error payload rather than an HTTP-status check. The TS source asserted
    /// <c>status === 404 || error != null</c>, hedging between the two; the product code shows only
    /// the second branch is ever reached.
    /// </summary>
    [Fact]
    public async Task GetReferenceData_ArbitraryFileKey_Returns200WithError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger("totally-fake-file-key-12345", "fake-instance-id"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Error.Should().NotBeNullOrEmpty();
    }
}
