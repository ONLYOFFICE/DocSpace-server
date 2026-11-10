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
/// Covers the templates assigned to a file: the assignment and its removal, and what the deletion of a template or of
/// one of its fields does to the files that carry it.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFileAssignmentTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task AssignFileTemplates_ShowsTheTemplateWithEmptyValues()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync([new MetadataFieldRequest("A", MetadataFieldType.String), new MetadataFieldRequest("B", MetadataFieldType.Number)]);

        var before = await GetFileMetadataAsync(file.Id);

        before.Templates.Should().BeNullOrEmpty();
        before.CustomFields.Should().BeNullOrEmpty();

        await AssignToFileAsync(file.Id, template.Id);

        var assigned = (await GetFileMetadataAsync(file.Id)).Templates.Single(t => t.Id == template.Id);

        assigned.Cascade.Should().BeFalse();
        assigned.Fields.Select(f => f.Id).Should().Equal(template.Fields.Select(f => f.Id));
        assigned.Fields.Should().OnlyContain(f => f.Value == null);
    }

    [Fact]
    public async Task AssignFileTemplates_IsIdempotentAndAdditive_AndAnEmptyListChangesNothing()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var first = await CreateTemplateAsync();
        var second = await CreateTemplateAsync();

        await AssignToFileAsync(file.Id, first.Id);
        await AssignToFileAsync(file.Id, first.Id);
        await AssignToFileAsync(file.Id, second.Id);
        await AssignToFileAsync(file.Id);

        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public async Task AssignFileTemplates_WithAnUnknownTemplate_ReturnsNotFoundAndAssignsNothing()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AssignToFileAsync(file.Id, template.Id, MissingId));

        exception.ErrorCode.Should().Be(404);
        (await GetFileMetadataAsync(file.Id)).Templates.Should().BeNullOrEmpty("the known template is not assigned either");
    }

    [Fact]
    public async Task AssignFileTemplates_ToAnUnknownFile_ReturnsNotFound()
    {
        var template = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AssignToFileAsync(MissingId, template.Id));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UnassignFileTemplate_RemovesTheTemplateAndItsValues_AndKeepsTheOthers()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var first = await CreateTemplateAsync();
        var second = await CreateTemplateAsync();
        var firstField = first.Fields.Single().Id;
        var secondField = second.Fields.Single().Id;

        await AssignToFileAsync(file.Id, first.Id, second.Id);
        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(firstField, stringValue: "va"),
            new MetadataValueRequest(secondField, stringValue: "vb")
        ]), TestContext.Current.CancellationToken);

        await _metadataApi.UnassignFileTemplateAsync(file.Id, first.Id, TestContext.Current.CancellationToken);

        var metadata = await GetFileMetadataAsync(file.Id);

        metadata.Templates.Select(t => t.Id).Should().Equal(second.Id);
        ValueOf(metadata, second.Id, secondField)!.StringValue.Should().Be("vb");

        // assigning it again brings the template back without the value it held
        await AssignToFileAsync(file.Id, first.Id);

        ValueOf(await GetFileMetadataAsync(file.Id), first.Id, firstField).Should().BeNull();
    }

    [Fact]
    public async Task UnassignFileTemplate_NotCarriedByTheFileChangesNothing_AndAMissingTemplateReturnsNotFound()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var carried = await CreateTemplateAsync();
        var other = await CreateTemplateAsync();

        await AssignToFileAsync(file.Id, carried.Id);

        await _metadataApi.UnassignFileTemplateAsync(file.Id, other.Id, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UnassignFileTemplateAsync(file.Id, MissingId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().Equal(carried.Id);
    }

    [Fact]
    public async Task DeleteTemplate_AssignedToAFile_RemovesItAndItsValuesFromTheFile()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var keep = await CreateTemplateAsync();
        var drop = await CreateTemplateAsync();
        var keepField = keep.Fields.Single().Id;

        await AssignToFileAsync(file.Id, keep.Id, drop.Id);
        await SetFileStringAsync(file.Id, keepField, "k");

        await _metadataApi.DeleteTemplateAsync(drop.Id, TestContext.Current.CancellationToken);

        var metadata = await GetFileMetadataAsync(file.Id);

        metadata.Templates.Select(t => t.Id).Should().Equal(keep.Id);
        ValueOf(metadata, keep.Id, keepField)!.StringValue.Should().Be("k");
    }

    [Fact]
    public async Task DeleteField_OfATemplateAssignedToAFile_DropsItsValuesAndKeepsTheOtherFields()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync([new MetadataFieldRequest("a", MetadataFieldType.String), new MetadataFieldRequest("b", MetadataFieldType.String)]);
        var a = template.Field("a").Id;
        var b = template.Field("b").Id;

        await AssignToFileAsync(file.Id, template.Id);
        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(a, stringValue: "1"), new MetadataValueRequest(b, stringValue: "2")]), TestContext.Current.CancellationToken);

        await _metadataApi.DeleteFieldAsync(template.Id, a, TestContext.Current.CancellationToken);

        var entry = (await GetFileMetadataAsync(file.Id)).Templates.Single(t => t.Id == template.Id);

        entry.Fields.Select(f => f.Id).Should().Equal(b);
        entry.Fields.Single().Value!.StringValue.Should().Be("2");
    }
}
