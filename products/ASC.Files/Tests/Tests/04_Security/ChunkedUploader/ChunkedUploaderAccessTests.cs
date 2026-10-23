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

namespace ASC.Files.Tests.Tests._04_Security.ChunkedUploader;

/// <summary>
/// The legacy <c>ChunkedUploader.ashx</c> handler: <c>?initiate=true&amp;folderid=..&amp;title=..&amp;fileSize=..</c>
/// opens an upload session, <c>POST ?uid=..</c> with the bytes in the multipart field <c>file</c> commits it
/// in one chunk. It is an HTTP handler, not a controller, so it has no OpenAPI document and no SDK
/// signature — the raw-HTTP carve-out of the tests rule applies.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "ChunkedUploader")]
public class ChunkedUploaderAccessTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Bug 84580: the handler used to check only that the caller was authenticated, never
    /// <c>CanCreateAsync</c> on the target folder, so a user who was not a member of a room could
    /// plant a file in it although the regular API answered 403 for that room. Fixed by running the
    /// same folder check as <c>POST /files/{folderId}/session</c>
    /// (<c>FileUploader.VerifyFolderForUploadAsync</c>) when a session for a new file is initiated.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "84580")]
    public async Task Initiate_RoomCallerIsNotMemberOf_DoesNotCreateFile()
    {
        // Arrange
        var room = await CreateCustomRoom("Chunked Uploader Victim Room");
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);

        var sanity = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(room.Id, TestContext.Current.CancellationToken));
        sanity.ErrorCode.Should().Be(403);

        // Act
        var uploadId = await InitiateAsync(room.Id, "attacker-file.txt", "PLANTED BY NON-MEMBER");
        var uploadStatus = uploadId is null ? (HttpStatusCode?)null : await UploadAsync(uploadId, "attacker-file.txt", "PLANTED BY NON-MEMBER");

        // Assert
        uploadStatus.Should().NotBe(HttpStatusCode.Created);

        await _filesClient.Authenticate(Owner);
        var roomAfter = (await _foldersApi.GetFolderInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomAfter.FilesCount.Should().Be(0, "a caller the regular API denies must not create a file through the legacy handler");
    }

    [Fact]
    public async Task Initiate_RoomCallerCanCreateIn_CreatesFile()
    {
        // Arrange
        var room = await CreateCustomRoom("Chunked Uploader Member Room");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);

        // Act
        var uploadId = await InitiateAsync(room.Id, "member-file.txt", "MEMBER CONTENT");
        uploadId.Should().NotBeNull();
        var uploadStatus = await UploadAsync(uploadId!, "member-file.txt", "MEMBER CONTENT");

        // Assert
        uploadStatus.Should().Be(HttpStatusCode.Created);

        await _filesClient.Authenticate(Owner);
        var roomAfter = (await _foldersApi.GetFolderInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomAfter.FilesCount.Should().Be(1);
    }

    /// <summary>
    /// Opens a session; returns its id, or <c>null</c> when the handler refused (it answers 200 with
    /// <c>success: false</c> on errors).
    /// </summary>
    private async Task<string?> InitiateAsync(int folderId, string title, string content)
    {
        var query = "ChunkedUploader.ashx?initiate=true" +
            $"&folderid={folderId}" +
            $"&title={Uri.EscapeDataString(title)}" +
            $"&fileSize={Encoding.UTF8.GetByteCount(content)}";

        using var response = await _filesClient.GetAsync(query, TestContext.Current.CancellationToken);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return payload.RootElement.TryGetProperty("data", out var data)
               && data.ValueKind == JsonValueKind.Object
               && data.TryGetProperty("id", out var id)
            ? id.GetString()
            : null;
    }

    private async Task<HttpStatusCode> UploadAsync(string uploadId, string fileName, string content)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);

        using var response = await _filesClient.PostAsync($"ChunkedUploader.ashx?uid={uploadId}", form, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }
}
