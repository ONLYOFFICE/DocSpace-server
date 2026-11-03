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

namespace ASC.Core.Common.Tests;

/// <summary>
/// What a channel reports back after handing a message to its sender. The repeat check takes back its mark when a
/// letter was not delivered, and it can tell only from this answer.
/// </summary>
public class NotifySenderChannelTests
{
    private const string SenderName = "email.sender";

    [Theory]
    [InlineData(SendResult.OK)]
    [InlineData(SendResult.Impossible)]
    [InlineData(SendResult.Inprogress)]
    public async Task Channel_ReturnsWhatTheSenderAnswered(SendResult result)
    {
        var context = new Context();
        context.RegisterSender(new DispatchEngine(context, new ConfigurationBuilder().Build(), NullLoggerFactory.Instance), SenderName, new AnsweringSink(result));
        var message = new NoticeMessage(new DirectRecipient("3f2504e0-4f89-11d3-9a0c-0305e82c3301", null, ["user@example.com"]), new TestAction(), null);

        var response = await context.GetSender(SenderName).SendAsync(message, null!);

        response.Result.Should().Be(result);
    }

    private sealed class AnsweringSink(SendResult result) : Sink
    {
        public override Task<SendResponse> ProcessMessage(INoticeMessage message, IServiceScope serviceScope)
        {
            return Task.FromResult(new SendResponse(message, SenderName, result));
        }
    }

    private sealed class TestAction : INotifyAction
    {
        public string ID => "portal_deactivate";
        public List<Pattern> Patterns => [];
        public List<ITagValue> Tags { get; set; } = [];
    }
}
