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

namespace ASC.AI.Tests.Tests.AssignmentTests;

/// <summary>
/// Who may read and who may change the portal's model assignments (assignments.permission.spec.ts). Reads are open to
/// every member and refused for a Guest; writes are administrator-only. The split is per role, not per assignment, so
/// one read and one write per role is the whole matrix.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentPermissionTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task Anonymous_EveryRoute_Gets401_AndTheRefusedWriteWroteNothing()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        var cancellationToken = TestContext.Current.CancellationToken;

        await _aiChatClient.Authenticate(null);

        var calls = new (string Route, Func<Task> Call)[]
        {
            ("get-all-assignments", () => GetAllAssignmentsAsync()),
            ("get-assignment", () => _aiAssignments.AiAssignmentsGetAssignmentAsync(nameof(AiActionType.Chat), cancellationToken)),
            ("resolve-for-action", () => ResolveAsync(nameof(AiActionType.Chat))),
            ("try-resolve-for-action", () => TryResolveAsync(nameof(AiActionType.Chat))),
            ("assign", () => TryAssignAsync(AiActionType.Chat, text.Id)),
            ("bulk-assign", () => BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Chat)] = text.Id })),
            ("unassign", () => UnassignAsync(AiActionType.Chat)),
            ("cascade-profile-delete", () => CascadeProfileDeleteAsync(text.Id))
        };

        foreach (var (route, call) in calls)
        {
            (await StatusOfAsync(call)).Should().Be(401, $"{route} as anonymous");
        }

        // The refused write really wrote nothing.
        await _aiChatClient.Authenticate(Owner);
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, true)]
    [InlineData(EmployeeType.RoomAdmin, true)]
    [InlineData(EmployeeType.User, true)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task Read_ByRole_IsOpenToMembersAndRefusedForAGuest(EmployeeType type, bool canRead)
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Summarization, text.Id);

        var member = await InviteMemberAsync(type);
        await _aiChatClient.Authenticate(member);

        var all = await StatusOfAsync(() => GetAllAssignmentsAsync());
        var single = await StatusOfAsync(() => _aiAssignments.AiAssignmentsGetAssignmentAsync(nameof(AiActionType.Summarization), TestContext.Current.CancellationToken));
        var resolved = await StatusOfAsync(() => ResolveAsync(nameof(AiActionType.Chat)));

        if (!canRead)
        {
            all.Should().Be(403);
            single.Should().Be(403);
            resolved.Should().Be(403);

            return;
        }

        all.Should().Be(200);
        single.Should().Be(200);
        resolved.Should().Be(200);

        (await GetAllAssignmentsAsync())[nameof(AiActionType.Summarization)].Should().Be(text.Id);
        (await GetAssignmentAsync(AiActionType.Summarization)).Should().Be(text.Id);
    }

    /// <summary>
    /// Resolution is what the chat surface actually calls and it hands back the whole profile; an ordinary member must
    /// still not get a credential out of it. (The profile list and get-by-id strip the key for a non-administrator; the
    /// resolve route echoes the profile the engine read, which on a plain provider profile still carries it.)
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task Resolve_AsAUser_DoesNotCarryTheProviderKey()
    {
        await CreateProfileWithAsync(TextCapabilities);

        var user = await InviteMemberAsync(EmployeeType.User);
        await _aiChatClient.Authenticate(user);

        var resolved = await ResolveAsync(nameof(AiActionType.Chat));

        resolved.Profile.Key.Should().BeNullOrEmpty("a member never sees the provider credential");
        JsonSerializer.Serialize(resolved).Should().NotContain(StubModelProvider.Key);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, true)]
    [InlineData(EmployeeType.RoomAdmin, false)]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task Write_ByRole_IsAdministratorOnly(EmployeeType type, bool canWrite)
    {
        var seed = await CreateProfileWithAsync(TextCapabilities);
        var attempt = await CreateProfileWithAsync(TextVisionCapabilities);
        await AssignAsync(AiActionType.Chat, seed.Id);

        var member = await InviteMemberAsync(type);
        await _aiChatClient.Authenticate(member);

        var bulkMap = new Dictionary<string, string> { [nameof(AiActionType.Summarization)] = attempt.Id };

        if (canWrite)
        {
            (await TryAssignAsync(AiActionType.Chat, attempt.Id)).Success.Should().BeTrue();
            (await BulkAssignAsync(bulkMap)).Success.Should().BeTrue();
            (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();

            await _aiChatClient.Authenticate(Owner);
            (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("the admin's unassign took effect");

            return;
        }

        (await StatusOfAsync(() => TryAssignAsync(AiActionType.Chat, attempt.Id))).Should().Be(403, "assign");
        (await StatusOfAsync(() => BulkAssignAsync(bulkMap))).Should().Be(403, "bulk-assign");
        (await StatusOfAsync(() => UnassignAsync(AiActionType.Chat))).Should().Be(403, "unassign");

        // The seeded binding is intact, so the 403s were real refusals.
        await _aiChatClient.Authenticate(Owner);
        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(seed.Id, "the assignment is unchanged");
        (await GetAssignmentAsync(AiActionType.Summarization)).Should().BeNull();
    }

    /// <summary>A new member of the given type; a Guest goes through its own invitation route.</summary>
    private async Task<User> InviteMemberAsync(EmployeeType type) =>
        type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);
}
