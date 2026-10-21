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
/// Covers what a client needs to drive the cascade switch of a template on a folder: the state the entry metadata
/// reports, the templates a progress reports, and the per-template update that turns the cascade on or off without
/// taking the template off the folder.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataCascadeAssignmentTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const MetadataConflictResolveType Overwrite = MetadataConflictResolveType.Overwrite;

    [Fact]
    public async Task GetFolderMetadata_ReportsCascadeAndConflictMode()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("State " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);

        var room = await CreateCustomRoom($"State {suffix}");
        var plain = await CreateFolder($"Plain {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(plain.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: true, Overwrite), TestContext.Current.CancellationToken);
        await PollCascadeStatusAsync(room.Id);

        // the switch of the UI has to show its state after a reload, which the metadata of the folder did not carry
        var roomTemplate = (await GetFolderTemplatesAsync(room.Id)).Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeTrue("the room cascades the template");
        roomTemplate.ConflictResolveType.Should().Be(Overwrite, "the mode of the cascade is reported with it");

        var plainTemplate = (await GetFolderTemplatesAsync(plain.Id)).Single(t => t.Id == template.Id);
        plainTemplate.Cascade.Should().BeFalse("the sub-folder carries the template without cascading it");
        plainTemplate.ConflictResolveType.Should().BeNull("the mode means nothing without a cascade");

        var fileTemplates = await PollTemplatesAsync(file.Id, FileEntryType.File, [template.Id]);
        fileTemplates.Should().Contain(template.Id);

        var fileTemplate = (await GetFileTemplatesAsync(file.Id)).Single(t => t.Id == template.Id);
        fileTemplate.Cascade.Should().BeFalse("a file never cascades");
        fileTemplate.ConflictResolveType.Should().BeNull();
    }

    [Fact]
    public async Task CascadeProgress_ReportsTheTemplateIds()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("Progress " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);

        var room = await CreateCustomRoom($"Progress {suffix}");
        var idle = await CreateCustomRoom($"Idle {suffix}");

        // without the ids a client with two cascading templates cannot tell which one the progress belongs to
        var started = (await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken)).Response;
        started.TemplateIds.Should().Equal([template.Id], "the answer of the assignment names the templates of its pass");

        var status = await PollCascadeStatusAsync(room.Id);
        status.Should().NotBeNull();
        status!.TemplateIds.Should().Equal([template.Id], "the progress names the templates of the pass it reports");

        var none = (await _metadataApi.GetCascadeProgressAsync(idle.Id, TestContext.Current.CancellationToken)).Response;
        none.Should().NotBeNull();
        none!.IsCompleted.Should().BeTrue();
        none.TemplateIds.Should().BeEmpty("a folder that never cascaded has no templates in progress");
    }

    [Fact]
    public async Task UpdateFolderTemplate_CascadeOff_KeepsTheTemplateAndValues_AndStopsInheritance()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("Off " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Off {suffix}");
        var file = await CreateFile($"before-{suffix}.docx", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(clientFieldId, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken);
        await PollCascadeStatusAsync(room.Id);
        (await PollMetadataAsync(file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "ACME")).Should().NotBeEmpty();

        // the only way to stop a cascade used to be the unassignment, which also dropped the template and its values from the room
        var answer = (await _metadataApi.UpdateFolderTemplateAsync(room.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: false), TestContext.Current.CancellationToken)).Response;
        answer.IsCompleted.Should().BeTrue("turning the cascade off finishes in the request");
        answer.Id.Should().BeNull("nothing is queued when the cascade is turned off");

        var roomMetadata = await GetFolderTemplatesAsync(room.Id);
        var roomTemplate = roomMetadata.Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeFalse("the switch is off");
        roomTemplate.ConflictResolveType.Should().BeNull();
        ValueOf(roomMetadata, template.Id, clientFieldId).Should().Be("ACME", "the room keeps the template and its value");

        var fileMetadata = await GetFileTemplatesAsync(file.Id);
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("ACME", "the file below keeps the template and the inherited value as its own");

        var later = await CreateFile($"after-{suffix}.docx", room.Id);
        var laterMetadata = await GetFileTemplatesAsync(later.Id);
        laterMetadata.Should().NotContain(t => t.Id == template.Id, "a file created after the switch inherits nothing");

        // off again is a no-op, not an error
        var again = (await _metadataApi.UpdateFolderTemplateAsync(room.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: false), TestContext.Current.CancellationToken)).Response;
        again.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateFolderTemplate_CascadeOn_RunsThePass()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("On " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var clientFieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"On {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(clientFieldId, stringValue: "ACME")]), TestContext.Current.CancellationToken);

        var answer = (await _metadataApi.UpdateFolderTemplateAsync(room.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true, Overwrite), TestContext.Current.CancellationToken)).Response;
        answer.Id.Should().NotBeNullOrEmpty("turning the cascade on queues a pass");
        answer.TemplateIds.Should().Equal([template.Id]);

        var fileMetadata = await PollMetadataAsync(file.Id, FileEntryType.File, m => ValueOf(m, template.Id, clientFieldId) == "ACME", TimeSpan.FromSeconds(30));
        ValueOf(fileMetadata, template.Id, clientFieldId).Should().Be("ACME", "the pass copies the room's value to the file");

        var roomTemplate = (await GetFolderTemplatesAsync(room.Id)).Single(t => t.Id == template.Id);
        roomTemplate.Cascade.Should().BeTrue();
        roomTemplate.ConflictResolveType.Should().Be(Overwrite, "the mode of the request is stored with the link");

        var status = await PollCascadeStatusAsync(room.Id);
        status!.Error.Should().BeNullOrEmpty();
        status.TemplateIds.Should().Equal([template.Id]);
    }

    [Fact]
    public async Task UpdateFolderTemplate_OnATemplateTheFolderDoesNotCarry_ReturnsNotFound()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("Absent " + suffix, []);
        var room = await CreateCustomRoom($"Absent {suffix}");

        // the call changes an assignment, it does not create one: the assignment is made with PUT .../templates
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFolderTemplateAsync(room.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        var unknown = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFolderTemplateAsync(room.Id, int.MaxValue, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken));

        unknown.ErrorCode.Should().Be(404);
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<List<EntryTemplateDto>> GetFolderTemplatesAsync(int folderId)
    {
        return (await _metadataApi.GetFolderMetadataAsync(folderId, TestContext.Current.CancellationToken)).Response.Templates;
    }

    private async Task<List<EntryTemplateDto>> GetFileTemplatesAsync(int fileId)
    {
        return (await _metadataApi.GetFileMetadataAsync(fileId, TestContext.Current.CancellationToken)).Response.Templates;
    }

    /// <summary>
    /// Waits until the reported cascade operation of the folder is completed and returns it.
    /// </summary>
    private async Task<MetadataOperationDto?> PollCascadeStatusAsync(int folderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var status = (await _metadataApi.GetCascadeProgressAsync(folderId, TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true } || DateTime.UtcNow > deadline)
            {
                return status;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private async Task<List<int>> PollTemplatesAsync(int entryId, FileEntryType entryType, int[] expected)
    {
        var metadata = await PollMetadataAsync(entryId, entryType, m => expected.All(id => m.Any(e => e.Id == id)), TimeSpan.FromSeconds(30));

        return metadata.Select(e => e.Id).ToList();
    }

    private async Task<List<EntryTemplateDto>> PollMetadataAsync(int entryId, FileEntryType entryType, Func<List<EntryTemplateDto>, bool> until, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(15));

        while (true)
        {
            var metadata = entryType == FileEntryType.File
                ? await GetFileTemplatesAsync(entryId)
                : await GetFolderTemplatesAsync(entryId);

            if (until(metadata) || DateTime.UtcNow > deadline)
            {
                return metadata;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private static string? ValueOf(List<EntryTemplateDto> metadata, int templateId, int fieldId)
    {
        return metadata.FirstOrDefault(e => e.Id == templateId)?.Fields.FirstOrDefault(f => f.Id == fieldId)?.Value?.StringValue;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
