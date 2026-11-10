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
/// <c>GET /api/2.0/ai/config</c>, the portal AI settings: open to every member type, a Guest included, with one body
/// shape for all of them, the documented fields and none of the retired ones, anonymous refused with 401.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Settings")]
public class SettingsGetTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    /// <summary>Fields the pre-rewrite body carried; their absence is the published contract, not a defect.</summary>
    private static readonly string[] _retiredFields =
    [
        "webSearchEnabled",
        "webSearchNeedReset",
        "aiReadyNeedReset",
        "portalMcpServerId",
        "modelAliases",
        "knowledgeSearchToolName",
        "webSearchToolName",
        "webCrawlingToolName",
        "generateDocxToolName",
        "generateFormToolName",
        "generatePresentationToolName"
    ];

    /// <summary>The non-nullable fields of the body (<c>recommendedModelForForms</c> is nullable and may be left out).</summary>
    private static readonly string[] _declaredFields =
    [
        "vectorizationEnabled",
        "vectorizationNeedReset",
        "aiReady",
        "embeddingModel",
        "systemAiEnabled",
        "toolPermissionMode"
    ];

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetAiSettings_EveryRole_Gets200WithTheDeclaredFields(EmployeeType? type)
    {
        if (type is not null)
        {
            await ActAsMemberAsync(type.Value);
        }

        var config = await ReadConfigAsync();

        config.Should().NotBeNull();
        config.EmbeddingModel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAiSettings_Anonymous_Returns401()
    {
        await _aiChatClient.Authenticate(null);

        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsGetAsync(TestContext.Current.CancellationToken));

        status.Should().Be(401, error);
        error.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task GetAiSettings_Body_CarriesEveryDeclaredFieldAndNoneOfTheRetiredOnes()
    {
        var body = await ReadRawResponseAsync(ConfigRoute);

        // Positive control first: an absence check on an empty or unparsed body would pass for the wrong reason.
        foreach (var field in _declaredFields)
        {
            body.TryGetProperty(field, out _).Should().BeTrue($"declared field {field}");
        }
        foreach (var field in _retiredFields)
        {
            body.TryGetProperty(field, out _).Should().BeFalse($"retired field {field}");
        }
    }

    [Fact]
    public async Task GetAiSettings_RepeatedReads_AgreeAndReadingChangesNothing()
    {
        var visibleBefore = await ReadVisibleAsync();

        var first = (await ReadConfigAsync()).ToJson();
        var second = (await ReadConfigAsync()).ToJson();
        var third = (await ReadConfigAsync()).ToJson();

        second.Should().Be(first);
        third.Should().Be(first);
        // The per-user preference lives on a sibling route; reading the portal config must not have touched it.
        (await ReadVisibleAsync()).Should().Be(visibleBefore);
    }

    [Fact]
    public async Task GetAiSettings_EveryRole_SeesTheSameFieldsAndTheSamePortalState()
    {
        var owner = (await ReadRawResponseAsync(ConfigRoute)).GetRawText();
        var seen = new Dictionary<EmployeeType, string>();

        foreach (var type in new[] { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest })
        {
            await ActAsMemberAsync(type);
            seen[type] = (await ReadRawResponseAsync(ConfigRoute)).GetRawText();
        }

        // Portal state is identical for every caller; the user's own tool permission mode is the only per-user field,
        // and every user here is on the default.
        foreach (var (type, body) in seen)
        {
            FieldsOf(body).Should().BeEquivalentTo(FieldsOf(owner), $"{type} sees the same body as the owner");
        }
    }

    [Fact]
    public async Task GetAiSettings_FreshPortal_IsNotAiReadyUntilItHoldsAProfile()
    {
        // The standalone stand has no gateway: the portal is ready once the switch is on AND a profile exists.
        var fresh = await ReadConfigAsync();
        fresh.AiReady.Should().BeFalse("a fresh portal holds no profile");
        fresh.SystemAiEnabled.Should().BeFalse("no AI gateway is configured");
        fresh.VectorizationEnabled.Should().BeFalse("no vectorization provider is configured");

        await MakePortalAiReadyAsync();

        var ready = await ReadConfigAsync();
        ready.AiReady.Should().BeTrue();
        ready.SystemAiEnabled.Should().BeFalse("a profile does not make the gateway appear");
        ready.VectorizationEnabled.Should().BeFalse("nor does it configure vectorization");
    }

    private static Dictionary<string, string> FieldsOf(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!
            .ToDictionary(p => p.Key, p => p.Value.GetRawText());
}
