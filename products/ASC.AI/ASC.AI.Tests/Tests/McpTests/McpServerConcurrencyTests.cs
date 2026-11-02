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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// Concurrent writes to the registry. It is one stored map, so every write is a read-modify-write of the same document
/// — the shape that loses writes when it is not serialised.
/// </summary>
[Trait("Category", "Concurrency")]
[Trait("Feature", "AI/Mcp")]
public class McpServerConcurrencyTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    public async Task Add_FiveConcurrentRegistrations_AllLand()
    {
        var indexes = Enumerable.Range(1, 5).ToList();

        var results = await Task.WhenAll(indexes.Select(i => AddServerAsync($"autotest-parallel-{i}", UrlConfig($"mcp-{i}.example.invalid"))));

        ShouldHold(await ListServersAsync(), indexes.ToDictionary(i => $"autotest-parallel-{i}", i => (object)UrlConfig($"mcp-{i}.example.invalid")));
        results.Should().OnlyContain(r => r.Success);
    }

    /// <summary>
    /// The loser of a race for one name must get the same soft "already registered" answer as a sequential duplicate.
    /// On this stand a loser answers {"error":"Forbidden"} (403) instead: the add is checked before it is written, and the
    /// storage refusal of the racing insert is mapped to a 403. No bug number is filed for it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task Add_EightConcurrentRegistrationsOfOneName_StoreItOnce()
    {
        var configs = Enumerable.Range(0, 8).Select(i => UrlConfig($"race-{i}.example.invalid")).ToList();

        // The typed call; the status of a loser is read from the ApiException of its refusal.
        async Task<ToolsReply> TryAddAsync(Dictionary<string, object> config)
        {
            try
            {
                return new ToolsReply(HttpStatusCode.OK, Newtonsoft.Json.JsonConvert.SerializeObject(await AddServerAsync("contested", config)));
            }
            catch (ApiException exception)
            {
                return new ToolsReply((HttpStatusCode)exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
            }
        }

        var results = await Task.WhenAll(configs.Select(TryAddAsync));

        results.Should().OnlyContain(r => r.Code == 200, string.Join(" | ", results.Select(r => r.ToString())));
        results.Count(r => r.Success == true).Should().Be(1, "exactly one registration wins");
        foreach (var loser in results.Where(r => r.Success != true))
        {
            loser.SoftMessage.Should().Contain("already registered");
        }

        var stored = await ListServersAsync();
        stored.Keys.Should().BeEquivalentTo(["contested"]);
        configs.Should().Contain(c => SameConfig(stored["contested"], c), "the stored config is one that was sent, whole");
    }

    /// <summary>Which of the two wins is a race, not a contract: the replaced entry is gone and every value is whole.</summary>
    [Fact]
    public async Task ReplaceAll_ConcurrentAdd_NeverLeavesAPartialMap()
    {
        await RegisterAsync("autotest-pre-existing", UrlConfig("pre.example.invalid"));
        var fromReplace = UrlConfig("replace.example.invalid");
        var fromAdd = UrlConfig("add.example.invalid");

        var (replaced, added) = (
            ReplaceAllAsync(new Dictionary<string, object> { ["autotest-from-replace"] = fromReplace }),
            AddServerAsync("autotest-from-add", fromAdd));
        await Task.WhenAll(replaced, added);

        var listed = await ListServersAsync();
        listed.Keys.Should().NotContain("autotest-pre-existing", "the replaced entry is gone whichever call landed last");
        listed.Should().NotBeEmpty();
        foreach (var (name, config) in listed)
        {
            name.Should().BeOneOf("autotest-from-replace", "autotest-from-add");
            ShouldBeConfig(config, name == "autotest-from-replace" ? fromReplace : fromAdd, $"{name} is stored whole");
        }

        (await replaced).Success.Should().BeTrue();
        (await added).Success.Should().BeTrue();
    }

    /// <summary>An update racing a remove leaves the server absent or whole, and its neighbour untouched.</summary>
    [Fact]
    public async Task Update_RacingARemove_LeavesTheServerAbsentOrWhole_AndNeighboursUntouched()
    {
        await RegisterAsync("bystander", StdioConfig());

        for (var round = 0; round < 4; round++)
        {
            var name = $"racer-{round}";
            var original = UrlConfig("original.example.invalid");
            var next = UrlConfig($"raced-{round}.example.invalid");
            await RegisterAsync(name, original);

            // Absent when the remove won: the update answers a soft "not registered" or a 404, never anything else.
            var updating = Task.Run(async () =>
            {
                try
                {
                    await UpdateServerAsync(name, next);
                }
                catch (ApiException exception)
                {
                    exception.ErrorCode.Should().Be(404, $"round {round}");
                }
            }, TestContext.Current.CancellationToken);
            var removing = RemoveServerAsync(name);
            await Task.WhenAll(updating, removing);

            (await removing).Success.Should().BeTrue($"round {round}");

            var stored = await ListServersAsync();
            ShouldBeConfig(stored["bystander"], StdioConfig(), $"round {round}");
            if (stored.TryGetValue(name, out var survivor))
            {
                (SameConfig(survivor, original) || SameConfig(survivor, next)).Should().BeTrue($"round {round}: the survivor is whole");
            }
        }
    }

    [Fact]
    public async Task Update_ConcurrentUpdatesOfDifferentServers_LoseNone()
    {
        var names = Enumerable.Range(0, 6).Select(i => $"s{i}").ToList();
        foreach (var name in names)
        {
            await RegisterAsync(name, UrlConfig());
        }

        var results = await Task.WhenAll(names.Select(n => UpdateServerAsync(n, UrlConfig($"{n}-new.example.invalid"))));

        results.Should().OnlyContain(r => r.Success);
        ShouldHold(await ListServersAsync(), names.ToDictionary(n => n, n => (object)UrlConfig($"{n}-new.example.invalid")));
    }
}
