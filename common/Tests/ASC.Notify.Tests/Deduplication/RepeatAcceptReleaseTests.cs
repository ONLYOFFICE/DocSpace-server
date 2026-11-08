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


namespace ASC.Notify.Tests.Deduplication;

/// <summary>
/// A request the sending code accepted but failed to hand over - the link, the audit record or the publishing threw -
/// must not leave its acceptance behind: the repeat right after the failure has to build the letter, not be refused
/// for one that was never sent. Each case runs the real sending code with a bus whose first publication fails.
/// </summary>
public class RepeatAcceptReleaseTests
{
    [Fact]
    public async Task EmailActivation_AfterAFailedPublication_GoesOutOnTheRepeat()
    {
        await AssertRepeatGoesOutAsync((service, scope, email) => service.SendEmailActivationInstructionsAsync(scope.Recipient, email));
    }

    [Fact]
    public async Task ProfileDeletion_AfterAFailedPublication_GoesOutOnTheRepeat()
    {
        await AssertRepeatGoesOutAsync((service, scope, email) =>
        {
            var user = (UserInfo)scope.Recipient.Clone();
            user.Email = email;

            return service.SendMsgProfileDeletionAsync(user);
        });
    }

    private static async Task AssertRepeatGoesOutAsync(Func<StudioNotifyService, LetterScope, string, Task<bool>> send)
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        var bus = new RecordingEventBus { FailuresLeft = 1 };
        var helper = ActivatorUtilities.CreateInstance<StudioNotifyServiceHelper>(scope.Services, bus);
        var service = ActivatorUtilities.CreateInstance<StudioNotifyService>(scope.Services, helper);
        var email = $"repeat-{Guid.NewGuid():N}@preview.onlyoffice.com";

        await Assert.ThrowsAsync<InvalidOperationException>(() => send(service, scope, email));

        var repeat = await send(service, scope, email);

        repeat.Should().BeTrue("the failed request handed no letter over, so it must not hold the repeat back");
        bus.Published.OfType<NotifyItemIntegrationEvent>().Should().ContainSingle();
    }
}
