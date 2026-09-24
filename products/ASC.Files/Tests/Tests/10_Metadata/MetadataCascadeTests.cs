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
/// Covers the cascade materialization end to end: two cascades requested back to back on the same folder
/// must both reach the subtree, and a copy into a cascading folder must keep both the copied and the inherited values.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataCascadeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const string DepartmentField = "Department";

    [Fact]
    public async Task Cascade_OverlappingTemplateSets_BothReachTheSubtree()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var first = await api.CreateTemplateAsync("Overlap A " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var second = await api.CreateTemplateAsync("Overlap B " + suffix, [new MetadataFieldPayload { Name = DepartmentField, Type = 0 }], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"Overlap {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", folder.Id);

        // two requests back to back on one folder went wrong twice: first the worker deduplicated by folder only and
        // swallowed the second request while the first was running, then {A} and {A,B} ran side by side, both passes
        // read "no link for (entry, A)" and both inserted it, so the second insert failed a whole batch
        await api.AssignFolderTemplatesAsync(room.Id, [first.Id], cascade: true, TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [first.Id, second.Id], cascade: true, TestContext.Current.CancellationToken);

        var fileTemplates = await PollTemplatesAsync(api, file.Id, FileEntryType.File, [first.Id, second.Id]);
        fileTemplates.Should().Contain([first.Id, second.Id], "both passes must reach the file");

        var folderTemplates = await PollTemplatesAsync(api, folder.Id, FileEntryType.Folder, [first.Id, second.Id]);
        folderTemplates.Should().Contain([first.Id, second.Id], "both passes must reach the sub-folder");

        var status = await PollCascadeStatusAsync(api, room.Id);
        status.Should().NotBeNull();
        status!.Error.Should().BeNullOrEmpty("neither pass may fail on the other one's inserts");
    }

    [Fact]
    public async Task Cascade_AppliesTheFolderValuesToTheWholeSubtree_NearestCascadingFolderWins()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Nearest " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Nearest {suffix}");
        var plain = await CreateFolder($"Plain {suffix}", room.Id);
        var nested = await CreateFolder($"Nested {suffix}", room.Id);
        var plainFile = await CreateFile($"plain-{suffix}.docx", plain.Id);
        var nestedFile = await CreateFile($"nested-{suffix}.docx", nested.Id);

        // the nested folder cascades the same template with its own value before the room does
        await api.AssignFolderTemplatesAsync(nested.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(nested.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Nested" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(nested.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Room" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: 1);

        var plainFolderMetadata = await PollMetadataAsync(api, plain.Id, FileEntryType.Folder, m => ValueOf(m, template.Id, clientFieldId) == "Room");
        ValueOf(plainFolderMetadata, template.Id, clientFieldId).Should().Be("Room", "the sub-folder takes the room's value");

        var plainFileMetadata = await PollMetadataAsync(api, plainFile.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "Room");
        ValueOf(plainFileMetadata, template.Id, clientFieldId).Should().Be("Room", "the file below the sub-folder takes the room's value");

        // the nested cascading folder and its subtree are excluded from the room's pass even in Overwrite mode
        var nestedFileMetadata = await PollMetadataAsync(api, nestedFile.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "Nested");
        ValueOf(nestedFileMetadata, template.Id, clientFieldId).Should().Be("Nested", "the nearest cascading ancestor wins");

        var nestedFolderMetadata = await api.GetFolderMetadataAsync(nested.Id, TestContext.Current.CancellationToken);
        ValueOf(nestedFolderMetadata, template.Id, clientFieldId).Should().Be("Nested", "the nested cascading folder keeps its own value");
    }

    [Fact]
    public async Task NewSubFolder_InACascadingRoom_IsFoundByTheMetadataFilterRightAway()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Fresh " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Fresh {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);
        await PollCascadeStatusAsync(api, room.Id);

        var folder = await CreateFolder($"Fresh sub {suffix}", room.Id);

        var metadata = await api.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken);
        ValueOf(metadata, template.Id, clientFieldId).Should().Be("ACME", "a new folder inherits the cascade inside its save transaction");

        // the inherited values used to reach the database only: the new folder had no metadata search document, and the
        // filter is served by the index whenever it answers, so the folder stayed invisible until an unrelated reindex
        var content = await PollFolderContentAsync(api, room.Id, template.Id, [new { fieldId = clientFieldId, op = "eq", value = "ACME" }], expectedFolderId: folder.Id);

        content.FolderIds().Should().Contain(folder.Id, "the new sub-folder must be searchable by its inherited metadata");
    }

    [Fact]
    public async Task Cascade_PropagatesOnlyTheFilledFolderFields_AndLinksTheTemplateRegardless()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Partial " + suffix,
        [
            new MetadataFieldPayload { Name = ClientField, Type = 0 },
            new MetadataFieldPayload { Name = DepartmentField, Type = 0 }
        ], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;
        var departmentFieldId = template.Field(DepartmentField).Id;

        var room = await CreateCustomRoom($"Empty {suffix}");
        var bare = await CreateFile($"bare-{suffix}.docx", room.Id);
        var own = await CreateFile($"own-{suffix}.docx", room.Id);

        await api.AssignFileTemplatesAsync(own.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(own.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Own" }], TestContext.Current.CancellationToken);

        // the room cascades the template with Department filled and Client left empty, in Overwrite mode
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = departmentFieldId, StringValue = "Legal" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: 1);

        var bareMetadata = await PollMetadataAsync(api, bare.Id, FileEntryType.File, m => ValueOf(m, template.Id, departmentFieldId) == "Legal");
        bareMetadata.Should().Contain(e => e.Id == template.Id, "the template is linked even where the folder has nothing to give for a field");
        ValueOf(bareMetadata, template.Id, clientFieldId).Should().BeNull("an empty folder field stays empty on the entry");

        // only the filled fields of the folder travel down: an empty one never touches the entry's own value, Overwrite or not
        var ownMetadata = await PollMetadataAsync(api, own.Id, FileEntryType.File, m => ValueOf(m, template.Id, departmentFieldId) == "Legal");
        ValueOf(ownMetadata, template.Id, clientFieldId).Should().Be("Own", "an empty folder field does not overwrite the entry's value");
        ValueOf(ownMetadata, template.Id, departmentFieldId).Should().Be("Legal");

        // a template without any value cascades as well: the entries get the link with every field empty
        var empty = await api.CreateTemplateAsync("Empty " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);

        await api.AssignFolderTemplatesAsync(room.Id, [empty.Id], cascade: true, TestContext.Current.CancellationToken);

        var emptyLinked = await PollMetadataAsync(api, bare.Id, FileEntryType.File, m => m.Any(e => e.Id == empty.Id));
        emptyLinked.Should().ContainSingle(e => e.Id == empty.Id).Which.SetFields.Should().BeEmpty("an empty instance is linked without values");
    }

    [Fact]
    public async Task EditingTheValuesOfACascadingFolder_ReachesTheSubtreeOnlyWhenTheCascadeIsRequestedAgain()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Edit " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Edit {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "v1" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);

        var first = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "v1");
        ValueOf(first, template.Id, clientFieldId).Should().Be("v1");

        // the cascade is materialized once: editing the folder afterwards changes the folder only
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "v2" }], TestContext.Current.CancellationToken);

        var status = await PollCascadeStatusAsync(api, room.Id);
        status.Should().NotBeNull();
        status!.IsCompleted.Should().BeTrue("no new pass is started by a value edit");

        var untouched = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        ValueOf(untouched, template.Id, clientFieldId).Should().Be("v1", "the existing file keeps the value it inherited before the edit");

        // an entry entering the tree now reads the folder's current values
        var newcomer = await CreateFile($"new-{suffix}.docx", room.Id);
        var newcomerMetadata = await api.GetFileMetadataAsync(newcomer.Id, TestContext.Current.CancellationToken);
        ValueOf(newcomerMetadata, template.Id, clientFieldId).Should().Be("v2", "a new file inherits the edited value");

        // Skip only fills the gaps, so the existing file is still on v1; Overwrite pushes the edit down
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: 0);
        var afterSkip = await PollCascadeStatusAsync(api, room.Id);
        afterSkip!.IsCompleted.Should().BeTrue();
        ValueOf(await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken), template.Id, clientFieldId).Should().Be("v1", "Skip keeps the value the file already holds");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: 1);
        var overwritten = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "v2");
        ValueOf(overwritten, template.Id, clientFieldId).Should().Be("v2", "Overwrite replaces the value on the existing file");
    }

    [Fact]
    public async Task Uncascade_LeavesTheSubtreeTemplatesAsDirectAssignments()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Uncascade " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Uncascade {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);

        var cascaded = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "ACME");
        ValueOf(cascaded, template.Id, clientFieldId).Should().Be("ACME", "the cascade must reach the file first");

        // the un-cascade converts the inherited links into direct ones with a single update, which used to fail
        // to translate (a null literal in SetProperty) and turned the whole request into a server error
        await api.UnassignFolderTemplateAsync(room.Id, template.Id, TestContext.Current.CancellationToken);

        var roomMetadata = await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken);
        roomMetadata.Should().NotContain(m => m.Id == template.Id, "the template is gone from the room itself");

        var fileMetadata = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        fileMetadata.Should().Contain(m => m.Id == template.Id, "the file keeps the template as a direct assignment");
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("ACME", "the inherited value stays on the file");

        // there is no bulk rollback: the previously cascaded instance is removed entry by entry, values included
        await api.UnassignFileTemplateAsync(file.Id, template.Id, TestContext.Current.CancellationToken);

        var removed = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        removed.Should().NotContain(m => m.Id == template.Id, "the per-entry unassign removes the template with its values");
    }

    [Fact]
    public async Task Cascade_OnANestedFolder_TakesOverTheInheritedLinks_SoTheRoomUncascadeLeavesThemInherited()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Takeover " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Takeover {suffix}");
        var nested = await CreateFolder($"Nested {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", nested.Id);

        // the room cascades first, so the file inherits the template from the room
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Room" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);

        var fromRoom = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "Room");
        ValueOf(fromRoom, template.Id, clientFieldId).Should().Be("Room", "the room's cascade must reach the file first");

        // then the nested folder cascades the same template and becomes the nearest source of the file
        await api.AssignFolderTemplatesAsync(nested.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(nested.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "Nested" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(nested.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: 1);

        var fromNested = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "Nested");
        ValueOf(fromNested, template.Id, clientFieldId).Should().Be("Nested", "the nested folder's cascade must reach the file");

        // the room's un-cascade converts the links still inherited from the room only; the file's link used to keep the
        // room as its source and turned into a direct assignment here although the nested folder still cascades it
        await api.UnassignFolderTemplateAsync(room.Id, template.Id, TestContext.Current.CancellationToken);

        await CopyFileAsync(file.Id, room.Id);
        var copy = await FindFileAsync(api, room.Id, file.Title);

        var copyMetadata = await api.GetFileMetadataAsync(copy.Id, TestContext.Current.CancellationToken);
        copyMetadata.Should().NotContain(m => m.Id == template.Id, "a copy carries the direct assignments only, and the file's link is inherited from the nested folder");

        var fileMetadata = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("Nested", "the file itself keeps the template inherited from the nested folder");
    }

    [Fact]
    public async Task CopyFile_IntoCascadingRoom_KeepsTheCopiedAndTheInheritedValues()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var own = await api.CreateTemplateAsync("Own " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var inherited = await api.CreateTemplateAsync("Inherited " + suffix, [new MetadataFieldPayload { Name = DepartmentField, Type = 0 }], TestContext.Current.CancellationToken);

        var sourceRoom = await CreateCustomRoom($"Source {suffix}");
        var source = await CreateFile($"source-{suffix}.docx", sourceRoom.Id);

        await api.AssignFileTemplatesAsync(source.Id, [own.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(source.Id, [new MetadataValuePayload { FieldId = own.Field(ClientField).Id, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        var targetRoom = await CreateCustomRoom($"Target {suffix}");

        await api.AssignFolderTemplatesAsync(targetRoom.Id, [inherited.Id], cascade: true, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(targetRoom.Id, [new MetadataValuePayload { FieldId = inherited.Field(DepartmentField).Id, StringValue = "Legal" }], TestContext.Current.CancellationToken);

        await CopyFileAsync(source.Id, targetRoom.Id);

        var copy = await FindFileAsync(api, targetRoom.Id, source.Title);

        // the copy is created first (and inherits the room's cascade), the source metadata is copied afterwards:
        // the second step used to wipe every value of the copy, the inherited ones included
        var metadata = await PollMetadataAsync(api, copy.Id, FileEntryType.File,
            m => m.Any(e => e.Id == own.Id) && m.Any(e => e.Id == inherited.Id && e.SetFields.Any()));

        var ownValues = metadata.Single(e => e.Id == own.Id).SetFields;
        ownValues.Should().ContainSingle().Which.Value!.StringValue.Should().Be("ACME", "the copied value must follow the copy");

        var inheritedValues = metadata.Single(e => e.Id == inherited.Id).SetFields;
        inheritedValues.Should().ContainSingle().Which.Value!.StringValue.Should().Be("Legal", "the value inherited from the cascading room must survive the copy");
    }

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
    }

    private async Task CopyFileAsync(int fileId, int toFolderId)
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

    [Fact]
    public async Task AssignFolderTemplates_WithoutCascade_ReportsACompletedOperation()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Plain " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Plain {suffix}");

        // the body used to be null: the assignment is finished in the call itself, and the answer says so
        var status = await api.AssignFolderTemplatesWithStatusAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        status.Should().BeEquivalentTo(new { Id = (string?)null, Progress = 100d, IsCompleted = true, Error = (string?)null });

        var progress = await api.GetCascadeProgressAsync(room.Id, TestContext.Current.CancellationToken);

        progress.Should().BeEquivalentTo(status, "a folder that never cascaded reports the same completed operation");
    }

    /// <summary>
    /// Waits until the reported cascade operation of the folder is completed and returns it.
    /// </summary>
    private static async Task<MetadataOperationResponse?> PollCascadeStatusAsync(MetadataApiClient api, int folderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var status = await api.GetCascadeProgressAsync(folderId, TestContext.Current.CancellationToken);

            if (status is { IsCompleted: true } || DateTime.UtcNow > deadline)
            {
                return status;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<List<int>> PollTemplatesAsync(MetadataApiClient api, int entryId, FileEntryType entryType, int[] expected)
    {
        var metadata = await PollMetadataAsync(api, entryId, entryType, m => expected.All(id => m.Any(e => e.Id == id)), TimeSpan.FromSeconds(30));

        return metadata.Select(e => e.Id).ToList();
    }

    private static async Task<List<EntryTemplateResponse>> PollMetadataAsync(MetadataApiClient api, int entryId, FileEntryType entryType, Func<List<EntryTemplateResponse>, bool> until, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(15));

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

    private static string? ValueOf(List<EntryTemplateResponse> metadata, int templateId, int fieldId)
    {
        return metadata.FirstOrDefault(e => e.Id == templateId)?.Fields.FirstOrDefault(f => f.Id == fieldId)?.Value?.StringValue;
    }

    /// <summary>
    /// Requests the room content filtered by metadata, retrying only to absorb the indexing lag of a freshly written document.
    /// </summary>
    private static async Task<FolderContentResponse> PollFolderContentAsync(MetadataApiClient api, int roomId, int templateId, object[] conditions, int expectedFolderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await api.GetFolderContentAsync(roomId, templateId, conditions, cancellationToken: TestContext.Current.CancellationToken);

            if (content.FolderIds().Contains(expectedFolderId) || DateTime.UtcNow > deadline)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
