// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Tests.Tests.ToolPreferenceStorageTests;

[Collection("Test Collection")]
[Trait("Category", "Concurrency")]
[Trait("Feature", "AI/ToolPreferences")]
public class ToolPreferenceConcurrencyTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // Reproduces the duplicate-row bug: the upsert is a read-then-write, so without a lock
    // concurrent calls for the same server type both see "no existing row" and both insert,
    // leaving two rows. The fair lock in ToolPrefsStorage must serialize them into one.
    [Fact]
    public async Task ConcurrentUpsertDisabled_SameServerType_DoesNotCreateDuplicate()
    {
        var upserts = Enumerable.Range(0, 10)
            .Select(_ => UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search"]
            }));

        await Task.WhenAll(upserts);

        var stored = await ReadToolPrefsAsync();

        stored.Should().HaveCount(1);
        stored.Should().ContainKey(SystemToolsServerType);
        stored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    // The two write paths (disabled and allow-always) hit the same row through different
    // endpoints. Fired together for the same server type they must converge on a single row
    // that carries both values, not two competing rows.
    [Fact]
    public async Task ConcurrentUpsertDisabledAndAllowAlways_SameServerType_MergeIntoSingleRow()
    {
        var disabled = UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search"]
        });

        var allowAlways = UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["preview"]
        });

        await Task.WhenAll(disabled, allowAlways);

        var stored = await ReadToolPrefsAsync();

        stored.Should().HaveCount(1);
        stored.Should().ContainKey(SystemToolsServerType);
        stored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview"]);
    }

    // Scoped upserts are keyed by entry, so a global and a per-entry write for the same server
    // type run under different locks and must remain two independent rows.
    [Fact]
    public async Task ConcurrentUpsertDisabled_GlobalAndScoped_StayIsolated()
    {
        var roomId = await CreateRoomAsync();

        var global = UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        var scoped = UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        await Task.WhenAll(global, scoped);

        var globalStored = await ReadToolPrefsAsync();
        var scopedStored = await ReadToolPrefsAsync(roomId.ToString());

        globalStored.Should().HaveCount(1);
        globalStored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["global-tool"]);

        scopedStored.Should().HaveCount(1);
        scopedStored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["scoped-tool"]);
    }
}
