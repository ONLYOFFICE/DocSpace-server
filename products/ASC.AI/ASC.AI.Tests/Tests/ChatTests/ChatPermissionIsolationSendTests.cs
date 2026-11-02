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
/// <c>POST /api/2.0/ai/ai/send-with-stream</c> into the owner's thread by another member of the AI room. It used to
/// be the one operation on someone else's thread that answered HTTP 200 with
/// <c>{"type":"error","message":"stream error"}</c> in the body; it now refuses with 403 like the rest of the matrix
/// in <see cref="ChatPermissionIsolationTests"/>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionIsolationSendTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    [Theory]
    [Trait("Bug", "82717")]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SendWithStream_MemberOfTheAiRoom_IntoTheOwnersThread_Returns403(EmployeeType type)
    {
        var seed = await SeedOwnerThreadAsync();
        var member = await InviteMemberAsync(type);
        await ShareRoomAsync(seed.RoomId, member.Id, FileShare.ContentCreator);
        var question = $"Injected question {Guid.NewGuid():N}";

        await _aiChatClient.Authenticate(member);
        var round = await SendWithStreamAsync(seed.ThreadId, question, seed.Profile.Id, seed.RoomId.ToString());

        // The round is synchronous: a send that had gone through would have stored the question and the reply by the
        // time the stream ended, so the thread can be checked at once.
        await ExpectOwnerThreadIntactAsync(seed);
        ModelProvider.CompletionRequestBodies.Should().NotContain(b => b.Contains(question), "the injected question never reached the model");

        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        round.Raw.Should().Contain("Forbidden");
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }
}
