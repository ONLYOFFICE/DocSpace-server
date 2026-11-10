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
    private const string ClientField = "Client";
    private const string DepartmentField = "Department";

    [Fact]
    public async Task DeleteField_RemovesItFromTheTemplateAndTheEntries()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Fields " + suffix,
        [
            new MetadataFieldRequest(ClientField, MetadataFieldType.String),
            new MetadataFieldRequest(DepartmentField, MetadataFieldType.String)
        ]);
        var clientFieldId = template.Field(ClientField).Id;
        var departmentFieldId = template.Field(DepartmentField).Id;
        var room = await CreateCustomRoom($"Fields {suffix}");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(clientFieldId, stringValue: "ACME"),
            new MetadataValueRequest(departmentFieldId, stringValue: "Legal")
        ]), TestContext.Current.CancellationToken);

        await _metadataApi.DeleteFieldAsync(template.Id, departmentFieldId, TestContext.Current.CancellationToken);

        var reloaded = (await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken)).Response;
        reloaded.Fields.Should().ContainSingle().Which.Id.Should().Be(clientFieldId);

        var metadata = (await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Response.Templates;
        var entry = metadata.Should().ContainSingle(t => t.Id == template.Id).Which;
        entry.Fields.Should().ContainSingle("the deleted field leaves the entries with it").Which.Value.StringValue.Should().Be("ACME", "the other field keeps its value");

        // a filter on the deleted field is refused, the same way an unknown one is
        var search = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(room.Id,
                new FolderMetadataSearch(metadataFilters: [new MetadataFilterConditionRequest(fieldId: departmentFieldId, op: "eq", value: "Legal")]), TestContext.Current.CancellationToken));
        search.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task FileDeletedForGood_IsGoneWithItsMetadata()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Gone " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var condition = new MetadataFilterConditionRequest(fieldId: template.Field(ClientField).Id, op: "eq", value: "ACME");
        var room = await CreateCustomRoom($"Gone {suffix}");
        var file = await CreateFile($"gone-{suffix}.docx", room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await SetFileValueAsync(file.Id, condition.FieldId, "ACME");

        (await PollFolderContentAsync(room.Id, template.Id, condition, c => c.FileIds().Contains(file.Id))).FileIds().Should().Contain(file.Id);

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true });

        var response = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken));
        response.ErrorCode.Should().Be(404);

        var content = await PollFolderContentAsync(room.Id, template.Id, condition, c => !c.FileIds().Contains(file.Id));
        content.FileIds().Should().NotContain(file.Id, "a deleted file is not found by the metadata it had");
    }

    [Fact]
    public async Task EmptiedTrash_DropsTheMetadataOfItsEntries()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Trash " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var condition = new MetadataFilterConditionRequest(fieldId: template.Field(ClientField).Id, op: "eq", value: "ACME");

        // room entries are deleted for good, so the trash case is a folder of "My documents"
        var myDocumentsId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolderInMy($"Trash {suffix}", Owner);
        var file = await CreateFile($"trash-{suffix}.docx", folder.Id);

        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await SetFolderValueAsync(folder.Id, condition.FieldId, "ACME");
        await SetFileValueAsync(file.Id, condition.FieldId, "ACME");

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FolderIds = [new(folder.Id)], Immediately = false });

        var trashed = (await _metadataApi.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken)).Response.Templates;
        trashed.Should().ContainSingle(t => t.Id == template.Id, "a folder in the trash keeps its metadata until the trash is emptied");

        var results = (await _filesOperationsApi.EmptyTrashAsync(single: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        var folderResponse = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken));
        folderResponse.ErrorCode.Should().Be(404);

        var fileResponse = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken));
        fileResponse.ErrorCode.Should().Be(404);

        var content = await PollFolderContentAsync(myDocumentsId, template.Id, condition, c => !c.FolderIds().Contains(folder.Id) && !c.FileIds().Contains(file.Id));
        content.FolderIds().Should().NotContain(folder.Id, "an entry dropped with the trash is not found by the metadata it had");
        content.FileIds().Should().NotContain(file.Id);
    }

    [Fact]
    public async Task RestoredFile_KeepsItsMetadata()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Restore " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var condition = new MetadataFilterConditionRequest(fieldId: template.Field(ClientField).Id, op: "eq", value: "ACME");

        var myDocumentsId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFileInMy($"restore-{suffix}.docx", Owner);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await SetFileValueAsync(file.Id, condition.FieldId, "ACME");

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false });

        // the restore is a move out of the trash, back to where the file was
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(myDocumentsId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        var metadata = (await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken)).Response.Templates;
        metadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value.StringValue.Should().Be("ACME", "the trip through the trash changes nothing");

        var content = await PollFolderContentAsync(myDocumentsId, template.Id, condition, c => c.FileIds().Contains(file.Id));
        content.FileIds().Should().Contain(file.Id, "the restored file is found by its metadata where it came back to");
    }

    [Fact]
    public async Task CopiedFolder_CarriesTheMetadataOfItselfAndItsContent()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Copy " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var clientFieldId = template.Field(ClientField).Id;

        var sourceRoom = await CreateCustomRoom($"Copy source {suffix}");
        var folder = await CreateFolder($"Copied {suffix}", sourceRoom.Id);
        var file = await CreateFile($"copied-{suffix}.docx", folder.Id);

        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await SetFolderValueAsync(folder.Id, clientFieldId, "Folder");
        await SetFileValueAsync(file.Id, clientFieldId, "File");

        var targetRoom = await CreateCustomRoom($"Copy target {suffix}");

        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new BatchRequestDtoAllOfFolderIds(folder.Id)]
        }, TestContext.Current.CancellationToken)).Response;
        await WaitAsync(results);

        var folderCopy = await FindAsync(targetRoom.Id, c => c.Folders.FirstOrDefault(f => f.Title == folder.Title));
        var fileCopy = await FindAsync(folderCopy.Id, c => c.Files.FirstOrDefault(f => f.Title == file.Title));

        folderCopy.Id.Should().NotBe(folder.Id);

        var folderMetadata = await PollMetadataAsync(folderCopy.Id, FileEntryType.Folder, m => m.Any(t => t.Id == template.Id));
        folderMetadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value.StringValue.Should().Be("Folder", "the copy of the folder carries the folder's metadata");

        var fileMetadata = await PollMetadataAsync(fileCopy.Id, FileEntryType.File, m => m.Any(t => t.Id == template.Id));
        fileMetadata.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value.StringValue.Should().Be("File", "the copy of the content carries the content's metadata");

        var source = (await _metadataApi.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken)).Response.Templates;
        source.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value.StringValue.Should().Be("Folder", "the source is left as it was");
    }

    [Fact]
    public async Task DeletedRoom_IsGoneWithItsMetadata()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Room " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var condition = new MetadataFilterConditionRequest(fieldId: template.Field(ClientField).Id, op: "eq", value: "ACME");
        var room = await CreateCustomRoom($"Deleted {suffix}");
        var file = await CreateFile($"deleted-{suffix}.docx", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await SetFolderValueAsync(room.Id, condition.FieldId, "ACME");
        await SetFileValueAsync(file.Id, condition.FieldId, "ACME");

        (await PollRoomsAsync(template.Id, condition, r => r.RoomIds().Contains(room.Id))).RoomIds().Should().Contain(room.Id);

        var operation = (await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken)).Response;
        await WaitAsync(operation.Finished ? [operation] : await WaitLongOperation(operation.Id));

        var roomResponse = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken));
        roomResponse.ErrorCode.Should().Be(404);

        var fileResponse = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken));
        fileResponse.ErrorCode.Should().Be(404, "the content of a deleted room is deleted for good");

        var rooms = await PollRoomsAsync(template.Id, condition, r => !r.RoomIds().Contains(room.Id));
        rooms.RoomIds().Should().NotContain(room.Id, "a deleted room is not found by the metadata it had");
    }

    #region Helpers

    private async Task DeleteAndWaitAsync(DeleteBatchRequestDto request)
    {
        // only the started operation, so the wait follows it and not an older one listed first
        request.ReturnSingleOperation = true;

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

    private async Task<RoomEntryResponse> FindAsync(int folderId, Func<FolderContentResponse, RoomEntryResponse?> select)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await _foldersApi.GetFolderContentWithMetadataAsync(folderId, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken);
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
    private async Task<FolderContentResponse> PollFolderContentAsync(int folderId, int templateId, MetadataFilterConditionRequest condition, Func<FolderContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await _foldersApi.GetFolderContentWithMetadataAsync(folderId, templateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (until(content) || DateTime.UtcNow > deadline)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<RoomsContentResponse> PollRoomsAsync(int templateId, MetadataFilterConditionRequest condition, Func<RoomsContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var rooms = await _roomsApi.GetRoomsWithMetadataAsync(templateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (until(rooms) || DateTime.UtcNow > deadline)
            {
                return rooms;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<List<EntryTemplateDto>> PollMetadataAsync(int entryId, FileEntryType entryType, Func<List<EntryTemplateDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var metadata = entryType == FileEntryType.File
                ? (await _metadataApi.GetFileMetadataAsync(entryId, TestContext.Current.CancellationToken)).Response.Templates
                : (await _metadataApi.GetFolderMetadataAsync(entryId, TestContext.Current.CancellationToken)).Response.Templates;

            if (until(metadata) || DateTime.UtcNow > deadline)
            {
                return metadata;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private async Task SetFolderValueAsync(int folderId, int fieldId, string value)
    {
        await _metadataApi.SetFolderValuesAsync(folderId, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: value)]), TestContext.Current.CancellationToken);
    }

    private async Task SetFileValueAsync(int fileId, int fieldId, string value)
    {
        await _metadataApi.SetFileValuesAsync(fileId, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: value)]), TestContext.Current.CancellationToken);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    #endregion
}
