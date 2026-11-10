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

namespace ASC.AI.Tests.Tests.SettingsTests;

/// <summary>
/// The vectorization (embedding) config, <c>GET|PUT /api/2.0/ai/config/vectorization</c>.
/// </summary>
/// <remarks>
/// With an AI gateway configured nobody can edit it (403 for the owner too), which is what the TypeScript suite
/// measured. The standalone stand has no gateway, so here the rule underneath applies: only DocSpace admins (the
/// owner included) manage it, everybody else gets 403, anonymous 401, and the authorization answers before the body
/// is looked at. The "403 in every portal state" case of the TypeScript suite is about the gateway and the wallet
/// and is not ported.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Settings")]
public class SettingsVectorizationPermissionsTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    private static Dictionary<string, object> ResetBody() => new() { ["type"] = 0 };

    [Theory]
    [InlineData(null, 200)]
    [InlineData(EmployeeType.DocSpaceAdmin, 200)]
    [InlineData(EmployeeType.RoomAdmin, 403)]
    [InlineData(EmployeeType.User, 403)]
    [InlineData(EmployeeType.Guest, 403)]
    public async Task GetVectorization_ByRole_IsOpenToAdminsOnly(EmployeeType? type, int expected)
    {
        if (type is not null)
        {
            await ActAsMemberAsync(type.Value);
        }

        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsGetVectorizationAsync(TestContext.Current.CancellationToken));

        status.Should().Be(expected, error);
        if (expected == 403)
        {
            error.Should().Contain("Forbidden");
        }
    }

    [Theory]
    [InlineData(null, 200)]
    [InlineData(EmployeeType.DocSpaceAdmin, 200)]
    [InlineData(EmployeeType.RoomAdmin, 403)]
    [InlineData(EmployeeType.User, 403)]
    [InlineData(EmployeeType.Guest, 403)]
    public async Task SetVectorization_ByRole_IsOpenToAdminsOnly(EmployeeType? type, int expected)
    {
        if (type is not null)
        {
            await ActAsMemberAsync(type.Value);
        }

        // `type: 0` (none) resets the provider: it needs no key and no outbound probe.
        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsSetVectorizationAsync(ResetBody(), TestContext.Current.CancellationToken));

        status.Should().Be(expected, error);
        if (expected == 403)
        {
            error.Should().Contain("Forbidden");
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task Vectorization_Anonymous_Returns401(string verb)
    {
        await _aiChatClient.Authenticate(null);

        var (status, body) = verb == "GET"
            ? await OutcomeOfAsync(() => _aiSettings.AiSettingsGetVectorizationAsync(TestContext.Current.CancellationToken))
            : await OutcomeOfAsync(() => _aiSettings.AiSettingsSetVectorizationAsync(ResetBody(), TestContext.Current.CancellationToken));

        status.Should().Be(401, body);
        body.Should().Contain("Unauthorized");
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("nullKey")]
    [InlineData("unknownField")]
    public async Task SetVectorization_RoomAdminWithAWellFormedBody_Gets403WhateverTheBodyHolds(string kind)
    {
        await ActAsMemberAsync(EmployeeType.RoomAdmin);
        var body = kind switch
        {
            "empty" => new Dictionary<string, object>(),
            "nullKey" => new() { ["key"] = null! },
            _ => new() { ["unknownField"] = 1 }
        };

        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsSetVectorizationAsync(body, TestContext.Current.CancellationToken));

        status.Should().Be(403, error);
        error.Should().Contain("Forbidden");
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("{bad")]
    public async Task SetVectorization_RoomAdminWithABodyThatIsNotAnObject_Gets400(string json)
    {
        await ActAsMemberAsync(EmployeeType.RoomAdmin);

        // Raw: the typed call always sends a JSON object.
        var (status, body) = await RawAsync(HttpMethod.Put, VectorizationRoute, json);

        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task SetVectorization_Admin_ResetStoresNoProviderAndGetReadsItBack()
    {
        var written = await _aiSettings.AiSettingsSetVectorizationAsync(ResetBody(), TestContext.Current.CancellationToken);
        var read = await _aiSettings.AiSettingsGetVectorizationAsync(TestContext.Current.CancellationToken);

        written.Response.Type.Should().Be(AiEmbeddingProviderType.None);
        read.Response.Type.Should().Be(AiEmbeddingProviderType.None);
        read.Response.NeedReset.Should().BeFalse();
    }
}
