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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// The four BYOK routes on a portal without the AI gateway (this stack is standalone): an administrator is answered
/// 404, every other member 403 before the gateway is looked at, an anonymous caller 401.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Byok")]
public class ByokRoutesWithoutGatewayTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    private const string FakeKey = "sk-autotest-fake-byok-0000000000";
    private static readonly string _modelId = Guid.NewGuid().ToString();

    public static TheoryData<string> Routes => ["list", "providers", "set", "delete"];

    private async Task<Outcome> CallAsync(string route)
    {
        var byok = new DocSpace.API.SDK.Api.AI.BYOKApi(
            _aiChatClient,
            new Configuration { BasePath = _aiChatClient.BaseAddress!.ToString().TrimEnd('/') });
        var cancellationToken = TestContext.Current.CancellationToken;

        return route switch
        {
            "list" => await OutcomeOfAsync(() => byok.AiByokKeysListAsync(cancellationToken)),
            "providers" => await OutcomeOfAsync(() => byok.AiByokProvidersAsync(cancellationToken)),
            "set" => await OutcomeOfAsync(() => byok.AiByokKeysSetAsync(_modelId, new AiByokKeyInput(provider: "openai", key: FakeKey), cancellationToken)),
            _ => await OutcomeOfAsync(() => byok.AiByokKeysDeleteAsync(_modelId, cancellationToken))
        };
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EveryRoute_Owner_Returns404WithoutTheGateway(string route)
    {
        var outcome = await CallAsync(route);

        outcome.Status.Should().Be(404, $"{route} as the owner: {outcome.Body}");
        outcome.Body.Should().NotContain(FakeKey);
    }

    [Theory]
    [InlineData("list", EmployeeType.DocSpaceAdmin, 404)]
    [InlineData("set", EmployeeType.DocSpaceAdmin, 404)]
    [InlineData("list", EmployeeType.RoomAdmin, 403)]
    [InlineData("providers", EmployeeType.RoomAdmin, 403)]
    [InlineData("set", EmployeeType.RoomAdmin, 403)]
    [InlineData("delete", EmployeeType.RoomAdmin, 403)]
    [InlineData("list", EmployeeType.User, 403)]
    [InlineData("providers", EmployeeType.User, 403)]
    [InlineData("set", EmployeeType.User, 403)]
    [InlineData("delete", EmployeeType.User, 403)]
    [InlineData("list", EmployeeType.Guest, 403)]
    [InlineData("set", EmployeeType.Guest, 403)]
    public async Task Route_ByRole_OnlyAnAdministratorGetsPastTheRoleCheck(string route, EmployeeType role, int expected)
    {
        await ActAsAsync(role);

        var outcome = await CallAsync(route);

        outcome.Status.Should().Be(expected, $"{route} as {role}: {outcome.Body}");
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EveryRoute_Anonymous_Returns401(string route)
    {
        await _aiChatClient.Authenticate(null);

        var outcome = await CallAsync(route);

        outcome.Status.Should().Be(401, $"{route} as anonymous: {outcome.Body}");
    }
}
