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
/// The links of <c>profile_delete</c>, <c>portal_deactivate</c> and <c>portal_delete</c> are checked against the
/// address with the database comparison, which tells case apart on PostgreSQL. So the repeat check keys these letters
/// on the address exactly as stored: after a change of case the next request has to deliver a link made for the new
/// address, not be dropped as a repeat of a letter whose link no longer opens.
/// </summary>
public class LinkAddressCaseTests
{
    [Fact]
    public async Task ProfileDeletion_ForTheAddressInAnotherCase_IsANewLetter()
    {
        await using var context = await OpenAsync();
        var email = $"Repeat-{Guid.NewGuid():N}@Preview.OnlyOffice.com";

        var first = await context.Service.SendMsgProfileDeletionAsync(WithEmail(context.Scope.Recipient, email));
        var lowered = await context.Service.SendMsgProfileDeletionAsync(WithEmail(context.Scope.Recipient, email.ToLowerInvariant()));

        first.Should().BeTrue();
        lowered.Should().BeTrue("the address went into the key as stored, so another case is another letter");
    }

    [Fact]
    public async Task PortalDeactivation_KeysOnTheAddressAsStored()
    {
        await using var context = await OpenAsync();
        var owner = WithEmail(context.Scope.Recipient, "Owner.Case@Preview.OnlyOffice.com");

        var action = context.Scope.Services.GetRequiredService<PortalDeactivateNotifyAction>();
        action.Init(owner, context.Scope.PortalUrl, context.Scope.PortalUrl);

        action.Tags.Should().Contain(t => t.Tag == "LinkAddress" && "Owner.Case@Preview.OnlyOffice.com".Equals(t.Value));
    }

    [Fact]
    public async Task PortalDeletion_KeysOnTheAddressAsStored()
    {
        await using var context = await OpenAsync();
        var owner = WithEmail(context.Scope.Recipient, "Owner.Case@Preview.OnlyOffice.com");

        var action = context.Scope.Services.GetRequiredService<PortalDeleteNotifyAction>();
        action.Init(owner, context.Scope.PortalUrl, false);

        action.Tags.Should().Contain(t => t.Tag == "LinkAddress" && "Owner.Case@Preview.OnlyOffice.com".Equals(t.Value));
    }

    private static async Task<Context> OpenAsync()
    {
        var stack = await LetterStackFixture.CurrentAsync();
        var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        var helper = ActivatorUtilities.CreateInstance<StudioNotifyServiceHelper>(scope.Services, new RecordingEventBus());
        var service = ActivatorUtilities.CreateInstance<StudioNotifyService>(scope.Services, helper);

        return new Context(scope, service);
    }

    private static UserInfo WithEmail(UserInfo user, string email)
    {
        var copy = (UserInfo)user.Clone();
        copy.Email = email;

        return copy;
    }

    private sealed record Context(LetterScope Scope, StudioNotifyService Service) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Scope.Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
