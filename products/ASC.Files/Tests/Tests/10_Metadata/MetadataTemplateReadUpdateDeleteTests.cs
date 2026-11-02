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
/// Covers the listing and filtering of templates, the lookup of one, its partial update and its deletion, with the
/// refusals each of them answers with and the guarantee that a refused request changes nothing.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTemplateReadUpdateDeleteTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task GetTemplates_WithTheVisibleFilter_NarrowsTheListToVisibleOrHiddenTemplates()
    {
        var visible = await CreateTemplateAsync([], visible: true);
        var hidden = await CreateTemplateAsync([], visible: false);

        var onlyVisible = (await _metadataApi.GetTemplatesAsync(true, TestContext.Current.CancellationToken)).Response.Select(t => t.Id).ToList();
        var onlyHidden = (await _metadataApi.GetTemplatesAsync(false, TestContext.Current.CancellationToken)).Response.Select(t => t.Id).ToList();
        var all = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Id).ToList();

        onlyVisible.Should().Contain(visible.Id).And.NotContain(hidden.Id);
        onlyHidden.Should().Contain(hidden.Id).And.NotContain(visible.Id);
        all.Should().Contain([visible.Id, hidden.Id]);
    }

    [Fact]
    public async Task GetTemplates_ListsTheTemplatesInTheOrderOfTheirCreation()
    {
        var first = await CreateTemplateAsync();
        var second = await CreateTemplateAsync();

        var ids = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Id).ToList();

        ids.IndexOf(first.Id).Should().BeLessThan(ids.IndexOf(second.Id));
    }

    [Theory]
    [InlineData(MissingId)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTemplate_WithAnUnknownId_ReturnsNotFound(int templateId)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetTemplateAsync(templateId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateTemplate_WithoutTheName_KeepsTheName()
    {
        var template = await CreateTemplateAsync();

        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(visible: false), TestContext.Current.CancellationToken);

        var reloaded = await GetTemplateAsync(template.Id);

        reloaded.Name.Should().Be(template.Name, "the name was not mentioned in the request");
        reloaded.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTemplate_WithTheNameOfAnotherTemplate_ReturnsBadRequestAndKeepsTheName()
    {
        var first = await CreateTemplateAsync();
        var second = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateTemplateAsync(second.Id, new UpdateMetadataTemplate(first.Name.ToLowerInvariant()), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetTemplateAsync(second.Id)).Name.Should().Be(second.Name);
    }

    [Fact]
    public async Task UpdateTemplate_WithItsOwnName_Succeeds()
    {
        var template = await CreateTemplateAsync();

        var updated = (await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(template.Name), TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be(template.Name);
    }

    [Fact]
    public async Task UpdateTemplate_WithATooLongName_ReturnsBadRequestAndKeepsTheName()
    {
        var template = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(new string('x', MetadataService.MaxNameLength + 1)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetTemplateAsync(template.Id)).Name.Should().Be(template.Name);
    }

    [Fact]
    public async Task UpdateTemplate_WithAnUnknownId_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateTemplateAsync(MissingId, new UpdateMetadataTemplate("x"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteTemplate_RemovesItFromTheTemplateLookupAndTheList()
    {
        var template = await CreateTemplateAsync();

        await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Id).Should().NotContain(template.Id);
    }

    [Fact]
    public async Task DeleteTemplate_Twice_ReturnsNotFoundTheSecondTime()
    {
        var template = await CreateTemplateAsync();

        await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteTemplate_WithAnUnknownId_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteTemplateAsync(MissingId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
