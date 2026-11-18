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
/// Covers a move to Trash, which stamps nothing: an entry that leaves a cascading folder for good owns what it inherited
/// (a copy carries it), an entry sent to Trash does not, so after the restore its inherited template is still the
/// cascade's and a copy of it carries nothing. Room entries are deleted for good, so the folders are in My documents.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTrashMoveTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    private const string Inherited = "Inherited value";

    [Fact]
    public async Task FileTrashedAndRestored_KeepsItsTemplateInherited_SoACopyCarriesNothing()
    {
        var data = await ArrangeAsync();
        var file = await CreateFile($"trashed-{data.Suffix}.docx", data.CascadingFolderId);
        ValueOf(await GetFileMetadataAsync(file.Id), data.Template.Id, data.FieldId)!.StringValue.Should().Be(Inherited, "the file inherits on creation");

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false });

        var trashed = await GetFileMetadataAsync(file.Id);
        ValueOf(trashed, data.Template.Id, data.FieldId)!.StringValue.Should().Be(Inherited, "the trash keeps the metadata");

        await MoveAndWaitAsync([file.Id], [], data.CascadingFolderId);
        await CopyAndWaitAsync([file.Id], [], data.TargetFolderId);

        var copy = await FindAsync(data.TargetFolderId, c => c.Files.FirstOrDefault(f => f.Title == file.Title));
        var copyMetadata = await GetFileMetadataAsync(copy.Id);

        // only the direct assignments are copied: had the move to Trash made the inherited link the file's own, as a
        // move out of the cascading folder does, the copy would carry the template
        copyMetadata.Templates.Should().BeNullOrEmpty("the trip through the trash left the link inherited");
    }

    [Fact]
    public async Task FolderTrashedAndRestored_LeavesItsContentInherited_SoACopyCarriesNothing()
    {
        var data = await ArrangeAsync();
        var folder = await CreateFolder($"Trashed {data.Suffix}", data.CascadingFolderId);
        var file = await CreateFile($"nested-{data.Suffix}.docx", folder.Id);
        ValueOf(await GetFileMetadataAsync(file.Id), data.Template.Id, data.FieldId)!.StringValue.Should().Be(Inherited);

        await DeleteAndWaitAsync(new DeleteBatchRequestDto { FolderIds = [new(folder.Id)], Immediately = false });
        await MoveAndWaitAsync([], [folder.Id], data.CascadingFolderId);

        ValueOf(await GetFileMetadataAsync(file.Id), data.Template.Id, data.FieldId)!.StringValue.Should().Be(Inherited, "the restored content keeps the value");

        await CopyAndWaitAsync([file.Id], [], data.TargetFolderId);

        var copy = await FindAsync(data.TargetFolderId, c => c.Files.FirstOrDefault(f => f.Title == file.Title));
        var copyMetadata = await GetFileMetadataAsync(copy.Id);

        copyMetadata.Templates.Should().BeNullOrEmpty("a folder sent to Trash does not convert the inherited links of its content");
    }

    private async Task<TrashData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        var cascading = await CreateFolderInMy($"Cascading {suffix}", Owner);
        var target = await CreateFolderInMy($"Target {suffix}", Owner);

        // the value comes first, so the cascade never propagates an empty folder
        await AssignToFolderAsync(cascading.Id, template.Id);
        await SetFolderStringAsync(cascading.Id, fieldId, Inherited);
        await AssignWithCascadeAsync(cascading.Id, [template.Id]);

        return new TrashData(suffix, template, fieldId, cascading.Id, target.Id);
    }

    private async Task DeleteAndWaitAsync(DeleteBatchRequestDto request)
    {
        request.ReturnSingleOperation = true;

        await WaitAsync((await _filesOperationsApi.DeleteBatchItemsAsync(request, TestContext.Current.CancellationToken)).Response);
    }

    /// <summary>
    /// A move out of Trash back to where the entries were: the restore.
    /// </summary>
    private async Task MoveAndWaitAsync(int[] fileIds, int[] folderIds, int toFolderId)
    {
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(BatchOf(fileIds, folderIds, toFolderId), TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    private async Task CopyAndWaitAsync(int[] fileIds, int[] folderIds, int toFolderId)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(BatchOf(fileIds, folderIds, toFolderId), TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    private static BatchRequestDto BatchOf(int[] fileIds, int[] folderIds, int toFolderId)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(toFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = fileIds.Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList(),
            FolderIds = folderIds.Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList(),
            ReturnSingleOperation = true
        };
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
            var found = select(await _foldersApi.GetFolderContentWithMetadataAsync(folderId, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken));

            if (found != null || DateTime.UtcNow > deadline)
            {
                return found ?? throw new InvalidOperationException($"The entry did not appear in folder {folderId}.");
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private sealed record TrashData(string Suffix, MetadataTemplateDto Template, int FieldId, int CascadingFolderId, int TargetFolderId);
}
