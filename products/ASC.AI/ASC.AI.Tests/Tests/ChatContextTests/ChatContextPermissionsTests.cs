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

namespace ASC.AI.Tests.Tests.ChatContextTests;

// The aggregate's contract is omission-instead-of-error: a section the
// caller may not read is left out while the response stays 200, so the Node
// service falls back to the individual endpoint and reproduces its exact
// error semantics. Only the request-wide gates (auth, user type) fail the
// whole response.
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/ChatContext")]
public class ChatContextPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Read_Unauthorized_Returns401()
    {
        await _aiClient.Authenticate(null);

        using var response = await _ai.GetAsync(ChatContextPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Read_RegularUser_Succeeds()
    {
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        var context = await ReadChatContextAsync();

        context.Global.Should().NotBeNull();
    }

    [Fact]
    public async Task Read_ForeignThread_OmitsThreadSection()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id);

        var user = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        var context = await ReadChatContextAsync(threadId: thread.Id);

        context.Thread.Should().BeNull();
        context.Messages.Should().BeNull();
        context.Global.Should().NotBeNull();
    }

    [Fact]
    public async Task Read_NonExistentEntityId_OmitsEntitySection()
    {
        var context = await ReadChatContextAsync(entityId: "999999999");

        context.Entity.Should().BeNull();
        context.Global.Should().NotBeNull();
    }

    [Fact]
    public async Task Read_NonNumericEntityId_OmitsEntitySection()
    {
        var context = await ReadChatContextAsync(entityId: "not-a-folder-id");

        context.Entity.Should().BeNull();
        context.Global.Should().NotBeNull();
    }

    [Fact]
    public async Task Read_UnknownThreadId_OmitsThreadSection()
    {
        var context = await ReadChatContextAsync(threadId: Guid.NewGuid());

        context.Thread.Should().BeNull();
        context.Messages.Should().BeNull();
        context.Global.Should().NotBeNull();
    }
}
