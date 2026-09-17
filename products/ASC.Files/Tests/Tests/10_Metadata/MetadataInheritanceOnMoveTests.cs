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


namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers what happens to the metadata of an entry when it changes place. The cascade has no live link: what an entry
/// inherited stays with it when it leaves the cascading tree, and entering one stamps the entry with the templates of its
/// new ancestors under the rule the folder cascades with — Skip fills the empty fields only, Overwrite replaces the
/// entry's own values. The file move has two paths (the batch inside a room and the per-file fallback), both are covered.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataInheritanceOnMoveTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const string DepartmentField = "Department";

    private const string FolderClient = "Folder client";
    private const string FolderDepartment = "Folder department";
    private const string OwnClient = "Own client";

    /// <summary>
    /// The <c>MetadataConflictResolveType</c> values as the API takes them.
    /// </summary>
    private const int Skip = 0;
    private const int Overwrite = 1;

    [Fact]
    public async Task MovedFile_OutOfCascadingFolder_KeepsInheritedMetadata()
    {
        var data = await ArrangeAsync(Skip);

        var file = await CreateFile($"leaving-{data.Suffix}.docx", data.CascadingFolderId);

        // a new entry is stamped inside its save transaction, so the inherited values are there before the create call returns
        var inherited = await data.Api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        data.ValueOf(inherited, ClientField).Should().Be(FolderClient, "the file inherits the folder's value on creation, without any background pass");

        await MoveAsync([file.Id], [], data.PlainFolderId);

        var after = await data.Api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);

        data.ValueOf(after, ClientField).Should().Be(FolderClient, "the inherited value is materialized on the file and leaves the cascading folder together with it");
        data.ValueOf(after, DepartmentField).Should().Be(FolderDepartment);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MovedFile_IntoSkipCascadingFolder_FillsOnlyEmptyFields(bool sameRoom)
    {
        var data = await ArrangeAsync(Skip);

        var file = await CreateOwnFileAsync(data, sameRoom);

        await MoveAsync([file.Id], [], data.CascadingFolderId);

        // the batch move inside a room used to skip the stamping altogether, so the moved files never inherited anything
        var metadata = await PollMetadataAsync(data.Api, file.Id, m => data.ValueOf(m, DepartmentField) == FolderDepartment);

        data.ValueOf(metadata, DepartmentField).Should().Be(FolderDepartment, "the empty field is filled from the cascading folder");
        data.ValueOf(metadata, ClientField).Should().Be(OwnClient, "Skip keeps the value the file already had");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MovedFile_IntoOverwriteCascadingFolder_ReplacesFilledFields(bool sameRoom)
    {
        var data = await ArrangeAsync(Overwrite);

        var file = await CreateOwnFileAsync(data, sameRoom);

        await MoveAsync([file.Id], [], data.CascadingFolderId);

        // the conflict rule used to live only in the request that started the cascade, so a later move always behaved as Skip
        var metadata = await PollMetadataAsync(data.Api, file.Id, m => data.ValueOf(m, ClientField) == FolderClient);

        data.ValueOf(metadata, ClientField).Should().Be(FolderClient, "Overwrite replaces the value the file already had");
        data.ValueOf(metadata, DepartmentField).Should().Be(FolderDepartment);
    }

    [Fact]
    public async Task MovedFolder_IntoCascadingFolder_SubtreeIsStamped()
    {
        var data = await ArrangeAsync(Skip);

        var moving = await CreateFolder($"Moving {data.Suffix}", data.PlainFolderId);
        var nested = await CreateFile($"nested-{data.Suffix}.docx", moving.Id);

        await MoveAsync([], [moving.Id], data.CascadingFolderId);

        // the moved folder is stamped inside the move transaction, its content by a background pass over the subtree
        var folderMetadata = await PollMetadataAsync(data.Api, moving.Id, m => data.ValueOf(m, ClientField) == FolderClient, isFolder: true);
        data.ValueOf(folderMetadata, ClientField).Should().Be(FolderClient, "the moved folder itself inherits the cascade");

        var fileMetadata = await PollMetadataAsync(data.Api, nested.Id, m => data.ValueOf(m, ClientField) == FolderClient, TimeSpan.FromSeconds(30));
        data.ValueOf(fileMetadata, ClientField).Should().Be(FolderClient, "the file inside the moved folder is stamped by the background pass");
    }

    [Fact]
    public async Task MovedCascadingFolder_IntoOverwriteCascadingFolder_KeepsItsOwnCascadeForItsSubtree()
    {
        var data = await ArrangeAsync(Overwrite);

        // the moving folder cascades the same template with its own Client and an empty Department
        var moving = await CreateFolder($"Moving {data.Suffix}", data.PlainFolderId);
        await data.Api.AssignFolderTemplatesAsync(moving.Id, [data.TemplateId], cascade: false, TestContext.Current.CancellationToken);
        await data.Api.SetFolderValuesAsync(moving.Id, [data.Value(ClientField, OwnClient)], TestContext.Current.CancellationToken);
        await data.Api.AssignFolderTemplatesAsync(moving.Id, [data.TemplateId], cascade: true, TestContext.Current.CancellationToken);

        var nested = await CreateFile($"nested-{data.Suffix}.docx", moving.Id);

        var inherited = await data.Api.GetFileMetadataAsync(nested.Id, TestContext.Current.CancellationToken);
        data.ValueOf(inherited, ClientField).Should().Be(OwnClient, "the file inherits the moving folder's value on creation");

        await MoveAsync([], [moving.Id], data.CascadingFolderId);

        // the stamp pass over the moved subtree used to ignore the nested cascades: with Overwrite at the destination it
        // rewrote every entry below the moving folder with the destination's values, although the moving folder is the
        // nearest cascading ancestor of its own subtree (the rule the assign pass already follows)
        var folderMetadata = await PollMetadataAsync(data.Api, moving.Id, m => data.ValueOf(m, ClientField) != null, isFolder: true);
        data.ValueOf(folderMetadata, ClientField).Should().Be(OwnClient, "the moved cascading folder keeps its own value, like a nested cascading folder does in the assign pass");

        // the pass has nothing to do for this template; the deadline is the only way to be sure it did not touch the file
        var fileMetadata = await PollMetadataAsync(data.Api, nested.Id, m => data.ValueOf(m, ClientField) == FolderClient, TimeSpan.FromSeconds(10));
        data.ValueOf(fileMetadata, ClientField).Should().Be(OwnClient, "the file below the moved cascading folder keeps the value of its nearest cascading ancestor");
        data.ValueOf(fileMetadata, DepartmentField).Should().BeNull("the template is excluded from the destination's pass as a whole, its empty fields included");
    }

    [Fact]
    public async Task MovedFile_OutOfCascadingFolder_OwnsTheInheritedTemplate_SoACopyCarriesIt()
    {
        var data = await ArrangeAsync(Skip);

        var file = await CreateFile($"owned-{data.Suffix}.docx", data.CascadingFolderId);

        await MoveAsync([file.Id], [], data.PlainFolderId);

        var target = await CreateFolder($"Copy target {data.Suffix}", data.RoomId);

        await CopyAsync(file.Id, target.Id);

        var copy = await FindFileAsync(data.Api, target.Id, file.Title);

        // only the direct assignments are copied. The link used to keep pointing at the cascading folder the file had left,
        // so the copy came without the template until somebody un-cascaded that folder and the link turned direct by accident
        var copyMetadata = await PollMetadataAsync(data.Api, copy.Id, m => data.ValueOf(m, ClientField) != null);
        data.ValueOf(copyMetadata, ClientField).Should().Be(FolderClient, "what the file inherited became its own when it left the cascading folder, so the copy carries it");
    }

    [Fact]
    public async Task MovedFolder_OutOfCascadingFolder_ItsContentOwnsTheInheritedTemplate()
    {
        var data = await ArrangeAsync(Skip);

        var moving = await CreateFolder($"Moving out {data.Suffix}", data.CascadingFolderId);
        var nested = await CreateFile($"nested-out-{data.Suffix}.docx", moving.Id);

        await MoveAsync([], [moving.Id], data.PlainFolderId);

        var target = await CreateFolder($"Copy target {data.Suffix}", data.RoomId);

        await CopyAsync(nested.Id, target.Id);

        var copy = await FindFileAsync(data.Api, target.Id, nested.Title);

        // the destination cascades nothing, so no background pass runs over the moved subtree: the links of the content
        // are converted inside the move transaction, otherwise they would keep pointing at the folder left behind
        var copyMetadata = await PollMetadataAsync(data.Api, copy.Id, m => data.ValueOf(m, ClientField) != null);
        data.ValueOf(copyMetadata, ClientField).Should().Be(FolderClient, "the file below the moved folder owns what it inherited, so the copy carries it");
    }

    #region Arrange

    private async Task<InheritanceData> ArrangeAsync(int conflictResolveType)
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Inherit " + suffix,
        [
            new MetadataFieldPayload { Name = ClientField, Type = 0 },
            new MetadataFieldPayload { Name = DepartmentField, Type = 0 }
        ], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"Inherit {suffix}");
        var cascadingFolder = await CreateFolder($"Cascading {suffix}", room.Id);
        var plainFolder = await CreateFolder($"Plain {suffix}", room.Id);

        var data = new InheritanceData(api, template)
        {
            Suffix = suffix,
            RoomId = room.Id,
            CascadingFolderId = cascadingFolder.Id,
            PlainFolderId = plainFolder.Id
        };

        // the values come first, so the cascade never propagates an empty folder
        await api.AssignFolderTemplatesAsync(cascadingFolder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(cascadingFolder.Id, [data.Value(ClientField, FolderClient), data.Value(DepartmentField, FolderDepartment)], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(cascadingFolder.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType);

        return data;
    }

    /// <summary>
    /// A file holding its own Client and an empty Department. Inside the room it takes the batch move path,
    /// from "My documents" the per-file one.
    /// </summary>
    private async Task<FileDtoInteger> CreateOwnFileAsync(InheritanceData data, bool sameRoom)
    {
        var title = $"own-{data.Suffix}.docx";

        var file = sameRoom
            ? await CreateFile(title, data.PlainFolderId)
            : await CreateFileInMy(title, Owner);

        await data.Api.AssignFileTemplatesAsync(file.Id, [data.TemplateId], TestContext.Current.CancellationToken);
        await data.Api.SetFileValuesAsync(file.Id, [data.Value(ClientField, OwnClient)], TestContext.Current.CancellationToken);

        return file;
    }

    private async Task MoveAsync(int[] fileIds, int[] folderIds, int toFolderId)
    {
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(toFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = fileIds.Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList(),
            FolderIds = folderIds.Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList(),
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            var statuses = await WaitLongOperation(results.FirstOrDefault()?.Id);
            statuses.Should().AllSatisfy(s => s.Finished.Should().BeTrue("the move operation must finish"));
        }
    }

    private async Task CopyAsync(int fileId, int toFolderId)
    {
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(toFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.CopyBatchItemsAsync(copyParams, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            var statuses = await WaitLongOperation(results.FirstOrDefault()?.Id);
            statuses.Should().AllSatisfy(s => s.Finished.Should().BeTrue("the copy operation must finish"));
        }
    }

    private static async Task<RoomEntryResponse> FindFileAsync(MetadataApiClient api, int folderId, string title)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await api.GetFolderContentAsync(folderId, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken);
            var file = content.Files.FirstOrDefault(f => f.Title == title);

            if (file != null || DateTime.UtcNow > deadline)
            {
                return file ?? throw new InvalidOperationException($"The copy '{title}' did not appear in folder {folderId}.");
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<List<EntryMetadataResponse>> PollMetadataAsync(MetadataApiClient api, int entryId, Func<List<EntryMetadataResponse>, bool> until, TimeSpan? timeout = null, bool isFolder = false)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(15));

        while (true)
        {
            var metadata = isFolder
                ? await api.GetFolderMetadataAsync(entryId, TestContext.Current.CancellationToken)
                : await api.GetFileMetadataAsync(entryId, TestContext.Current.CancellationToken);

            if (until(metadata) || DateTime.UtcNow > deadline)
            {
                return metadata;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private sealed class InheritanceData(MetadataApiClient api, MetadataTemplateResponse template)
    {
        public MetadataApiClient Api { get; } = api;
        public int TemplateId { get; } = template.Id;

        public required string Suffix { get; init; }
        public int RoomId { get; init; }
        public int CascadingFolderId { get; init; }
        public int PlainFolderId { get; init; }

        public MetadataValuePayload Value(string fieldName, string value)
        {
            return new MetadataValuePayload { FieldId = template.Field(fieldName).Id, StringValue = value };
        }

        /// <summary>
        /// The string value of the template field on the entry, or <c>null</c> when the template or the value is absent.
        /// </summary>
        public string? ValueOf(List<EntryMetadataResponse> metadata, string fieldName)
        {
            var fieldId = template.Field(fieldName).Id;

            return metadata.FirstOrDefault(e => e.Template.Id == TemplateId)?.Values.FirstOrDefault(v => v.FieldId == fieldId)?.StringValue;
        }
    }

    #endregion
}
