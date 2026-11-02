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
/// Covers the creation of a template: the fields of every type, the names it accepts and the ones it refuses,
/// and that a refused request creates nothing.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTemplateCreateTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task CreateTemplate_WithEveryFieldType_ReadsBackWithItsOptions()
    {
        var name = "Every type " + Suffix();

        var created = await CreateTemplateAsync(
        [
            new MetadataFieldRequest("Title", MetadataFieldType.String),
            new MetadataFieldRequest("Due", MetadataFieldType.Date),
            new MetadataFieldRequest("Amount", MetadataFieldType.Number),
            new MetadataFieldRequest("Status", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "Open"), new MetadataFieldOptionRequest(value: "Closed")]),
            new MetadataFieldRequest("Tags", MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "a"), new MetadataFieldOptionRequest(value: "b")])
        ], name);

        created.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be(name);
        created.Visible.Should().BeTrue();
        created.Fields.Should().HaveCount(5);

        var template = await GetTemplateAsync(created.Id);

        template.Name.Should().Be(name);
        template.Field("Due").Type.Should().Be(MetadataFieldType.Date);
        template.Field("Amount").Type.Should().Be(MetadataFieldType.Number);
        template.Field("Status").Options.Select(o => o.Value).Should().Equal("Open", "Closed");
        template.Field("Status").Options.Should().OnlyContain(o => o.Id != Guid.Empty, "every option gets an ID");
    }

    [Fact]
    public async Task CreateTemplate_AppearsOnceInTheList()
    {
        var template = await CreateTemplateAsync();

        var ids = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Id).ToList();

        ids.Count(id => id == template.Id).Should().Be(1);
        ids.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData("Шаблон договора")]
    [InlineData("模板 テンプレート")]
    [InlineData("A&B 'q' %20 ;")]
    public async Task CreateTemplate_WithUnicodeOrSpecialCharactersInTheName_KeepsTheName(string name)
    {
        var created = await CreateTemplateAsync([], name);

        (await GetTemplateAsync(created.Id)).Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateTemplate_WithTheNameOfAnotherInADifferentCase_ReturnsBadRequestAndCreatesNothing()
    {
        var existing = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync([], existing.Name.ToUpperInvariant()));

        exception.ErrorCode.Should().Be(400);

        var same = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response
            .Where(t => string.Equals(t.Name, existing.Name, StringComparison.OrdinalIgnoreCase));

        same.Select(t => t.Id).Should().Equal(existing.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplate_WithABlankName_ReturnsBadRequestAndCreatesNothing(string name)
    {
        var before = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Count;

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateTemplateAsync([], name));

        exception.ErrorCode.Should().Be(400);
        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Count.Should().Be(before);
    }

    [Fact]
    public async Task CreateTemplate_WithANullName_ReturnsBadRequestAndCreatesNothing()
    {
        var before = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Count;

        // raw: the name is a required non-nullable constructor parameter of the SDK model, so a null never leaves the client
        using var response = await new RawApiClient(_filesClient).PostRawAsync("api/2.0/files/metadata/templates", """{"name":null}""", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Count.Should().Be(before);
    }

    [Fact]
    public async Task CreateTemplate_WithTheLongestAllowedName_Succeeds()
    {
        var name = new string('n', MetadataService.MaxNameLength);

        var created = await CreateTemplateAsync([], name);

        created.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateTemplate_WithAnInvalidField_IsRejectedAsAWhole()
    {
        var name = "Atomic " + Suffix();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync(
            [
                new MetadataFieldRequest("ok", MetadataFieldType.String),
                new MetadataFieldRequest("bad", MetadataFieldType.SingleChoice)
            ], name));

        exception.ErrorCode.Should().Be(400, "a choice field needs options");
        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Name).Should().NotContain(name);
    }

    [Fact]
    public async Task CreateTemplate_WithOptionsOnANonChoiceField_ReturnsBadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync([new MetadataFieldRequest("f", MetadataFieldType.String, [new MetadataFieldOptionRequest(value: "x")])]));

        exception.ErrorCode.Should().Be(400);
    }
}
