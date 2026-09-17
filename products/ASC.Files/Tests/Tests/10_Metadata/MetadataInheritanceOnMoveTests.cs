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
