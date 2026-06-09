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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class MessageDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        var thread = await CreateThreadAsync();
        var created = await CreateMessageAsync(thread.Id);

        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{created.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var readResponse = await Ai.GetAsync(
            $"{MessagesPath}/{created.Id}",
            TestContext.Current.CancellationToken);
        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExisting_Returns404()
    {
        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OneMessage_DoesNotAffectOthers()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));

        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{first.Id}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await ReadMessagesByThreadAsync(thread.Id);
        remaining.Should().ContainSingle(m => m.Id == second.Id);
    }

    [Fact]
    public async Task DeleteByThread_Existing_RemovesAllMessages()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await ReadMessagesByThreadAsync(thread.Id);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByThread_DoesNotAffectOtherThreads()
    {
        var first = await CreateThreadAsync("first");
        var second = await CreateThreadAsync("second");

        await CreateMessageAsync(first.Id, BuildMessageContents("first-msg"));
        var secondMessage = await CreateMessageAsync(second.Id, BuildMessageContents("second-msg"));

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{first.Id}/messages",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondMessages = await ReadMessagesByThreadAsync(second.Id);
        secondMessages.Should().ContainSingle(m => m.Id == secondMessage.Id);
    }

    [Fact]
    public async Task DeleteByThread_NonExistentThread_Returns404()
    {
        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteByThread_EmptyThread_Returns204()
    {
        var thread = await CreateThreadAsync();

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
