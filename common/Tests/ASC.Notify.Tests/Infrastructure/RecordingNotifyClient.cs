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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// Stands in for the notify client the tariff job registers. It records what it was asked to send and
/// sends nothing: the point here is the recipient list, and rendering the letter is what the letter
/// tests are for.
/// </summary>
internal sealed class RecordingNotifyClient : INotifyClient
{
    public List<(INotifyAction Action, IRecipient Recipient)> Sent { get; } = [];

    public Task SendNoticeToAsync(INotifyAction action, IRecipient recipient, string senderNames)
    {
        Sent.Add((action, recipient));

        return Task.CompletedTask;
    }

    // Nothing else is reachable from BasePeriodicNotifyAction.SendAsync. A call here means the
    // sending code changed and this stand-in stopped standing in for it.
    public void AddInterceptor(ISendInterceptor interceptor) => throw NotUsed();

    public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, bool checkSubscription) => throw NotUsed();

    public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient) => throw NotUsed();

    public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, string senderNames) => throw NotUsed();

    public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient[] recipient, string senderNames) => throw NotUsed();

    public Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, bool checkSubsciption) => throw NotUsed();

    public Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames) => throw NotUsed();

    private static NotSupportedException NotUsed([CallerMemberName] string member = "")
    {
        return new NotSupportedException(
            $"A periodic letter reached INotifyClient.{member}, which it never used to. Either the "
            + "sending code changed, or this stand-in is being asked the wrong thing.");
    }
}
