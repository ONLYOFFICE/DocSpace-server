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
/// Covers the access to the metadata of an entry: it follows the right on the file or the folder. A room manager reads
/// and writes, a read-only member reads and is refused every write, a user without access is refused everything, and an
/// anonymous caller without a link is unauthorized.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataEntryPermissionsTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task RoomManager_ReadsAndWritesTheMetadataOfTheFileAndTheFolder()
    {
        var s = await ArrangeAsync();
        var manager = await InviteContact(EmployeeType.RoomAdmin);

        await InviteToRoom(s.RoomId, manager, FileShare.RoomManager);
        await _filesClient.Authenticate(manager);

        await _metadataApi.GetFileMetadataAsync(s.FileId, TestContext.Current.CancellationToken);
        await _metadataApi.GetFolderMetadataAsync(s.FolderId, TestContext.Current.CancellationToken);

        await SetFileStringAsync(s.FileId, s.FieldId, "by manager");
        await SetFolderStringAsync(s.FolderId, s.FieldId, "by manager");
        await _metadataApi.SetFileCustomFieldsAsync(s.FileId, new SetCustomFields([new CustomFieldRequest("k", "v")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderCustomFieldsAsync(s.FolderId, new SetCustomFields([new CustomFieldRequest("k", "v")]), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);

        ValueOf(await GetFileMetadataAsync(s.FileId), s.TemplateId, s.FieldId)!.StringValue.Should().Be("by manager");
    }

    [Fact]
    public async Task ReadOnlyMember_ReadsTheMetadata_ButEveryWriteIsRefusedWith403AndChangesNothing()
    {
        var s = await ArrangeAsync();
        var reader = await InviteContact(EmployeeType.User);

        await InviteToRoom(s.RoomId, reader, FileShare.Read);

        var other = await CreateTemplateAsync();

        await _filesClient.Authenticate(reader);

        var fileMetadata = await GetFileMetadataAsync(s.FileId);

        fileMetadata.Templates.Select(t => t.Id).Should().Equal(s.TemplateId);
        (await GetFolderMetadataAsync(s.FolderId)).Templates.Select(t => t.Id).Should().Equal(s.TemplateId);

        var writes = new Dictionary<string, int>
        {
            ["assignFile"] = await StatusOfAsync(async () => await AssignToFileAsync(s.FileId, other.Id)),
            ["unassignFile"] = await StatusOfAsync(async () => await _metadataApi.UnassignFileTemplateAsync(s.FileId, s.TemplateId, TestContext.Current.CancellationToken)),
            ["setFileValues"] = await StatusOfAsync(async () => await SetFileStringAsync(s.FileId, s.FieldId, "nope")),
            ["setFileCustom"] = await StatusOfAsync(async () => await _metadataApi.SetFileCustomFieldsAsync(s.FileId, new SetCustomFields([new CustomFieldRequest("k", "v")]), TestContext.Current.CancellationToken)),
            ["assignFolder"] = await StatusOfAsync(async () => await AssignToFolderAsync(s.FolderId, other.Id)),
            ["updateFolder"] = await StatusOfAsync(async () => await _metadataApi.UpdateFolderTemplateAsync(s.FolderId, s.TemplateId, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken)),
            ["unassignFolder"] = await StatusOfAsync(async () => await _metadataApi.UnassignFolderTemplateAsync(s.FolderId, s.TemplateId, TestContext.Current.CancellationToken)),
            ["setFolderValues"] = await StatusOfAsync(async () => await SetFolderStringAsync(s.FolderId, s.FieldId, "nope")),
            ["setFolderCustom"] = await StatusOfAsync(async () => await _metadataApi.SetFolderCustomFieldsAsync(s.FolderId, new SetCustomFields([new CustomFieldRequest("k", "v")]), TestContext.Current.CancellationToken))
        };

        writes.Where(w => w.Value != 403).Should().BeEmpty("every write follows the right to edit the entry");

        await _filesClient.Authenticate(Owner);

        foreach (var metadata in new[] { await GetFileMetadataAsync(s.FileId), await GetFolderMetadataAsync(s.FolderId) })
        {
            metadata.Templates.Select(t => t.Id).Should().Equal(s.TemplateId);
            ValueOf(metadata, s.TemplateId, s.FieldId).Should().BeNull();
            metadata.CustomFields.Should().BeNullOrEmpty();
        }
    }

    [Fact]
    public async Task UserWithoutAccessToTheRoom_CannotReadOrWriteItsMetadata()
    {
        var s = await ArrangeAsync();
        var stranger = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(stranger);

        var reads = new[]
        {
            await StatusOfAsync(async () => await _metadataApi.GetFileMetadataAsync(s.FileId, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.GetFolderMetadataAsync(s.FolderId, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.GetCascadeProgressAsync(s.FolderId, TestContext.Current.CancellationToken))
        };

        reads.Should().OnlyContain(status => status == 403);

        (await StatusOfAsync(async () => await SetFileStringAsync(s.FileId, s.FieldId, "nope"))).Should().Be(403);

        await _filesClient.Authenticate(Owner);

        ValueOf(await GetFileMetadataAsync(s.FileId), s.TemplateId, s.FieldId).Should().BeNull();
    }

    [Fact]
    public async Task AnonymousCallerWithoutALink_IsUnauthorizedOnFileAndFolderMetadata()
    {
        var s = await ArrangeAsync();

        await _filesClient.Authenticate(null);

        var statuses = new[]
        {
            await StatusOfAsync(async () => await _metadataApi.GetFileMetadataAsync(s.FileId, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.GetFolderMetadataAsync(s.FolderId, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await SetFileStringAsync(s.FileId, s.FieldId, "nope"))
        };

        statuses.Should().OnlyContain(status => status == 401);
    }

    /// <summary>
    /// A room with a file and a folder, both carrying a template with one string field.
    /// </summary>
    private async Task<Scenario> ArrangeAsync()
    {
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Meta room {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);
        var folder = await CreateFolder($"Meta folder {suffix}", room.Id);
        var template = await CreateTemplateAsync();

        await AssignToFileAsync(file.Id, template.Id);
        await AssignToFolderAsync(folder.Id, template.Id);

        return new Scenario(room.Id, file.Id, folder.Id, template.Id, template.Fields.Single().Id);
    }

    private sealed record Scenario(int RoomId, int FileId, int FolderId, int TemplateId, int FieldId);
}
