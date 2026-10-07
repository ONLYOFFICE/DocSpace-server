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

namespace ASC.Web.Studio.Core.Notify;

/// <summary>
/// The category a portal was blocked under, kept with the portal's settings. The letter about the block
/// names the deletion date of that category, so the portal keeps it until it is deleted or unblocked,
/// whatever its wallet shows in the meantime.
/// </summary>
/// <remarks>
/// Written on every block, cleared by <c>PUT api/2.0/portal/unblock</c> and read only while the portal is
/// blocked. A portal unblocked some other way keeps the value, but the next block overwrites it before
/// it is read again.
/// </remarks>
public class PortalRetentionBlockSettings : ISettings<PortalRetentionBlockSettings>
{
    /// <summary>The category of the current block, or null for a portal that is not blocked by the policy.</summary>
    public PortalRetentionCategory? Category { get; set; }

    public static Guid ID => new("{0FB77F96-4DFF-4CDF-BF86-A88AA6D0B531}");

    public PortalRetentionBlockSettings GetDefault()
    {
        return new PortalRetentionBlockSettings();
    }

    public DateTime LastModified { get; set; }
}
