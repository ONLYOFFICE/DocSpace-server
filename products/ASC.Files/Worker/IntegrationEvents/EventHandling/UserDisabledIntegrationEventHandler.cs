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

namespace ASC.Files.Worker.IntegrationEvents.EventHandling;

/// <summary>
/// Deletes the file keys of private rooms that a disabled account held. The People actions that disable an account do
/// it themselves before they answer; this pass may run alongside them and then finds the same keys or nothing left. It
/// is what reaches the accounts an LDAP synchronization or a failed directory check disables in Web.Api, which hosts no
/// Files code. The account is read again first: an event that arrives after the account was enabled again changes
/// nothing, so keys issued since then stay.
/// </summary>
[Scope]
public class UserDisabledIntegrationEventHandler(
    ILogger<UserDisabledIntegrationEventHandler> logger,
    PrivateRoomAccountHelper privateRoomAccountHelper,
    TenantManager tenantManager,
    SecurityContext securityContext,
    UserManager userManager)
    : IIntegrationEventHandler<UserDisabledIntegrationEvent>
{
    public async Task Handle(UserDisabledIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();
        using (logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);
            await tenantManager.SetCurrentTenantAsync(@event.TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

            // An account deleted before the worker got here holds nothing either: its keys go too.
            var user = await userManager.GetUsersAsync(@event.UserId, false);
            if (!Equals(user, ASC.Core.Users.Constants.LostUser) && !user.Removed && user.Status != EmployeeStatus.Terminated)
            {
                return;
            }

            await privateRoomAccountHelper.RevokeFileKeysOfDisabledAsync(@event.UserId);
        }
    }
}
