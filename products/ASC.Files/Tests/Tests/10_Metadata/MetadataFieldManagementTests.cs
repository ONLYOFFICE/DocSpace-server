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
/// Covers the creation, the update and the deletion of the fields of a template: what a field of each type
/// reads back as, which requests are refused without adding anything, and what a refusal leaves untouched.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFieldManagementTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task CreateField_OfEveryType_IsAddedWithItsOptions()
    {
        var template = await CreateTemplateAsync([]);

        List<MetadataFieldRequest> requests =
        [
            new("s", MetadataFieldType.String),
            new("d", MetadataFieldType.Date),
            new("n", MetadataFieldType.Number),
            new("sc", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "one"), new MetadataFieldOptionRequest(value: "two")]),
            new("mc", MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "x")])
        ];

        foreach (var request in requests)
        {
            var field = (await _metadataApi.CreateFieldAsync(template.Id, request, TestContext.Current.CancellationToken)).Response;

            field.Id.Should().BeGreaterThan(0, request.Name);
            field.TemplateId.Should().Be(template.Id, request.Name);
            field.Type.Should().Be(request.Type!.Value, request.Name);
        }

        var reloaded = await GetTemplateAsync(template.Id);

        reloaded.Fields.Select(f => f.Name).Should().Equal(requests.Select(r => r.Name));
        reloaded.Field("sc").Options.Select(o => o.Value).Should().Equal("one", "two");
        reloaded.Field("sc").Options.Should().OnlyContain(o => o.Id != Guid.Empty);
    }

    [Fact]
    public async Task CreateField_WithoutAnOrder_GoesLast_AndAnExplicitNegativeOrderGoesFirst()
    {
        var template = await CreateTemplateAsync([new MetadataFieldRequest("first", MetadataFieldType.String)]);

        await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("last", MetadataFieldType.String), TestContext.Current.CancellationToken);
        await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("top", MetadataFieldType.String, order: -10), TestContext.Current.CancellationToken);

        (await GetTemplateAsync(template.Id)).Fields.Select(f => f.Name).Should().Equal("top", "first", "last");
    }

    [Theory]
    [InlineData("empty name")]
    [InlineData("whitespace name")]
    [InlineData("over-long name")]
    [InlineData("choice without options")]
    [InlineData("choice with duplicate options")]
    [InlineData("options on a number")]
    [InlineData("unknown type")]
    public async Task CreateField_WithAnInvalidField_ReturnsBadRequestAndAddsNothing(string scenario)
    {
        var template = await CreateTemplateAsync([]);

        var request = scenario switch
        {
            "empty name" => new MetadataFieldRequest("", MetadataFieldType.String),
            "whitespace name" => new MetadataFieldRequest("  ", MetadataFieldType.String),
            "over-long name" => new MetadataFieldRequest(new string('x', MetadataService.MaxNameLength + 1), MetadataFieldType.String),
            "choice without options" => new MetadataFieldRequest("c", MetadataFieldType.SingleChoice),
            "choice with duplicate options" => new MetadataFieldRequest("c", MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "a"), new MetadataFieldOptionRequest(value: "a")]),
            "options on a number" => new MetadataFieldRequest("n", MetadataFieldType.Number, [new MetadataFieldOptionRequest(value: "a")]),
            _ => new MetadataFieldRequest("u", (MetadataFieldType)99)
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.CreateFieldAsync(template.Id, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetTemplateAsync(template.Id)).Fields.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateField_OnAnUnknownTemplate_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.CreateFieldAsync(MissingId, new MetadataFieldRequest("x", MetadataFieldType.String), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateField_Rename_KeepsTheIdTheTypeAndTheOrder()
    {
        var template = await CreateTemplateAsync([new MetadataFieldRequest("Old", MetadataFieldType.Number)]);
        var field = template.Field("Old");

        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(name: "New"), TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be("New");

        var after = (await GetTemplateAsync(template.Id)).Fields.Single();

        after.Id.Should().Be(field.Id);
        after.Name.Should().Be("New");
        after.Type.Should().Be(MetadataFieldType.Number);
        after.Order.Should().Be(field.Order);
    }

    [Fact]
    public async Task UpdateField_WithTheNameOfASibling_ReturnsBadRequestAndKeepsBothNames()
    {
        var template = await CreateTemplateAsync([new MetadataFieldRequest("A", MetadataFieldType.String), new MetadataFieldRequest("B", MetadataFieldType.String)]);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(template.Id, template.Field("B").Id, new UpdateMetadataFieldRequest(name: "a"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetTemplateAsync(template.Id)).Fields.Select(f => f.Name).Should().BeEquivalentTo(["A", "B"]);
    }

    [Fact]
    public async Task UpdateField_OfAnUnknownField_ReturnsNotFound()
    {
        var template = await CreateTemplateAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(template.Id, MissingId, new UpdateMetadataFieldRequest(name: "x"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateField_WithOptions_KeepsTheIdsOfTheKnownOnesRenamesAndAddsNew()
    {
        var template = await CreateTemplateAsync(
            [new MetadataFieldRequest("c", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "a"), new MetadataFieldOptionRequest(value: "b")])]);
        var field = template.Field("c");
        var a = field.Option("a");
        var b = field.Option("b");

        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id,
            new UpdateMetadataFieldRequest(options: [new MetadataFieldOptionRequest(a, "a renamed"), new MetadataFieldOptionRequest(b, "b"), new MetadataFieldOptionRequest(value: "c")]),
            TestContext.Current.CancellationToken)).Response;

        updated.Options.Select(o => o.Value).Should().Equal("a renamed", "b", "c");
        updated.Options[0].Id.Should().Be(a);
        updated.Options[1].Id.Should().Be(b);
        updated.Options[2].Id.Should().NotBeEmpty();

        (await GetTemplateAsync(template.Id)).Fields.Single().Options.Select(o => o.Value).Should().Equal("a renamed", "b", "c");
    }

    [Fact]
    public async Task UpdateField_WithANewOrder_MovesTheFieldAndKeepsItsOptionsOnRename()
    {
        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest("a", MetadataFieldType.String),
            new MetadataFieldRequest("b", MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "x")]),
            new MetadataFieldRequest("c", MetadataFieldType.String)
        ]);

        await _metadataApi.UpdateFieldAsync(template.Id, template.Field("b").Id, new UpdateMetadataFieldRequest(name: "b2", order: -5), TestContext.Current.CancellationToken);

        var reloaded = await GetTemplateAsync(template.Id);

        reloaded.Fields.Select(f => f.Name).Should().Equal("b2", "a", "c");
        reloaded.Fields[0].Options.Select(o => o.Value).Should().Equal("x");
    }

    [Fact]
    public async Task UpdateField_Type_IsRefusedOnceAFileHoldsAValue()
    {
        var template = await CreateTemplateAsync();
        var field = template.Fields.Single();
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);

        await AssignToFileAsync(file.Id, template.Id);
        await SetFileStringAsync(file.Id, field.Id, "v");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(type: MetadataFieldType.Number), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetTemplateAsync(template.Id)).Fields.Single().Type.Should().Be(MetadataFieldType.String);
    }

    [Fact]
    public async Task DeleteField_RemovesOnlyTheRequestedField()
    {
        var template = await CreateTemplateAsync(
            [new MetadataFieldRequest("A", MetadataFieldType.String), new MetadataFieldRequest("B", MetadataFieldType.String), new MetadataFieldRequest("C", MetadataFieldType.String)]);

        await _metadataApi.DeleteFieldAsync(template.Id, template.Field("B").Id, TestContext.Current.CancellationToken);

        (await GetTemplateAsync(template.Id)).Fields.Select(f => f.Name).Should().Equal("A", "C");
    }

    [Fact]
    public async Task DeleteField_ThroughAnotherTemplateOrTwice_ReturnsNotFound()
    {
        var first = await CreateTemplateAsync([new MetadataFieldRequest("A", MetadataFieldType.String)]);
        var second = await CreateTemplateAsync([new MetadataFieldRequest("B", MetadataFieldType.String)]);
        var fieldId = first.Field("A").Id;

        var foreign = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteFieldAsync(second.Id, fieldId, TestContext.Current.CancellationToken));
        foreign.ErrorCode.Should().Be(404, "the field belongs to the other template");
        (await GetTemplateAsync(first.Id)).Fields.Should().HaveCount(1);

        await _metadataApi.DeleteFieldAsync(first.Id, fieldId, TestContext.Current.CancellationToken);

        var again = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteFieldAsync(first.Id, fieldId, TestContext.Current.CancellationToken));
        again.ErrorCode.Should().Be(404);
    }
}
