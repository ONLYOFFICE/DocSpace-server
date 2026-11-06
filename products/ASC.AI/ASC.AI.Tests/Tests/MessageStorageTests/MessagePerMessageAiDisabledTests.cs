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

/// <summary>
/// The per-message routes with the portal AI switch off (<c>settings/ai-access</c>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessagePerMessageAiDisabledTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    [Fact]
    public async Task PerMessageRoutes_AiDisabled_Return403AndChangeNothing()
    {
        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId, "original");

        await SetPortalAiAccessAsync(false);

        var read = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(messageId));
        read.ErrorCode.Should().Be(403, "get-message-by-id with AI off");

        var update = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: messageId, message: BuildUserMessage("off")),
            TestContext.Current.CancellationToken));
        update.ErrorCode.Should().Be(403, "update-message with AI off");

        var delete = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsDeleteMessageAsync(messageId, TestContext.Current.CancellationToken));
        delete.ErrorCode.Should().Be(403, "delete-message with AI off");

        // Turning AI back on shows the refused writes really were refused.
        await SetPortalAiAccessAsync(true);

        var stored = await GetMessageAsync(messageId);
        TextOf(stored).Should().Be("original");
        (await ReadMessagesAsync(threadId)).Should().ContainSingle(m => m.Id == messageId);
    }
}
