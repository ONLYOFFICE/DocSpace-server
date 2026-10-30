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
/// Covers the templates assigned to a folder: the plain assignment, the cascading one with its conflict modes and its
/// progress, the switch of the cascade of an assigned template and the removal of the assignment.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFolderAssignmentTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task AssignFolderTemplates_WithoutCascade_ReportsACompletedOperation_AndLeavesTheContentAlone()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var file = await CreateFile($"doc-{Suffix()}.docx", folder.Id);
        var template = await CreateTemplateAsync([new MetadataFieldRequest("a", MetadataFieldType.String), new MetadataFieldRequest("b", MetadataFieldType.Number)]);

        var operation = (await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken)).Response;

        operation.IsCompleted.Should().BeTrue();
        operation.Id.Should().BeNullOrEmpty("nothing was queued");

        var assigned = (await GetFolderMetadataAsync(folder.Id)).Templates.Single(t => t.Id == template.Id);

        assigned.Cascade.Should().BeFalse();
        assigned.Fields.Select(f => f.Id).Should().Equal(template.Fields.Select(f => f.Id));
        (await GetFileMetadataAsync(file.Id)).Templates.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AssignFolderTemplates_IsAdditiveAndIdempotent_AndAnUnknownTemplateAssignsNothing()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var other = await CreateFolderInMy($"other-{Suffix()}", Owner);
        var first = await CreateTemplateAsync();
        var second = await CreateTemplateAsync();

        await AssignToFolderAsync(folder.Id, first.Id);
        await AssignToFolderAsync(folder.Id, first.Id);
        await AssignToFolderAsync(folder.Id, second.Id);

        (await GetFolderMetadataAsync(folder.Id)).Templates.Select(t => t.Id).Should().BeEquivalentTo([first.Id, second.Id]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AssignToFolderAsync(other.Id, first.Id, MissingId));

        exception.ErrorCode.Should().Be(404);
        (await GetFolderMetadataAsync(other.Id)).Templates.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AssignFolderTemplates_ToAnUnknownFolder_ReturnsNotFound()
    {
        var template = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AssignToFolderAsync(MissingId, template.Id));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Cascade_CopiesTheTemplateAndTheFolderValuesToTheNestedFilesAndFolders()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var sub = await CreateFolder($"sub-{Suffix()}", root.Id);
        var deep = await CreateFolder($"deep-{Suffix()}", sub.Id);
        var fileA = await CreateFile($"a-{Suffix()}.docx", root.Id);
        var fileB = await CreateFile($"b-{Suffix()}.docx", deep.Id);
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        await AssignToFolderAsync(root.Id, template.Id);
        await SetFolderStringAsync(root.Id, fieldId, "from root");

        // before the cascade nothing below the root carries the template
        (await GetFileMetadataAsync(fileA.Id)).Templates.Should().BeNullOrEmpty();
        (await GetFileMetadataAsync(fileB.Id)).Templates.Should().BeNullOrEmpty();
        (await GetFolderMetadataAsync(sub.Id)).Templates.Should().BeNullOrEmpty();
        (await GetFolderMetadataAsync(deep.Id)).Templates.Should().BeNullOrEmpty();

        var started = (await _metadataApi.AssignFolderTemplatesAsync(root.Id,
            new AssignMetadataTemplates([template.Id], cascade: true, MetadataConflictResolveType.Overwrite), TestContext.Current.CancellationToken)).Response;

        started.TemplateIds.Should().Equal(template.Id);
        started.Id.Should().NotBeNullOrEmpty();

        var done = await WaitForCascadeAsync(root.Id, started.Id);

        done.IsCompleted.Should().BeTrue();

        ValueOf(await GetFileMetadataAsync(fileA.Id), template.Id, fieldId)!.StringValue.Should().Be("from root");
        ValueOf(await GetFileMetadataAsync(fileB.Id), template.Id, fieldId)!.StringValue.Should().Be("from root");
        ValueOf(await GetFolderMetadataAsync(sub.Id), template.Id, fieldId)!.StringValue.Should().Be("from root");
        ValueOf(await GetFolderMetadataAsync(deep.Id), template.Id, fieldId)!.StringValue.Should().Be("from root");

        var rootTemplate = (await GetFolderMetadataAsync(root.Id)).Templates.Single(t => t.Id == template.Id);

        rootTemplate.Cascade.Should().BeTrue();
        rootTemplate.ConflictResolveType.Should().Be(MetadataConflictResolveType.Overwrite);
    }

    [Fact]
    public async Task Cascade_Skip_KeepsAnExistingValueBelowAndFillsTheEmptyOnes_AndOverwriteReplacesAll()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var withValue = await CreateFile($"with-{Suffix()}.docx", root.Id);
        var empty = await CreateFile($"empty-{Suffix()}.docx", root.Id);
        var subWithValue = await CreateFolder($"subv-{Suffix()}", root.Id);
        var subEmpty = await CreateFolder($"sube-{Suffix()}", root.Id);
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        // the root and two children hold their own value, two hold none
        foreach (var folderId in new[] { root.Id, subWithValue.Id, subEmpty.Id })
        {
            await AssignToFolderAsync(folderId, template.Id);
        }

        await AssignToFileAsync(withValue.Id, template.Id);
        await AssignToFileAsync(empty.Id, template.Id);
        await SetFolderStringAsync(root.Id, fieldId, "folder");
        await SetFileStringAsync(withValue.Id, fieldId, "file");
        await SetFolderStringAsync(subWithValue.Id, fieldId, "sub");

        async Task<string?[]> ReadAsync()
        {
            return
            [
                ValueOf(await GetFileMetadataAsync(withValue.Id), template.Id, fieldId)?.StringValue,
                ValueOf(await GetFileMetadataAsync(empty.Id), template.Id, fieldId)?.StringValue,
                ValueOf(await GetFolderMetadataAsync(subWithValue.Id), template.Id, fieldId)?.StringValue,
                ValueOf(await GetFolderMetadataAsync(subEmpty.Id), template.Id, fieldId)?.StringValue
            ];
        }

        (await ReadAsync()).Should().Equal("file", null, "sub", null);

        await AssignWithCascadeAsync(root.Id, [template.Id], MetadataConflictResolveType.Skip);

        (await ReadAsync()).Should().Equal("file", "folder", "sub", "folder");

        await UpdateWithCascadeAsync(root.Id, template.Id, MetadataConflictResolveType.Overwrite);

        (await ReadAsync()).Should().Equal("folder", "folder", "folder", "folder");
    }

    [Fact]
    public async Task GetCascadeProgress_OfAFolderThatNeverCascaded_ReportsACompletedOperationWithoutId()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);

        var progress = (await _metadataApi.GetCascadeProgressAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        progress.IsCompleted.Should().BeTrue();
        progress.Id.Should().BeNullOrEmpty();
        progress.TemplateIds.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetCascadeProgress_AfterACascade_KeepsReportingTheFinishedPass()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);

        for (var i = 0; i < 3; i++)
        {
            await CreateFile($"doc{i}-{Suffix()}.docx", root.Id);
        }

        var template = await CreateTemplateAsync();

        var started = (await _metadataApi.AssignFolderTemplatesAsync(root.Id, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken)).Response;

        started.Id.Should().NotBeNullOrEmpty();
        started.TemplateIds.Should().Equal(template.Id);

        var done = await WaitForCascadeAsync(root.Id, started.Id);

        done.Progress.Should().Be(100);

        // the finished pass stays reported, with the same identity
        var again = (await _metadataApi.GetCascadeProgressAsync(root.Id, TestContext.Current.CancellationToken)).Response;

        again.Id.Should().Be(started.Id);
        again.TemplateIds.Should().Equal(template.Id);
        again.IsCompleted.Should().BeTrue();
        again.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetCascadeProgress_OfAnUnknownFolder_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetCascadeProgressAsync(MissingId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Cascade_AFileCreatedAfterTheAssignment_InheritsTheTemplate()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var template = await CreateTemplateAsync();

        await AssignWithCascadeAsync(root.Id, [template.Id]);

        var file = await CreateFile($"doc-{Suffix()}.docx", root.Id);

        (await GetFileMetadataAsync(file.Id)).Templates.Select(t => t.Id).Should().Contain(template.Id);
    }

    [Fact]
    public async Task UpdateFolderTemplate_ForATemplateTheFolderDoesNotCarryOrAnUnknownOne_ReturnsNotFound()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var template = await CreateTemplateAsync();

        var notCarried = await StatusOfAsync(async () => await _metadataApi.UpdateFolderTemplateAsync(folder.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken));
        var unknown = await StatusOfAsync(async () => await _metadataApi.UpdateFolderTemplateAsync(folder.Id, MissingId, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken));

        notCarried.Should().Be(404);
        unknown.Should().Be(404);
    }

    [Fact]
    public async Task UnassignFolderTemplate_RemovesTheTemplateFromTheFolderOnly_TheCascadedContentKeepsItsOwnCopy()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var file = await CreateFile($"doc-{Suffix()}.docx", root.Id);
        var keep = await CreateTemplateAsync();
        var drop = await CreateTemplateAsync();
        var dropField = drop.Fields.Single().Id;

        await AssignToFolderAsync(root.Id, keep.Id, drop.Id);
        await SetFolderStringAsync(root.Id, dropField, "d");
        await AssignWithCascadeAsync(root.Id, [drop.Id]);

        await _metadataApi.UnassignFolderTemplateAsync(root.Id, drop.Id, TestContext.Current.CancellationToken);

        (await GetFolderMetadataAsync(root.Id)).Templates.Select(t => t.Id).Should().Equal(keep.Id);
        ValueOf(await GetFileMetadataAsync(file.Id), drop.Id, dropField)!.StringValue.Should().Be("d");
    }

    [Fact]
    public async Task UnassignFolderTemplate_ForAnUnknownFolderOrTemplate_ReturnsNotFound()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var template = await CreateTemplateAsync();

        var noFolder = await StatusOfAsync(async () => await _metadataApi.UnassignFolderTemplateAsync(MissingId, template.Id, TestContext.Current.CancellationToken));
        var noTemplate = await StatusOfAsync(async () => await _metadataApi.UnassignFolderTemplateAsync(folder.Id, MissingId, TestContext.Current.CancellationToken));

        noFolder.Should().Be(404);
        noTemplate.Should().Be(404);
    }
}
