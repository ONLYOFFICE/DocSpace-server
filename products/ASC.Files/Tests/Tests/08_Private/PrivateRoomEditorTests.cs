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
/// The editor around an end-to-end encrypted file. The save callback of the document server is played
/// by the test: a refused callback never downloads anything, so no running converter is needed. That a
/// session really is dropped cannot be seen from here, because the stack shares one document server
/// address between all portals and a test cannot put a stand-in behind it.
/// </summary>
[Trait("Category", "Editing")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomEditorTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private const string Refusal = "permissions to edit";
    private const int ForceSaveStatus = 6;
    private const int UserSubmitInitiator = 3;

    // A document server leaves out what does not apply, and the tracker cannot read a null where it expects a value.
    private static readonly JsonSerializerOptions _callbackJson = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private async Task<(FolderDto Room, int FileId)> CreateRoomWithEncryptedFile(string title = "Encrypted.docx")
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, await UploadEncryptedFile(room.Id, title));
    }

    /// <summary>Plays the callback a document server sends when the session ends with something to save.</summary>
    private async Task<string> SendSaveCallback(int fileId, Guid userId, bool encrypted, int status = 2, int? forceSaveType = null, CancellationToken cancellationToken = default)
    {
        await _filesClient.Authenticate(Owner);
        var config = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var callback = new Uri(config.EditorConfig.CallbackUrl);

        var payload = JsonSerializer.Serialize(new
        {
            key = config.Document.Key,
            status,
            forcesavetype = forceSaveType,
            url = "http://127.0.0.1:1/never-downloaded",
            filetype = "docx",
            users = new[] { userId.ToString() },
            encrypted
        }, _callbackJson);

        using var request = new HttpRequestMessage(HttpMethod.Post, callback.PathAndQuery);
        request.Content = new StringContent(JsonSerializer.Serialize(new { token = SignAsDocumentServer(payload) }), Encoding.UTF8, "application/json");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        using var response = await _filesClient.SendAsync(request, linked.Token);

        return await response.Content.ReadAsStringAsync(linked.Token);
    }

    /// <summary>The token a document server wraps its callback in. The secret is the one the stack gives the editors and the services.</summary>
    private static string SignAsDocumentServer(string payload)
    {
        static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var content = $"{Encode(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""))}.{Encode(Encoding.UTF8.GetBytes(payload))}";

        return $"{content}.{Encode(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes(content)))}";
    }

    private async Task<int> CurrentVersion(int fileId)
    {
        await _filesClient.Authenticate(Owner);

        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Version;
    }

    private async Task AssertRefusedAndNoVersion(int fileId, Func<Task<string>> callback)
    {
        var version = await CurrentVersion(fileId);

        var answer = await callback();

        answer.Should().Contain("\"error\":1").And.Contain(Refusal);
        (await CurrentVersion(fileId)).Should().Be(version);
    }

    [Fact]
    public async Task SaveCallback_WithoutTheEncryptedFlag_IsRefusedAndWritesNoVersion()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        await AssertRefusedAndNoVersion(fileId, () => SendSaveCallback(fileId, Owner.Id, encrypted: false));
    }

    [Fact]
    public async Task SaveCallback_OfAFormSubmit_IsRefused()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        await AssertRefusedAndNoVersion(fileId, () => SendSaveCallback(fileId, Owner.Id, encrypted: true, ForceSaveStatus, UserSubmitInitiator));
    }

    [Fact]
    public async Task SaveCallback_OfSomebodyWhoNeverHadAccess_IsRefused()
    {
        var stranger = await InviteContact(EmployeeType.User);
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        await AssertRefusedAndNoVersion(fileId, () => SendSaveCallback(fileId, stranger.Id, encrypted: true));
    }

    [Fact]
    public async Task SaveCallback_OfAMemberRemovedFromTheRoom_IsRefused()
    {
        var member = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(member);
        await SetFakeKeys(publicKeyPrefix: "member");
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        // Somebody in the room has the file open, which is what makes the removal look at the sessions on it.
        await _filesClient.Authenticate(member);
        var key = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Document.Key;
        await _filesApi.TrackEditFileAsync(fileId, docKeyForTrack: key, cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, FileShare.None);

        await AssertRefusedAndNoVersion(fileId, () => SendSaveCallback(fileId, member.Id, encrypted: true));
    }

    [Fact]
    public async Task SaveCallback_OfAMemberWithTheFlag_GoesOnToDownloadTheFile()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        // A refusal answers at once. This save has passed the checks and is busy retrying an address that leads nowhere,
        // so the answer is not waited for: the stack cannot complete a save without a converter behind it.
        using var giveUp = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var answered = () => SendSaveCallback(fileId, Owner.Id, encrypted: true, cancellationToken: giveUp.Token);

        await answered.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveEditing_WithoutTheFlag_IsRefused()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();
        var version = await CurrentVersion(fileId);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.SaveEditingFileFromFormAsync(
            fileId,
            fileExtension: ".docx",
            file: new FileParameter(new MemoryStream(new byte[16])),
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await CurrentVersion(fileId)).Should().Be(version);
    }

    [Fact]
    public async Task RestoreVersion_ByUrl_IsRefused()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.RestoreFileVersionAsync(
            fileId, 1, "http://127.0.0.1:1/never-downloaded", TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ReferenceData_OfAFileInAPrivateRoom_IsRefused()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        var config = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDto(config.Document.ReferenceData.FileKey, config.Document.ReferenceData.InstanceId),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartFilling_OfAnEncryptedForm_IsRefused()
    {
        var (_, fileId) = await CreateRoomWithEncryptedFile("Form.pdf");

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.StartFillingFileAsync(fileId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task EncryptedForm_ReportsNoFilling(RoomType roomType)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", roomType);
        var fileId = await UploadEncryptedFile(room.Id, "Form.pdf");

        var file = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        file.Security.StartFilling.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptedFile_IsOfferedOnThePublicAddress()
    {
        // The browser downloads an encrypted file, so the address must not be rewritten for the document server the way an open one is.
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        var plainId = await UploadEncryptedFile(room.Id, "Plain.docx");
        await MakeFileOpen(plainId);

        var encrypted = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Document.Url;
        var plain = (await _filesApi.OpenEditFileAsync(plainId, cancellationToken: TestContext.Current.CancellationToken)).Response.Document.Url;

        new Uri(encrypted).Authority.Should().NotBe(new Uri(plain).Authority);
    }

    [Fact]
    public async Task FilesSettings_SayWhetherTheEditorsCanEncrypt()
    {
        await _filesClient.Authenticate(Owner);

        // raw: a false value and a property the server left out read the same through the model
        using var response = await new RawApiClient(_filesClient).GetAsync("api/2.0/files/settings", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        document.RootElement.GetProperty("response").GetProperty("docServiceEncryptionSupported").ValueKind
            .Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }
}
