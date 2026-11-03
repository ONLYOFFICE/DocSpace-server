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

namespace ASC.AI.Tests.Tests.ThirdPartyTests;

/// <summary>
/// Chat attachments from a third-party room (thirdparty.spec.ts, "chat attachments"): a file stored on the third-party
/// backend attaches like any DocSpace file, and a user who was never invited cannot attach it by id. Needs a Nextcloud
/// account; see <see cref="ThirdPartyTestBase"/>.
/// </summary>
[Trait("Category", "ThirdParty")]
[Trait("Requires", "Nextcloud")]
[Trait("Feature", "AI/ThirdParty")]
public class ThirdPartyAttachmentTests(AspireAppFixture fixture) : ThirdPartyTestBase(fixture)
{
    // The File type discriminator of an attachment input (FileType.Document).
    private const int DocumentFileType = 7;

    private DocSpace.API.SDK.Api.AI.AttachmentsApi Attachments => _aiAttachments;

    private async Task<ThirdPartyFileDto> StoreDocumentAsync(string roomId)
    {
        var content = System.Text.Encoding.UTF8.GetBytes($"Document body. {Marker}");
        var file = new FileParameter($"autotest-tp-{Guid.NewGuid():N}.txt", "text/plain", new MemoryStream(content));

        var uploaded = await FoldersApi.UploadFileAsync(roomId, file: file, cancellationToken: TestContext.Current.CancellationToken);

        return uploaded.Response.Single();
    }

    private Task<AiAttachment> AttachAsync(ThirdPartyFileDto stored) =>
        Attachments.AiAttachmentsSaveFileAsync(
            new AiAttachmentsSaveFileRequest(input: new AiAttachmentsSaveFileRequestInput(path: stored.Id, content: string.Empty, type: DocumentFileType, title: stored.Title)),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task SaveFile_FileStoredOnTheThirdPartyBackend_AttachesLikeAnyDocSpaceFile()
    {
        var roomId = await CreateNextcloudRoomAsync("TP Attach Room");
        var stored = await StoreDocumentAsync(roomId);

        var attached = await AttachAsync(stored);

        attached.Content.Should().Contain(Marker);
    }

    [Fact]
    public async Task SaveFile_NonMemberOfTheThirdPartyRoom_CannotAttachItsFileById()
    {
        var roomId = await CreateNextcloudRoomAsync("TP Attach Perm Room");
        var stored = await StoreDocumentAsync(roomId);
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AttachAsync(stored));

        exception.ErrorCode.Should().BeInRange(400, 499, "a user never invited to the room cannot read its file through save-file");
    }
}
