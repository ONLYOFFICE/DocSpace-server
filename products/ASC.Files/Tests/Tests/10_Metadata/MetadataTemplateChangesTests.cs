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
/// Covers the template as the entries see it: its authorship, what a rename or a deletion does to the files and
/// folders that carry it, the metadata of a room, and the inheritance of a cascaded template.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTemplateChangesTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task GetTemplate_CarriesTheAuthorTheDatesAndTheFieldsInDisplayOrder()
    {
        var created = await CreateTemplateAsync([new MetadataFieldRequest("first", MetadataFieldType.String), new MetadataFieldRequest("second", MetadataFieldType.Number)]);

        var template = await GetTemplateAsync(created.Id);

        template.CreateBy.Id.Should().Be(Owner.Id);
        template.CreateBy.DisplayName.Should().NotBeNullOrEmpty();
        template.ModifiedBy.Id.Should().Be(template.CreateBy.Id);
        template.CreateOn.Should().NotBeNull();
        template.ModifiedOn.Should().NotBeNull();
        template.Visible.Should().BeTrue();
        template.Fields.Select(f => f.Order).Should().BeInAscendingOrder();
        template.Fields.Should().OnlyContain(f => f.TemplateId == created.Id);
    }

    [Fact]
    public async Task UpdateTemplate_ByAnotherAdmin_IsRecordedInModifiedBy_AndTheCreatorStays()
    {
        var template = await CreateTemplateAsync();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate("By admin " + Suffix()), TestContext.Current.CancellationToken);
        await _filesClient.Authenticate(Owner);

        var reloaded = await GetTemplateAsync(template.Id);

        reloaded.CreateBy.Id.Should().Be(Owner.Id);
        reloaded.ModifiedBy.Id.Should().Be(admin.Id);
    }

    [Fact]
    public async Task UpdateTemplate_RenamingAnAssignedTemplate_KeepsItsValuesOnTheFileAndTheFolder()
    {
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var file = await CreateFile($"doc-{Suffix()}.docx", folder.Id);

        await AssignToFolderAsync(folder.Id, template.Id);
        await AssignToFileAsync(file.Id, template.Id);
        await SetFolderStringAsync(folder.Id, fieldId, "f");
        await SetFileStringAsync(file.Id, fieldId, "g");

        var newName = "Renamed " + Suffix();

        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(newName), TestContext.Current.CancellationToken);

        var fileMetadata = await GetFileMetadataAsync(file.Id);
        var folderMetadata = await GetFolderMetadataAsync(folder.Id);

        fileMetadata.Templates.Single().Name.Should().Be(newName);
        folderMetadata.Templates.Single().Name.Should().Be(newName);
        ValueOf(fileMetadata, template.Id, fieldId)!.StringValue.Should().Be("g");
        ValueOf(folderMetadata, template.Id, fieldId)!.StringValue.Should().Be("f");
    }

    [Fact]
    public async Task DeleteTemplateAndField_FolderLosesTheTemplateOrTheFieldValue_AndKeepsTheRest()
    {
        var keep = await CreateTemplateAsync([new MetadataFieldRequest("a", MetadataFieldType.String), new MetadataFieldRequest("b", MetadataFieldType.String)]);
        var drop = await CreateTemplateAsync();
        var a = keep.Field("a").Id;
        var b = keep.Field("b").Id;
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);

        await AssignToFolderAsync(folder.Id, keep.Id, drop.Id);
        await _metadataApi.SetFolderValuesAsync(folder.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(a, stringValue: "1"),
            new MetadataValueRequest(b, stringValue: "2"),
            new MetadataValueRequest(drop.Fields.Single().Id, stringValue: "3")
        ]), TestContext.Current.CancellationToken);

        await _metadataApi.DeleteFieldAsync(keep.Id, a, TestContext.Current.CancellationToken);
        await _metadataApi.DeleteTemplateAsync(drop.Id, TestContext.Current.CancellationToken);

        var metadata = await GetFolderMetadataAsync(folder.Id);

        metadata.Templates.Select(t => t.Id).Should().Equal(keep.Id);
        metadata.Templates.Single().Fields.Select(f => f.Id).Should().Equal(b);
        ValueOf(metadata, keep.Id, b)!.StringValue.Should().Be("2");
    }

    [Fact]
    public async Task Room_TakesTemplatesValuesAndCustomFieldsLikeAFolder_AndCascadesToItsFiles()
    {
        var room = await CreateCustomRoom("Metadata room " + Suffix());
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        await AssignToFolderAsync(room.Id, template.Id);
        await SetFolderStringAsync(room.Id, fieldId, "room");
        await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest("k", "v")]), TestContext.Current.CancellationToken);

        var metadata = await GetFolderMetadataAsync(room.Id);

        ValueOf(metadata, template.Id, fieldId)!.StringValue.Should().Be("room");
        metadata.CustomFields.Select(f => (f.Name, f.Value)).Should().Equal(("k", "v"));

        // the cascade of a room reaches the files inside
        var file = await CreateFile($"doc-{Suffix()}.docx", room.Id);

        await AssignWithCascadeAsync(room.Id, [template.Id]);

        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().Contain(template.Id);
    }

    [Fact]
    public async Task UnassignFileTemplate_OfAnInheritedTemplate_Works_AndALaterCascadeAssignsItAgain()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var template = await CreateTemplateAsync();

        await AssignWithCascadeAsync(root.Id, [template.Id]);

        var file = await CreateFile($"doc-{Suffix()}.docx", root.Id);

        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().Equal(template.Id);

        await _metadataApi.UnassignFileTemplateAsync(file.Id, template.Id, TestContext.Current.CancellationToken);

        (await GetFileMetadataAsync(file.Id)).Templates.Should().BeNullOrEmpty();

        await UpdateWithCascadeAsync(root.Id, template.Id);

        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().Equal(template.Id);
    }

    [Fact]
    public async Task UpdateFolderTemplate_CascadeFromAnInheritedSubfolder_MakesTheAssignmentItsOwn()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var sub = await CreateFolder($"sub-{Suffix()}", root.Id);
        var template = await CreateTemplateAsync();

        await AssignWithCascadeAsync(root.Id, [template.Id]);

        (await GetFolderMetadataAsync(sub.Id)).Templates.Single(t => t.Id == template.Id).Cascade.Should().BeFalse("the sub-folder inherits the template without cascading it");

        await UpdateWithCascadeAsync(sub.Id, template.Id);

        (await GetFolderMetadataAsync(sub.Id)).Templates.Single(t => t.Id == template.Id).Cascade.Should().BeTrue();
        (await GetFolderMetadataAsync(root.Id)).Templates.Single(t => t.Id == template.Id).Cascade.Should().BeTrue("the cascade of the root is not affected");
    }
}
