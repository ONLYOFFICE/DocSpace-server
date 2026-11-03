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
/// The per-user chat config (<c>GET|PUT /api/2.0/ai/config/user</c>, one flag: <c>chatRecommendedModelVisible</c>):
/// the shipped default, writes that read back, the value kept per user whoever else writes, and a request that never
/// reaches the portal-wide config or another user.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Settings")]
public class SettingsUserConfigTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    [Fact]
    public async Task GetUserConfig_NewUser_SeesTheRecommendedModelBanner()
    {
        // The shipped default, kept as its own test so the update tests below can stay default-agnostic.
        (await ReadVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SetUserConfig_OwnersUpdate_IsPersisted()
    {
        var initial = await ReadVisibleAsync();

        var echoed = await WriteVisibleAsync(!initial);

        echoed.Should().Be(!initial);
        (await ReadVisibleAsync()).Should().Be(!initial);
    }

    [Fact]
    public async Task SetUserConfig_OneUsersPreference_DoesNotChangeAnybodyElses()
    {
        var user = await InviteMemberAsync(EmployeeType.User);
        var roomAdmin = await InviteMemberAsync(EmployeeType.RoomAdmin);

        await _aiChatClient.Authenticate(user);
        var target = !await ReadVisibleAsync();
        await WriteVisibleAsync(target);
        (await ReadVisibleAsync()).Should().Be(target);

        await _aiChatClient.Authenticate(roomAdmin);
        (await ReadVisibleAsync()).Should().Be(!target);

        await _aiChatClient.Authenticate(Owner);
        (await ReadVisibleAsync()).Should().Be(!target);
    }

    [Fact]
    public async Task GetUserConfig_RepeatedReads_DoNotChangeTheValue()
    {
        foreach (var stored in new[] { true, false })
        {
            await WriteVisibleAsync(stored);

            for (var i = 0; i < 3; i++)
            {
                (await ReadVisibleAsync()).Should().Be(stored, $"read {i} of {stored}");
            }
        }
    }

    [Fact]
    public async Task GetUserConfig_FollowsTheWriteInBothDirectionsAndAcrossClients()
    {
        foreach (var stored in new[] { false, true })
        {
            await WriteVisibleAsync(stored);

            // Two unrelated clients (the typed SDK and a raw request) for one token: the value has to come from
            // storage, not from the writer's session.
            (await ReadVisibleAsync()).Should().Be(stored);
            (await ReadRawResponseAsync(UserConfigRoute)).GetProperty(ChatRecommendedModelVisible).GetBoolean().Should().Be(stored);
        }
    }

    [Fact]
    public async Task UserConfig_EachMemberKeepsTheirOwnValueThroughInterleavedWritesByOthers()
    {
        var members = new Dictionary<string, User>
        {
            ["owner"] = Owner,
            ["userA"] = await InviteMemberAsync(EmployeeType.User),
            ["userB"] = await InviteMemberAsync(EmployeeType.User),
            ["roomAdmin"] = await InviteMemberAsync(EmployeeType.RoomAdmin),
            ["guest"] = await InviteMemberAsync(EmployeeType.Guest)
        };
        var expected = members.Keys.ToDictionary(name => name, _ => true);

        async Task ExpectAllAsync(string step)
        {
            foreach (var (name, member) in members)
            {
                await _aiChatClient.Authenticate(member);
                (await ReadVisibleAsync()).Should().Be(expected[name], $"{step}: {name}");
            }
        }

        await ExpectAllAsync("start");

        // Each write is by a different member and in a different direction from the one before, so a value that
        // leaked sideways would flip somebody else's.
        (string Name, bool Value)[] writes =
        [
            ("guest", false),
            ("roomAdmin", false),
            ("userA", false),
            ("userA", true),
            ("owner", false),
            ("userB", false),
            ("userB", true)
        ];
        foreach (var (name, value) in writes)
        {
            await _aiChatClient.Authenticate(members[name]);
            await WriteVisibleAsync(value);
            expected[name] = value;

            await ExpectAllAsync($"after {name}={value}");
        }
    }

    [Fact]
    public async Task UserConfig_TheTokenAloneDecidesWhoseValueIsReadAndWritten()
    {
        var user = await InviteMemberAsync(EmployeeType.User);
        await WriteVisibleAsync(true);
        await _aiChatClient.Authenticate(user);
        await WriteVisibleAsync(true);

        // The user names the owner in the body and in the query string. Neither may redirect the write.
        await _aiSettings.AiSettingsSetUserAsync(
            new Dictionary<string, object>
            {
                [ChatRecommendedModelVisible] = false,
                ["userId"] = Owner.Id,
                ["id"] = Owner.Id,
                ["targetUserId"] = Owner.Id
            },
            TestContext.Current.CancellationToken);
        // Raw: a query parameter the typed signature does not have.
        var (status, body) = await RawAsync(HttpMethod.Put, $"{UserConfigRoute}?userId={Owner.Id}", """{"chatRecommendedModelVisible":false}""");

        status.Should().Be(HttpStatusCode.OK, body);
        (await ReadVisibleAsync()).Should().BeFalse("the caller's own value moved");
        await _aiChatClient.Authenticate(Owner);
        (await ReadVisibleAsync()).Should().BeTrue("the owner is untouched");

        // Reading with the owner's id in the query still returns the caller's own.
        await _aiChatClient.Authenticate(user);
        var read = await RawAsync(HttpMethod.Get, $"{UserConfigRoute}?userId={Owner.Id}");
        read.Body.Should().Contain("\"chatRecommendedModelVisible\":false");
    }

    [Fact]
    public async Task SetUserConfig_TogglesAndEveryWriteReadsBack()
    {
        foreach (var value in new[] { true, false, true })
        {
            (await WriteVisibleAsync(value)).Should().Be(value);

            // The echo proves nothing about storage; the read does.
            (await ReadVisibleAsync()).Should().Be(value);
        }
    }

    [Fact]
    public async Task SetUserConfig_SameValueTwice_IsIdempotent()
    {
        foreach (var value in new[] { false, true })
        {
            await WriteVisibleAsync(value);
            await WriteVisibleAsync(value);

            (await ReadVisibleAsync()).Should().Be(value);
        }
    }

    [Fact]
    public async Task SetUserConfig_TheLastOfSeveralWritesWins()
    {
        foreach (var value in new[] { false, true, false, false, true, false })
        {
            await WriteVisibleAsync(value);
        }

        (await ReadVisibleAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task SetUserConfig_NeverTouchesThePortalWideConfig_WhateverTheBodyClaims()
    {
        var before = (await ReadConfigAsync()).ToJson();

        // The body tries to flip the portal's own flags next to the legitimate one.
        await _aiSettings.AiSettingsSetUserAsync(
            new Dictionary<string, object>
            {
                [ChatRecommendedModelVisible] = false,
                ["aiReady"] = true,
                ["vectorizationEnabled"] = true,
                ["vectorizationNeedReset"] = true,
                ["systemAiEnabled"] = true,
                ["embeddingModel"] = "attacker/embedding-model",
                ["recommendedModelForForms"] = "attacker/model"
            },
            TestContext.Current.CancellationToken);

        (await ReadVisibleAsync()).Should().BeFalse();
        (await ReadConfigAsync()).ToJson().Should().Be(before);
    }
}
