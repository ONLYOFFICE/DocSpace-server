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

namespace ASC.Core.Users;

/// <summary>
/// An account went from active to disabled, by whatever path: an API call, an LDAP synchronization, a sign-in that failed
/// the directory's group check. <see cref="UserManager.UpdateUserInfoAsync"/> publishes it after the change is saved, so
/// that a service the disabling process does not host can clean up after it (the Files worker deletes the account's file
/// keys of private rooms).
/// </summary>
[ProtoContract]
public record UserDisabledIntegrationEvent : IntegrationEvent
{
    private UserDisabledIntegrationEvent() : base() { }

    public UserDisabledIntegrationEvent(Guid createBy, int tenantId) : base(createBy, tenantId)
    {
    }

    /// <summary>The account that was disabled.</summary>
    [ProtoMember(1)]
    public Guid UserId { get; init; }
}
