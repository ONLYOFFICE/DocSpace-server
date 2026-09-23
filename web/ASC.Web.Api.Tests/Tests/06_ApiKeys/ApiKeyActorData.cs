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

namespace ASC.Web.Api.Tests.Tests._06_ApiKeys;

/// <summary>
/// The portal owner plus the roles the TS <c>apiKeys.spec.ts</c> suite exercises. A dedicated enum
/// (rather than a nullable <see cref="EmployeeType"/>) keeps it a plain value type in
/// <see cref="TheoryData{T1}"/> and gives readable test names — same convention as
/// <c>NotificationActor</c> in <c>Tests/02_Settings/Notifications</c>.
/// </summary>
public enum ApiKeyActor
{
    Owner,
    DocSpaceAdmin,
    RoomAdmin,
    User,
    Guest
}

/// <summary>
/// The actor combinations shared by the functional ApiKeys suites — read again before adding a
/// new combination, per the class-size rule.
/// </summary>
public static class ApiKeyActorData
{
    /// <summary>Owner, DocSpaceAdmin, RoomAdmin and User — every role the TS suite lets create and manage its own key.</summary>
    public static TheoryData<ApiKeyActor> AllRoles()
    {
        var data = new TheoryData<ApiKeyActor>();

        foreach (var actor in new[] { ApiKeyActor.Owner, ApiKeyActor.DocSpaceAdmin, ApiKeyActor.RoomAdmin, ApiKeyActor.User })
        {
            data.Add(actor);
        }

        return data;
    }

    /// <summary>The three admin-like roles (Owner, DocSpaceAdmin, RoomAdmin) that request the full permission catalog in the TS suite — User requests a reduced one (no <c>accounts:*</c>).</summary>
    public static TheoryData<ApiKeyActor> AdminLikeRoles()
    {
        var data = new TheoryData<ApiKeyActor>();

        foreach (var actor in new[] { ApiKeyActor.Owner, ApiKeyActor.DocSpaceAdmin, ApiKeyActor.RoomAdmin })
        {
            data.Add(actor);
        }

        return data;
    }

    /// <summary>The three roles that are never allowed to touch the portal owner's own key.</summary>
    public static TheoryData<ApiKeyActor> NonOwnerRoles()
    {
        var data = new TheoryData<ApiKeyActor>();

        foreach (var actor in new[] { ApiKeyActor.RoomAdmin, ApiKeyActor.User, ApiKeyActor.Guest })
        {
            data.Add(actor);
        }

        return data;
    }
}
