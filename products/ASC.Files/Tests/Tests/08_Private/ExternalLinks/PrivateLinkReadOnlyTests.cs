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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// A visitor of a link of an item in a private room reads only. The document opens for viewing on the public address
/// the browser downloads the ciphertext from, also when the visitor asks for the edit mode. A visitor who has not signed
/// in reaches no operation that changes data (401: none of them is open without signing in). A signed-in visitor who is
/// not a member gets 403 for an upload, a new folder, a rename, a delete, a move and a link of their own, the editor's
/// save for them is refused, and without the cookie of the link session the file does not open at all.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkReadOnlyTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private sealed record Visit(int RoomId, int FolderId, int FileId, string Title, string Token, string[] Session, User? Visitor);

    /// <summary>A folder link with key material, a file in the folder, and a link session opened by its proof.</summary>
    private async Task<Visit> VisitFolderLink(User? visitor = null)
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var file = await UploadEncryptedFileInfo(folderId, "inside.docx");
        using var response = await PutFolderLink(folderId, new LinkBody { LinkProofHash = proof.Hash });
        ((int)response.StatusCode).Should().Be(200);
        var token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;

        await _filesClient.Authenticate(visitor);
        var session = await OpenLinkSession(token, proof);

        return new Visit(room.Id, folderId, file.Id, file.Title, token, session, visitor);
    }

    private async Task<Visit> VisitAsNonMember() => await VisitFolderLink(await InviteContact(EmployeeType.RoomAdmin));

    private async Task<(HttpStatusCode Status, JsonElement Answer, string Text)> Send(Visit visit, HttpMethod method, string path, object? body = null, bool withSession = true)
    {
        await _filesClient.Authenticate(visit.Visitor);
        using var response = await SendOnLink(method, path, visit.Token, withSession ? visit.Session : [], body);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var answer = response.StatusCode == HttpStatusCode.OK && text.Length > 0
            ? JsonSerializer.Deserialize<JsonElement>(text).GetProperty("response").Clone()
            : default;

        return (response.StatusCode, answer, text);
    }

    private async Task<FileDto> FileAsOwner(int fileId)
    {
        await _filesClient.Authenticate(Owner);

        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private static string ModeOf(JsonElement config) => config.GetProperty("editorConfig").GetProperty("mode").GetString()!;

    private static object UploadBody => new { fileName = $"{Guid.NewGuid()}.docx", fileSize = 1024, encrypted = true };

    [Fact]
    public async Task Visitor_OpensTheDocumentForViewing_OnThePublicAddress()
    {
        var visit = await VisitFolderLink();

        var (status, config, _) = await Send(visit, HttpMethod.Get, $"api/2.0/files/file/{visit.FileId}/openedit");
        await _filesClient.Authenticate(Owner);
        var ownerUrl = (await _filesApi.OpenEditFileAsync(visit.FileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Document.Url;

        status.Should().Be(HttpStatusCode.OK);
        ModeOf(config).Should().Be("view");
        config.GetProperty("document").GetProperty("permissions").GetProperty("edit").GetBoolean().Should().BeFalse();
        new Uri(config.GetProperty("document").GetProperty("url").GetString()!).Authority
            .Should().Be(new Uri(ownerUrl).Authority, "the browser downloads the ciphertext from the address a member gets");
    }

    [Fact]
    public async Task Visitor_AskingForTheEditMode_StillGetsTheViewMode()
    {
        var visit = await VisitFolderLink();

        var (status, config, _) = await Send(visit, HttpMethod.Get, $"api/2.0/files/file/{visit.FileId}/openedit?edit=true");

        status.Should().Be(HttpStatusCode.OK);
        ModeOf(config).Should().Be("view");
    }

    [Fact]
    public async Task Visitor_WhoHasNotSignedIn_ReachesNoOperationThatChangesData()
    {
        var visit = await VisitFolderLink();

        var upload = await Send(visit, HttpMethod.Post, $"api/2.0/files/{visit.FolderId}/upload/create_session", UploadBody);
        var folder = await Send(visit, HttpMethod.Post, $"api/2.0/files/folder/{visit.FolderId}", new { title = "Visitor folder" });
        var rename = await Send(visit, HttpMethod.Put, $"api/2.0/files/file/{visit.FileId}", new { title = "renamed.docx" });
        var delete = await Send(visit, HttpMethod.Put, "api/2.0/files/fileops/delete", new { fileIds = new[] { visit.FileId }, immediately = true });
        var link = await Send(visit, HttpMethod.Put, $"api/2.0/files/file/{visit.FileId}/links", new LinkBody());

        // Only the rename is open without signing in, and the file security refuses it; the others need an account.
        new[] { upload.Status, folder.Status, rename.Status, delete.Status, link.Status }
            .Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized);
        (await FileAsOwner(visit.FileId)).Title.Should().Be(visit.Title);
    }

    [Fact]
    public async Task SignedInVisitorWhoIsNoMember_OpensForViewing_EvenAskingForTheEditMode()
    {
        var visit = await VisitAsNonMember();

        var (status, config, _) = await Send(visit, HttpMethod.Get, $"api/2.0/files/file/{visit.FileId}/openedit?edit=true");

        status.Should().Be(HttpStatusCode.OK);
        ModeOf(config).Should().Be("view");
    }

    [Fact]
    public async Task SignedInVisitorWithoutTheSessionCookie_CannotOpenTheFile()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Get, $"api/2.0/files/file/{visit.FileId}/openedit", withSession: false);

        status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedInVisitor_CannotStartAnUpload()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Post, $"api/2.0/files/{visit.FolderId}/upload/create_session", UploadBody);

        status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedInVisitor_CannotCreateAFolder()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Post, $"api/2.0/files/folder/{visit.FolderId}", new { title = "Visitor folder" });

        status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedInVisitor_CannotRenameAFile()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Put, $"api/2.0/files/file/{visit.FileId}", new { title = "renamed.docx" });

        status.Should().Be(HttpStatusCode.Forbidden);
        (await FileAsOwner(visit.FileId)).Title.Should().Be(visit.Title);
    }

    [Fact]
    public async Task SignedInVisitor_CannotSetALinkOfTheirOwn()
    {
        var visit = await VisitAsNonMember();

        var (status, _, text) = await Send(visit, HttpMethod.Put, $"api/2.0/files/file/{visit.FileId}/links", new LinkBody());

        status.Should().Be(HttpStatusCode.Forbidden, text);
        await _filesClient.Authenticate(Owner);
        (await FileLinkCount(visit.FileId)).Should().Be(0);
    }

    [Fact]
    public async Task SignedInVisitor_CannotDeleteAFile()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Put, "api/2.0/files/fileops/delete",
            new { fileIds = new[] { visit.FileId }, immediately = true });

        await AssertRefusedOperation(status, visit);
    }

    [Fact]
    public async Task SignedInVisitor_CannotMoveAFile()
    {
        var visit = await VisitAsNonMember();

        var (status, _, _) = await Send(visit, HttpMethod.Put, "api/2.0/files/fileops/move",
            new { fileIds = new[] { visit.FileId }, destFolderId = visit.RoomId, conflictResolveType = 0 });

        await AssertRefusedOperation(status, visit);
    }

    /// <summary>
    /// A file operation is queued before its rights are checked, so the refusal may come as the error of the finished
    /// operation; either way the file stays where it was.
    /// </summary>
    private async Task AssertRefusedOperation(HttpStatusCode status, Visit visit)
    {
        if (status == HttpStatusCode.OK)
        {
            var finished = await PollUntil(
                async () => (await Send(visit, HttpMethod.Get, "api/2.0/files/fileops")).Answer,
                ops => ops.ValueKind == JsonValueKind.Array && ops.EnumerateArray().All(o => o.GetProperty("finished").GetBoolean()),
                TimeSpan.FromSeconds(30));

            finished.ValueKind.Should().Be(JsonValueKind.Array);
            finished.EnumerateArray().Should().NotBeEmpty()
                .And.OnlyContain(o => !string.IsNullOrEmpty(o.GetProperty("error").GetString()), "the operation is refused");
        }
        else
        {
            status.Should().Be(HttpStatusCode.Forbidden);
        }

        var file = await FileAsOwner(visit.FileId);
        file.FolderId.Should().Be(visit.FolderId);
        file.RootFolderType.Should().NotBe(FolderType.TRASH);
    }

    [Fact]
    public async Task SignedInVisitor_SaveFromTheEditor_IsRefused()
    {
        var visit = await VisitAsNonMember();
        await _filesClient.Authenticate(Owner);
        var config = (await _filesApi.OpenEditFileAsync(visit.FileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var version = (await FileAsOwner(visit.FileId)).Version;

        var payload = JsonSerializer.Serialize(new
        {
            key = config.Document.Key,
            status = 2,
            url = "http://127.0.0.1:1/never-downloaded",
            filetype = "docx",
            users = new[] { visit.Visitor!.Id.ToString() },
            encrypted = true
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(config.EditorConfig.CallbackUrl).PathAndQuery)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { token = SignAsDocumentServer(payload) }), Encoding.UTF8, "application/json")
        };
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        var answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        answer.Should().Contain("\"error\":1").And.Contain("permissions to edit");
        (await FileAsOwner(visit.FileId)).Version.Should().Be(version);
    }
}
