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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// <c>POST /api/2.0/ai/attachments/get</c>: one draft by id. The body is a bare JSON string literal, an unknown id is
/// <c>200 null</c> and a malformed one is a 400.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentGetTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    public async Task Get_BareJsonStringLiteral_IsTheBodyFormat()
    {
        var id = await SaveDraftAsync(Owner, "Autotest get.docx", "payload");

        var read = await GetAsync(id);

        read.Status.Should().Be(200, read.Error);
        read.Data.Should().NotBeNull($"draft {id} via POST /get");
        read.Data!.Id.Should().Be(id);
        read.Data.Content.Should().Be("payload");
    }

    [Fact]
    public async Task Get_AnIdObjectBody_BindsAsWell()
    {
        // Raw: the SDK sends the bare string; the route also accepts the legacy `{ id }` shape.
        var id = await SaveDraftAsync(Owner, "Autotest object-body.docx");

        var read = await PostRawAsync("get", Json(new { id }));

        read.Status.Should().Be(200, read.Body);
        using var document = JsonDocument.Parse(read.Body);
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Object, "an { id } body resolves the draft");
        document.RootElement.GetProperty("id").GetString().Should().Be(id);
    }

    [Fact]
    public async Task Get_UnknownId_Answers200Null()
    {
        // The route returns null for an id it cannot find, and that is the only "not found" signal it has.
        var read = await GetAsync(MissingId);

        read.Status.Should().Be(200, read.Error);
        read.Data.Should().BeNull();
    }

    [Theory]
    [InlineData("\"   \"")]
    [InlineData("null")]
    [InlineData("123")]
    [InlineData("\"3fa85f64-5717\"")]
    [InlineData("\"definitely-not-a-uuid\"")]
    [InlineData(null)]
    public async Task Get_MalformedId_Is400(string? body)
    {
        // Raw: whitespace, a truncated uuid, a number, null and no body at all, none of which the typed string carries.
        var refused = await PostRawAsync("get", body);

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    [Trait("Bug", "82756")]
    public async Task Get_EmptyId_Is400LikeEveryOtherMalformedOne()
    {
        // An empty string used to report uniquely that POST is not allowed, on a route that accepts nothing but POST.
        var refused = await GetAsync(string.Empty);

        refused.Status.Should().Be(400);
        refused.Error.Should().Be("id is required");
    }
}
