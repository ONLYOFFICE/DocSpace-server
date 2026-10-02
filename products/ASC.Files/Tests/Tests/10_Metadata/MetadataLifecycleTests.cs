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
/// Covers what happens to the metadata along the life of the entries and the templates: a deleted field leaves the
/// template and the entries, a file deleted for good or dropped with the trash is gone with its metadata, a restored
/// file keeps it, a copied folder carries its own and its content's, a deleted room is gone with its metadata.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataLifecycleTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int StringType = 0;
    private const string ClientField = "Client";
    private const string DepartmentField = "Department";

    [Fact]
    public async Task DeleteField_RemovesItFromTheTemplateAndTheEntries()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Fields " + suffix,
        [
            new MetadataFieldPayload { Name = ClientField, Type = StringType },
            new MetadataFieldPayload { Name = DepartmentField, Type = StringType }
        ], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;
        var departmentFieldId = template.Field(DepartmentField).Id;
        var room = await CreateCustomRoom($"Fields {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id,
        [
            new MetadataValuePayload { FieldId = clientFieldId, StringValue = "ACME" },
            new MetadataValuePayload { FieldId = departmentFieldId, StringValue = "Legal" }
        ], TestContext.Current.CancellationToken);

        await api.DeleteFieldAsync(template.Id, departmentFieldId, TestContext.Current.CancellationToken);

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Should().ContainSingle().Which.Id.Should().Be(clientFieldId);

        var metadata = await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken);
        var entry = metadata.Should().ContainSingle(t => t.Id == template.Id).Which;
        entry.Fields.Should().ContainSingle("the deleted field leaves the entries with it").Which.Value!.StringValue.Should().Be("ACME", "the other field keeps its value");

        // a filter on the deleted field is refused, the same way an unknown one is
        using var search = await api.SearchFolderResponseAsync(room.Id, new { metadataFilters = new object[] { new { fieldId = departmentFieldId, op = "eq", value = "Legal" } } }, TestContext.Current.CancellationToken);
        search.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FileDeletedForGood_IsGoneWithItsMetadata()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Gone " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var condition = new { fieldId = template.Field(ClientField).Id, op = "eq", value = "ACME" };
        var room = await CreateCustomRoom($"Gone {suffix}");
        var file = await CreateFile($"gone-{suffix}.docx", room.Id);

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = condition.fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        (await PollFolderContentAsync(api, room.Id, template.Id, condition, c => c.FileIds().Contains(file.Id))).FileIds().Should().Contain(file.Id);

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true });

        using var response = await api.GetFileMetadataResponseAsync(file.Id, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await PollFolderContentAsync(api, room.Id, template.Id, condition, c => !c.FileIds().Contains(file.Id));
        content.FileIds().Should().NotContain(file.Id, "a deleted file is not found by the metadata it had");
    }

    [Fact]
    public async Task EmptiedTrash_DropsTheMetadataOfItsEntries()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Trash " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var condition = new { fieldId = template.Field(ClientField).Id, op = "eq", value = "ACME" };
        var value = new MetadataValuePayload { FieldId = condition.fieldId, StringValue = "ACME" };

        // room entries are deleted for good, so the trash case is a folder of "My documents"
        var myDocumentsId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolderInMy($"Trash {suffix}", Owner);
        var file = await CreateFile($"trash-{suffix}.docx", folder.Id);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [value], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [value], TestContext.Current.CancellationToken);

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FolderIds = [new(folder.Id)], Immediately = false });

        var trashed = await api.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken);
        trashed.Should().ContainSingle(t => t.Id == template.Id, "a folder in the trash keeps its metadata until the trash is emptied");

        var results = (await _filesOperationsApi.EmptyTrashAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        using var folderResponse = await api.GetFolderMetadataResponseAsync(folder.Id, TestContext.Current.CancellationToken);
        folderResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var fileResponse = await api.GetFileMetadataResponseAsync(file.Id, TestContext.Current.CancellationToken);
        fileResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await PollFolderContentAsync(api, myDocumentsId, template.Id, condition, c => !c.FolderIds().Contains(folder.Id) && !c.FileIds().Contains(file.Id));
        content.FolderIds().Should().NotContain(folder.Id, "an entry dropped with the trash is not found by the metadata it had");
        content.FileIds().Should().NotContain(file.Id);
    }

    [Fact]
    public async Task RestoredFile_KeepsItsMetadata()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Restore " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var condition = new { fieldId = template.Field(ClientField).Id, op = "eq", value = "ACME" };

        var myDocumentsId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFileInMy($"restore-{suffix}.docx", Owner);

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = condition.fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false });

        // the restore is a move out of the trash, back to where the file was
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(myDocumentsId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        var metadata = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        metadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME", "the trip through the trash changes nothing");

        var content = await PollFolderContentAsync(api, myDocumentsId, template.Id, condition, c => c.FileIds().Contains(file.Id));
        content.FileIds().Should().Contain(file.Id, "the restored file is found by its metadata where it came back to");
    }

    [Fact]
    public async Task CopiedFolder_CarriesTheMetadataOfItselfAndItsContent()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Copy " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var sourceRoom = await CreateCustomRoom($"Copy source {suffix}");
        var folder = await CreateFolder($"Copied {suffix}", sourceRoom.Id);
        var file = await CreateFile($"copied-{suffix}.docx", folder.Id);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Folder" }], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "File" }], TestContext.Current.CancellationToken);

        var targetRoom = await CreateCustomRoom($"Copy target {suffix}");

        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new BatchRequestDtoAllOfFolderIds(folder.Id)]
        }, TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        var folderCopy = await FindAsync(api, targetRoom.Id, c => c.Folders.FirstOrDefault(f => f.Title == folder.Title));
        var fileCopy = await FindAsync(api, folderCopy.Id, c => c.Files.FirstOrDefault(f => f.Title == file.Title));

        folderCopy.Id.Should().NotBe(folder.Id);

        var folderMetadata = await PollMetadataAsync(api, folderCopy.Id, FileEntryType.Folder, m => m.Any(t => t.Id == template.Id));
        folderMetadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("Folder", "the copy of the folder carries the folder's metadata");

        var fileMetadata = await PollMetadataAsync(api, fileCopy.Id, FileEntryType.File, m => m.Any(t => t.Id == template.Id));
        fileMetadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("File", "the copy of the content carries the content's metadata");

        var source = await api.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken);
        source.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("Folder", "the source is left as it was");
    }

    [Fact]
    public async Task DeletedRoom_IsGoneWithItsMetadata()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Room " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var condition = new { fieldId = template.Field(ClientField).Id, op = "eq", value = "ACME" };
        var room = await CreateCustomRoom($"Deleted {suffix}");
        var file = await CreateFile($"deleted-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = condition.fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = condition.fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        (await PollRoomsAsync(api, template.Id, condition, r => r.RoomIds().Contains(room.Id))).RoomIds().Should().Contain(room.Id);

        var operation = (await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken)).Response;
        await WaitAsync(operation.Finished ? [operation] : await WaitLongOperation(operation.Id));

        using var roomResponse = await api.GetFolderMetadataResponseAsync(room.Id, TestContext.Current.CancellationToken);
        roomResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var fileResponse = await api.GetFileMetadataResponseAsync(file.Id, TestContext.Current.CancellationToken);
        fileResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "the content of a deleted room is deleted for good");

        var rooms = await PollRoomsAsync(api, template.Id, condition, r => !r.RoomIds().Contains(room.Id));
        rooms.RoomIds().Should().NotContain(room.Id, "a deleted room is not found by the metadata it had");
    }

    #region Helpers

    private async Task DeleteAndWaitAsync(DeleteBatchRequestDto request)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(request, TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    private async Task WaitAsync(List<FileOperationDto>? results)
    {
        if (results == null || results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results?.FirstOrDefault()?.Id);
        }

        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    private static async Task<RoomEntryResponse> FindAsync(MetadataApiClient api, int folderId, Func<FolderContentResponse, RoomEntryResponse?> select)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await api.GetFolderContentAsync(folderId, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken);
            var entry = select(content);

            if (entry != null || DateTime.UtcNow > deadline)
            {
                return entry ?? throw new InvalidOperationException($"The copy did not appear in folder {folderId}.");
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Requests the folder content filtered by metadata, retrying only to absorb the indexing lag of a written or removed document.
    /// </summary>
    private static async Task<FolderContentResponse> PollFolderContentAsync(MetadataApiClient api, int folderId, int templateId, object condition, Func<FolderContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await api.GetFolderContentAsync(folderId, templateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (until(content) || DateTime.UtcNow > deadline)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<RoomsContentResponse> PollRoomsAsync(MetadataApiClient api, int templateId, object condition, Func<RoomsContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var rooms = await api.GetRoomsAsync(templateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (until(rooms) || DateTime.UtcNow > deadline)
            {
                return rooms;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<List<EntryTemplateResponse>> PollMetadataAsync(MetadataApiClient api, int entryId, FileEntryType entryType, Func<List<EntryTemplateResponse>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var metadata = entryType == FileEntryType.File
                ? await api.GetFileMetadataAsync(entryId, TestContext.Current.CancellationToken)
                : await api.GetFolderMetadataAsync(entryId, TestContext.Current.CancellationToken);

            if (until(metadata) || DateTime.UtcNow > deadline)
            {
                return metadata;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    #endregion
}
