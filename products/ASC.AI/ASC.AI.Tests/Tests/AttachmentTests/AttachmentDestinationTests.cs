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
/// The destination of a device file. "Upload from device" in a chat becomes a DocSpace file — in the area the chat is open
/// in, or in My Documents when the user cannot create files there — and only then a chat attachment. There is no upload
/// route under <c>/api/2.0/ai/</c>, so the destination is chosen by the client before it calls
/// <c>POST /files/{folderId}/upload</c>; what the server can show is that the two signals the choice depends on are true: the
/// folder's advertised <c>security.Create</c> matches what an upload to it really does.
/// </summary>
/// <remarks>
/// The TypeScript suite called the room an "agent"; it is an AI room now (<see cref="RoomType.AiRoom"/>), which still
/// carries the built-in Knowledge and Chat outputs folders.
/// </remarks>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentDestinationTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    public static TheoryData<string> CandidateUploadRoutes { get; } =
    [
        "/api/2.0/ai/attachments/upload",
        "/api/2.0/ai/attachments/save-file-from-docspace",
        "/api/2.0/ai/attachments/from-file",
        "/api/2.0/ai/attachments/save-docspace-file",
        "/api/2.0/ai/files/upload",
        "/api/2.0/ai/upload"
    ];

    public static TheoryData<string, bool> Destinations { get; } = new()
    {
        { "the AI room root", false },
        { "the room's Chat outputs", true },
        { "My Documents", true }
    };

    [Theory]
    [MemberData(nameof(CandidateUploadRoutes))]
    public async Task Post_CandidateChatUploadRoute_DoesNotExist(string route)
    {
        // The honest boundary: every plausible name answers 404, so the destination is chosen by the client and only a UI
        // test can show the branch is taken. Kept as a list so that the day one of them appears, this fails and is
        // rewritten instead of quietly staying true.
        var outcome = await SendRawAsync(HttpMethod.Post, route, "{}");

        outcome.Status.Should().Be(404, $"POST {route}");
    }

    [Theory]
    [MemberData(nameof(Destinations))]
    public async Task Upload_FolderAdvertisesSecurityCreate_TruthfullyForADeviceFile(string label, bool writable)
    {
        // The target folder's `security.Create` must be the truth: a client that trusts it and a client that tries the
        // upload have to reach the same conclusion.
        var roomId = await CreateAiRoomAsync();
        var folderId = label switch
        {
            "the AI room root" => roomId,
            "the room's Chat outputs" => await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs),
            _ => await MyDocumentsOfAsync(Owner)
        };

        var advertised = await AdvertisedCreateAsync(Owner, folderId);
        var (status, _) = await UploadDeviceFileAsync(
            Owner,
            folderId.ToString(),
            $"autotest-probe-{Suffix()}.txt",
            System.Text.Encoding.UTF8.GetBytes(DeviceText),
            "text/plain");

        advertised.Should().Be(writable, $"{label} advertises security.Create");
        status.Should().Be(writable ? HttpStatusCode.OK : HttpStatusCode.Forbidden, $"uploading into {label}");
    }

    [Fact]
    public async Task AiRoomRoot_HoldsNoFilesAtAll_ItsSubfoldersDo()
    {
        // Why the destination for an AI chat cannot simply be "the room". The root refuses both routes that could create
        // a file, to the portal owner as much as to anyone, while an ordinary Custom room takes the same request. The
        // portal's own export route resolves an AI room id to Chat outputs for exactly this reason.
        var roomId = await CreateAiRoomAsync();
        await FilesHttpClient.Authenticate(Owner);

        var created = await StatusOfAsync(async () =>
            await FilesApi.CreateFileAsync(roomId, new CreateFileRequest("autotest.docx"), TestContext.Current.CancellationToken));
        created.Should().Be(403, "creating a file in the AI room");
        (await ListFilesAsync(Owner, roomId)).Should().BeEmpty("the AI room root stays empty");

        var customRoomId = await CreateCustomRoomAsync("Autotest Root Control Room");
        var (status, _) = await UploadDeviceFileAsync(Owner, customRoomId.ToString(), "autotest-control.txt", System.Text.Encoding.UTF8.GetBytes(DeviceText), "text/plain");
        status.Should().Be(HttpStatusCode.OK, "the same upload into a Custom room");
    }

    [Fact]
    [Trait("Bug", "84033")]
    public async Task CreateFolder_DirectlyInAKnowledgeFolder_IsNotCreated()
    {
        // Knowledge is meant to hold only what the room vectorizes and searches over. A subfolder inside it has no meaning
        // to that search and only adds structure the feature was never designed to walk, so Knowledge refuses to create
        // one. Until it was fixed, `createFolder` against Knowledge answered 200 and the folder was really filed there.
        // (On a portal with vectorization switched off every create in Knowledge is refused anyway; the listing check
        // below is what keeps the test about the folder.)
        var roomId = await CreateAiRoomAsync();
        var knowledgeId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.Knowledge);
        const string folderTitle = "Autotest Knowledge Subfolder";

        var created = await OutcomeAsync(() => FoldersApi.CreateFolderWithHttpInfoAsync(
            knowledgeId,
            new CreateFolderRequest(folderTitle),
            TestContext.Current.CancellationToken));

        // The side effect before the status, so a status failure never hides whether the folder also leaked in.
        var knowledge = (await FoldersApi.GetFolderByFolderIdAsync(knowledgeId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (knowledge.Folders ?? []).OfType<FolderDto>().Select(folder => folder.Title)
            .Should().NotContain(folderTitle, "the folder must not appear in Knowledge");

        created.Status.Should().NotBe(200, "creating a subfolder in Knowledge");
    }

    [Fact]
    public async Task Upload_ChatOutputs_TakesADeviceFileAndKeepsItOutOfTheKnowledge()
    {
        // The writable place inside an AI room that does not change what the room knows. The file is real — the bytes come
        // back — and it carries no vectorization status, the field only Knowledge files get.
        var roomId = await CreateAiRoomAsync();
        var knowledgeId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.Knowledge);
        var chatOutputsId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs);
        var fileName = $"autotest-device-{Suffix()}.txt";
        var content = System.Text.Encoding.UTF8.GetBytes(DeviceText);

        var file = await UploadStoredAsync(Owner, chatOutputsId.ToString(), fileName, content, "text/plain");

        file.FolderId.Should().Be(chatOutputsId);
        file.PureContentLength.Should().Be(content.Length);
        System.Text.Encoding.UTF8.GetString(await DownloadAsync(Owner, file.Id)).Should().Be(DeviceText);
        (await ListFilesAsync(Owner, knowledgeId)).Select(entry => entry.Title)
            .Should().NotContain(fileName, "a chat attachment must not appear in the room's Knowledge");

        var info = await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
        info.Response.VectorizationStatus.Should().BeNull("a file outside Knowledge is never queued for indexing");
    }
}
