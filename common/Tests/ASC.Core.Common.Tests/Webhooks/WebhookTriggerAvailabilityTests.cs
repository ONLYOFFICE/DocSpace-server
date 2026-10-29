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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// Which roles may subscribe to which triggers, as <c>[AvailableFor]</c> on <see cref="WebhookTrigger"/> declares it.
/// A pure check of the enum: the API side, what a User is refused when creating a subscription, is covered by the
/// People suite (WebhookTest). This class checks the catalog as a whole; the role checks per trigger are split across
/// <see cref="WebhookAdminOnlyTriggerAvailabilityTests"/> and the <see cref="WebhookAllRolesTriggerAvailabilityTestBase"/>
/// classes, to keep each class small enough to run in parallel with the others.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookTriggerAvailabilityTests
{
    /// <summary>
    /// Every trigger sits in exactly one of the two lists of <see cref="WebhookTriggerAvailabilityData"/>, so a trigger
    /// added later cannot go untested by the role checks: it fails here until it is classified.
    /// </summary>
    [Fact]
    public void AllTriggers_AreClassifiedInExactlyOneList()
    {
        var adminOnly = WebhookTriggerAvailabilityData.AdminOnlyTriggers.Select(row => row.Data).ToList();
        var allRoles = WebhookTriggerAvailabilityData.AllRolesTriggers.Select(row => row.Data).ToList();

        adminOnly.Should().NotIntersectWith(allRoles);
        adminOnly.Concat(allRoles).Should().BeEquivalentTo(Enum.GetValues<WebhookTrigger>());
    }

    [Fact]
    public void AllTriggers_HaveAvailableForAttribute()
    {
        var type = typeof(WebhookTrigger);

        foreach (var value in Enum.GetValues<WebhookTrigger>())
        {
            var field = type.GetField(value.ToString());
            var attr = Attribute.GetCustomAttribute(field!, typeof(AvailableForAttribute));
            attr.Should().NotBeNull($"WebhookTrigger.{value} is missing [AvailableFor] attribute");
        }
    }
}
