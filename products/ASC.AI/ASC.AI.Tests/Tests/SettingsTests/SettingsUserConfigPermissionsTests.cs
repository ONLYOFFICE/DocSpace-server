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
/// The per-user chat config is not gated by role: every member type, a Guest included (a Guest is a Viewer, and the
/// preference is a Viewer's own), reads and sets their own. Anonymous gets 401, and so does a token whose signature was
/// corrupted — on every settings route, with the stored preference unmoved and the genuine token still accepted.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Settings")]
public class SettingsUserConfigPermissionsTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetUserConfig_EveryRole_ReadsTheirOwnPreference(EmployeeType? type)
    {
        if (type is not null)
        {
            await ActAsMemberAsync(type.Value);
        }

        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken));

        status.Should().Be(200, error);
        (await ReadVisibleAsync()).Should().BeTrue("the shipped default");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetUserConfig_EveryRole_UpdatesTheirOwnPreference(EmployeeType? type)
    {
        if (type is not null)
        {
            await ActAsMemberAsync(type.Value);
        }

        var target = !await ReadVisibleAsync();

        (await WriteVisibleAsync(target)).Should().Be(target);

        // The echo proves nothing about storage; the Guest's own re-read is the whole of what this route offers them.
        (await ReadVisibleAsync()).Should().Be(target);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task UserConfig_Anonymous_Returns401(string verb)
    {
        await _aiChatClient.Authenticate(null);

        var (status, body) = verb == "GET"
            ? await OutcomeOfAsync(() => _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken))
            : await OutcomeOfAsync(() => _aiSettings.AiSettingsSetUserAsync(
                new Dictionary<string, object> { [ChatRecommendedModelVisible] = false },
                TestContext.Current.CancellationToken));

        await _aiChatClient.Authenticate(Owner);
        (await ReadVisibleAsync()).Should().BeTrue("a refused write must not have changed anything");
        status.Should().Be(401, body);
        body.Should().Contain("Unauthorized");
    }

    [Theory]
    [InlineData("GET", ConfigRoute, null, 200)]
    [InlineData("GET", UserConfigRoute, null, 200)]
    [InlineData("PUT", UserConfigRoute, """{"chatRecommendedModelVisible":false}""", 200)]
    [InlineData("GET", VectorizationRoute, null, 200)]
    [InlineData("PUT", VectorizationRoute, """{"type":0}""", 200)]
    public async Task Route_TokenWithACorruptedSignature_Returns401(string verb, string route, string? json, int genuineStatus)
    {
        // A token that is present but wrong: the real one with its signature tail overwritten.
        var genuine = Owner.Token!;
        var tampered = $"{genuine[..^4]}AAAA";
        tampered.Should().NotBe(genuine);
        var method = new HttpMethod(verb);

        async Task<(HttpStatusCode Status, string Body)> SendAsync(string token)
        {
            using var request = new HttpRequestMessage(method, route);
            if (json is not null)
            {
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

            return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        var refused = await SendAsync(tampered);

        // State before status: nothing may have been stored by a refused call.
        (await ReadVisibleAsync()).Should().BeTrue();

        // Positive control: the genuine token gets past the authentication gate on the same request, so the 401 is
        // about the token and nothing else.
        var accepted = await SendAsync(genuine);
        accepted.Status.Should().Be((HttpStatusCode)genuineStatus, accepted.Body);

        refused.Status.Should().Be(HttpStatusCode.Unauthorized, refused.Body);
        refused.Body.Should().Contain("Unauthorized");
    }
}
