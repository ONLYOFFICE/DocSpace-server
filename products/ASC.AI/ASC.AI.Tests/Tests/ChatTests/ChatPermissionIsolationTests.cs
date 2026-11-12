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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Threads are per user, not per room: even a ContentCreator in the AI room cannot open, change or write into the
/// owner's thread. Every thread operation is checked, not only the readers — a backend can refuse get-by-id on
/// someone else's thread and still accept append-user-message against the same id. This replaces the old
/// room-owned chat matrices (Bugs 80791, 80797, 80801).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionIsolationTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    private static readonly string[] _operations =
    [
        "get-by-id",
        "read-messages",
        "rename",
        "delete",
        "clear-messages",
        "touch",
        "append-user-message"
    ];

    /// <summary>Guest is not a member type here: an AI room grants a Guest nothing above Read (ChatPermissionGuestTests).</summary>
    public static TheoryData<EmployeeType, string> MembersAndOperations
    {
        get
        {
            var data = new TheoryData<EmployeeType, string>();
            foreach (var type in new[] { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User })
            {
                foreach (var operation in _operations)
                {
                    data.Add(type, operation);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MembersAndOperations))]
    public async Task Operation_MemberOfTheAiRoom_OnTheOwnersThread_Returns403(EmployeeType type, string operation)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedOwnerThreadAsync();
        var member = await InviteMemberAsync(type);
        await ShareRoomAsync(seed.RoomId, member.Id, FileShare.ContentCreator);

        await _aiChatClient.Authenticate(member);
        Func<Task> act = operation switch
        {
            "get-by-id" => () => GetThreadAsync(seed.ThreadId),
            "read-messages" => () => ReadAllAsync(seed.ThreadId),
            "rename" => () => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(seed.ThreadId, "Hacked title"), cancellationToken),
            "delete" => () => _aiThreads.AiThreadsDeleteAsync(seed.ThreadId, cancellationToken),
            "clear-messages" => () => _aiThreads.AiThreadsClearMessagesAsync(seed.ThreadId, cancellationToken),
            "touch" => () => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(seed.ThreadId), cancellationToken),
            // The SDK call is fine here: its defect (the declared shape of the answer) only bites on a 200.
            _ => () => _aiThreads.AiThreadsAppendUserMessageAsync(
                new AiThreadsAppendUserMessageRequest(
                    threadId: seed.ThreadId,
                    message: BuildUserMessage("Injected by another member"),
                    profileId: seed.Profile.Id),
                cancellationToken)
        };
        var (status, error) = await OutcomeOfAsync(act);

        await ExpectOwnerThreadIntactAsync(seed);
        status.Should().Be(403, error);
        error.Should().Contain("Forbidden");
    }
}
