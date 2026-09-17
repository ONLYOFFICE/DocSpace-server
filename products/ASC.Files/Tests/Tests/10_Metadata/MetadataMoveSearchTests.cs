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
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the metadata filter after the entries change place: the search document stores the ancestor chain of the
/// entry, so a moved file must be found where it is now, and a file created in a cascading room must be found by the
/// inherited values right away.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataMoveSearchTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";

    [Fact]
    public async Task MovedFile_IsFoundByTheFilterInItsNewFolderOnly()
    {
        var data = await ArrangeAsync();

        // the value is searchable in the source folder before the move
        var before = await data.PollAsync(data.SourceFolderId, [data.FileId]);
        before.FileIds().Should().Equal(data.FileId);

        await MoveFileAsync(data.FileId, data.TargetFolderId);

        // the search document used to keep the ancestor chain of the source folder forever, so the filter kept
        // reporting the file in its old place and never in the new one
        var target = await data.PollAsync(data.TargetFolderId, [data.FileId]);
        target.FileIds().Should().Equal(new[] { data.FileId }, "the moved file must be found in its new folder");

        var source = await data.PollAsync(data.SourceFolderId, []);
        source.FileIds().Should().BeEmpty("the moved file must not be reported in its old folder any more");
    }

    [Fact]
    public async Task FileMovedToTrash_IsFoundByTheFilterInTrash()
    {
        var data = await ArrangeAsync();

        // room files are deleted for good, so the trash case is a file of "My documents"
        var myFile = await CreateFileInMy($"trashed-{Guid.NewGuid().ToString()[..8]}.docx", Owner);
        var myDocumentsId = await GetUserFolderIdAsync(Owner);

        await data.Api.AssignFileTemplatesAsync(myFile.Id, [data.TemplateId], TestContext.Current.CancellationToken);
        await data.Api.SetFileValuesAsync(myFile.Id, [data.Value("ACME")], TestContext.Current.CancellationToken);

        var before = await data.PollAsync(myDocumentsId, [myFile.Id]);
        before.FileIds().Should().Equal(myFile.Id);

        var results = (await _filesApi.DeleteFileAsync(myFile.Id, new Delete { Immediately = false }, true, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            await WaitLongOperation(results[0].Id);
        }

        var trashId = await GetTrashFolderIdAsync(Owner);

        var trash = await data.PollAsync(trashId, [myFile.Id]);
        trash.FileIds().Should().Contain(myFile.Id, "the file in the trash must be found by its metadata");
    }

    [Fact]
    public async Task FileCreatedInCascadingRoom_IsFoundByTheInheritedValue()
    {
        var data = await ArrangeAsync();

        await data.Api.AssignFolderTemplatesAsync(data.RoomId, [data.TemplateId], cascade: true, TestContext.Current.CancellationToken);
        await data.Api.SetFolderValuesAsync(data.RoomId, [data.Value("Inherited")], TestContext.Current.CancellationToken);

        // a new file inherits the room's values inside the save transaction; the inherited values used to reach the
        // index only with the next full indexing pass, so the SQL fallback and the index disagreed for a minute
        var file = await CreateFile($"inherited-{Guid.NewGuid().ToString()[..8]}.docx", data.RoomId);

        var found = await data.PollAsync(data.RoomId, [file.Id], value: "Inherited");
        found.FileIds().Should().Contain(file.Id, "the new file must be found by the value it inherited from the room");
    }

    private async Task MoveFileAsync(int fileId, int toFolderId)
    {
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(toFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            var statuses = await WaitLongOperation(results.FirstOrDefault()?.Id);
            statuses.Should().AllSatisfy(s => s.Finished.Should().BeTrue("the move operation must finish"));
        }
    }

    private async Task<MoveSearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Move " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"Move {suffix}");
        var sourceFolder = await CreateFolder($"Source {suffix}", room.Id);
        var targetFolder = await CreateFolder($"Target {suffix}", room.Id);
        var file = await CreateFile($"moved-{suffix}.docx", sourceFolder.Id);

        var data = new MoveSearchData(api, template)
        {
            RoomId = room.Id,
            SourceFolderId = sourceFolder.Id,
            TargetFolderId = targetFolder.Id,
            FileId = file.Id
        };

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [data.Value("ACME")], TestContext.Current.CancellationToken);

        return data;
    }

    private sealed class MoveSearchData(MetadataApiClient api, MetadataTemplateResponse template)
    {
        public MetadataApiClient Api { get; } = api;
        public int TemplateId { get; } = template.Id;

        public int RoomId { get; init; }
        public int SourceFolderId { get; init; }
        public int TargetFolderId { get; init; }
        public int FileId { get; init; }

        public MetadataValuePayload Value(string value)
        {
            return new MetadataValuePayload { FieldId = template.Field(ClientField).Id, StringValue = value };
        }

        /// <summary>
        /// Requests the folder content filtered by the client value, retrying until the expected files are returned.
        /// A negative expectation is only meaningful after a positive one proved the value is indexed.
        /// </summary>
        public async Task<FolderContentResponse> PollAsync(int folderId, int[] expectedFiles, string value = "ACME")
        {
            var condition = new { fieldId = template.Field(ClientField).Id, value };
            var deadline = DateTime.UtcNow.AddSeconds(20);

            while (true)
            {
                var content = await Api.GetFolderContentAsync(folderId, TemplateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

                if (content.FileIds().Order().SequenceEqual(expectedFiles.Order()) || DateTime.UtcNow > deadline)
                {
                    return content;
                }

                await Task.Delay(300, TestContext.Current.CancellationToken);
            }
        }
    }
}
