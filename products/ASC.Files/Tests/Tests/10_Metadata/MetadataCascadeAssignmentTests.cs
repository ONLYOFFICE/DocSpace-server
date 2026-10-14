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
/// Covers what a client needs to drive the cascade switch of a template on a folder: the state the entry metadata
/// reports, the templates a progress reports, and the per-template update that turns the cascade on or off without
/// taking the template off the folder.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataCascadeAssignmentTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const int Overwrite = 1;

    [Fact]
    public async Task GetFolderMetadata_ReportsCascadeAndConflictMode()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("State " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"State {suffix}");
        var plain = await CreateFolder($"Plain {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(plain.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken, conflictResolveType: Overwrite);
        await PollCascadeStatusAsync(api, room.Id);

        // the switch of the UI has to show its state after a reload, which the metadata of the folder did not carry
        var roomTemplate = (await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeTrue("the room cascades the template");
        roomTemplate.ConflictResolveType.Should().Be(Overwrite, "the mode of the cascade is reported with it");

        var plainTemplate = (await api.GetFolderMetadataAsync(plain.Id, TestContext.Current.CancellationToken)).Single(t => t.Id == template.Id);
        plainTemplate.Cascade.Should().BeFalse("the sub-folder carries the template without cascading it");
        plainTemplate.ConflictResolveType.Should().BeNull("the mode means nothing without a cascade");

        var fileTemplates = await PollTemplatesAsync(api, file.Id, FileEntryType.File, [template.Id]);
        fileTemplates.Should().Contain(template.Id);

        var fileTemplate = (await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken)).Single(t => t.Id == template.Id);
        fileTemplate.Cascade.Should().BeFalse("a file never cascades");
        fileTemplate.ConflictResolveType.Should().BeNull();
    }

    [Fact]
    public async Task CascadeProgress_ReportsTheTemplateIds()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Progress " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"Progress {suffix}");
        var idle = await CreateCustomRoom($"Idle {suffix}");

        // without the ids a client with two cascading templates cannot tell which one the progress belongs to
        var started = await api.AssignFolderTemplatesWithStatusAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);
        started.TemplateIds.Should().Equal([template.Id], "the answer of the assignment names the templates of its pass");

        var status = await PollCascadeStatusAsync(api, room.Id);
        status.Should().NotBeNull();
        status!.TemplateIds.Should().Equal([template.Id], "the progress names the templates of the pass it reports");

        var none = await api.GetCascadeProgressAsync(idle.Id, TestContext.Current.CancellationToken);
        none.Should().NotBeNull();
        none!.IsCompleted.Should().BeTrue();
        none.TemplateIds.Should().BeEmpty("a folder that never cascaded has no templates in progress");
    }

    [Fact]
    public async Task UpdateFolderTemplate_CascadeOff_KeepsTheTemplateAndValues_AndStopsInheritance()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Off " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Off {suffix}");
        var file = await CreateFile($"before-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: true, TestContext.Current.CancellationToken);
        await PollCascadeStatusAsync(api, room.Id);
        (await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "ACME")).Should().NotBeEmpty();

        // the only way to stop a cascade used to be the unassignment, which also dropped the template and its values from the room
        var answer = await api.UpdateFolderTemplateAsync(room.Id, template.Id, cascade: false, TestContext.Current.CancellationToken);
        answer.IsCompleted.Should().BeTrue("turning the cascade off finishes in the request");
        answer.Id.Should().BeNull("nothing is queued when the cascade is turned off");

        var roomMetadata = await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken);
        var roomTemplate = roomMetadata.Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeFalse("the switch is off");
        roomTemplate.ConflictResolveType.Should().BeNull();
        ValueOf(roomMetadata, template.Id, clientFieldId).Should().Be("ACME", "the room keeps the template and its value");

        var fileMetadata = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("ACME", "the file below keeps the template and the inherited value as its own");

        var later = await CreateFile($"after-{suffix}.docx", room.Id);
        var laterMetadata = await api.GetFileMetadataAsync(later.Id, TestContext.Current.CancellationToken);
        laterMetadata.Should().NotContain(t => t.Id == template.Id, "a file created after the switch inherits nothing");

        // off again is a no-op, not an error
        var again = await api.UpdateFolderTemplateAsync(room.Id, template.Id, cascade: false, TestContext.Current.CancellationToken);
        again.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateFolderTemplate_CascadeOn_RunsThePass()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("On " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"On {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = clientFieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        var answer = await api.UpdateFolderTemplateAsync(room.Id, template.Id, cascade: true, TestContext.Current.CancellationToken, conflictResolveType: Overwrite);
        answer.Id.Should().NotBeNullOrEmpty("turning the cascade on queues a pass");
        answer.TemplateIds.Should().Equal([template.Id]);

        var fileMetadata = await PollMetadataAsync(api, file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "ACME", TimeSpan.FromSeconds(30));
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("ACME", "the pass copies the room's value to the file");

        var roomTemplate = (await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeTrue();
        roomTemplate.ConflictResolveType.Should().Be(Overwrite, "the mode of the request is stored with the link");

        var status = await PollCascadeStatusAsync(api, room.Id);
        status!.Error.Should().BeNullOrEmpty();
        status.TemplateIds.Should().Equal([template.Id]);
    }

    [Fact]
    public async Task UpdateFolderTemplate_OnATemplateTheFolderDoesNotCarry_ReturnsNotFound()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Absent " + suffix, [], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Absent {suffix}");

        // the call changes an assignment, it does not create one: the assignment is made with PUT .../templates
        using var response = await api.UpdateFolderTemplateResponseAsync(room.Id, template.Id, cascade: true, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var unknown = await api.UpdateFolderTemplateResponseAsync(room.Id, int.MaxValue, cascade: true, TestContext.Current.CancellationToken);

        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
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

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
