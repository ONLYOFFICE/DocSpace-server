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
/// What the retention policy keeps about a portal, with the portal's settings. While the portal is active:
/// the last warning it was sent - the block keeps to the day that warning named, whatever schedule the
/// portal falls under afterwards - and the last sign-in the policy has seen, which outlives the login
/// history the portal's audit settings purge. Once it is blocked: the category it was blocked under - the
/// letter about the block names the deletion date of that category, so the portal keeps it until it is
/// deleted or unblocked, whatever its wallet shows in the meantime - and the day the last reminder before
/// the deletion went out.
/// </summary>
/// <remarks>
/// A block replaces the whole record, and <c>PUT api/2.0/portal/unblock</c> clears it: what was kept about
/// the active portal is of a count that is over either way. The block's part is read only while the portal
/// is blocked; a portal unblocked some other way keeps it, but the next block overwrites it before it is
/// read again.
/// </remarks>
public class PortalRetentionSettings : ISettings<PortalRetentionSettings>
{
    /// <summary>The category of the current block, or null for a portal that is not blocked by the policy.</summary>
    public PortalRetentionCategory? Category { get; set; }

    /// <summary>
    /// The day the last reminder before the deletion went out, or null while it has not: the portal is not
    /// deleted before a full notice period has passed since.
    /// </summary>
    public DateTime? FinalNoticeSentOn { get; set; }

    /// <summary>
    /// The last successful sign-in the policy has seen on the active portal, kept only while it is later
    /// than the last audit event: the audit trail is never purged, the login history is.
    /// </summary>
    public DateTime? LastLoginOn { get; set; }

    /// <summary>The day the last warning before the block went out, or null while none has.</summary>
    public DateTime? WarnedOn { get; set; }

    /// <summary>The day of the block that warning named.</summary>
    public DateTime? WarnedBlockOn { get; set; }

    public static Guid ID => new("{0FB77F96-4DFF-4CDF-BF86-A88AA6D0B531}");

    public PortalRetentionSettings GetDefault()
    {
        return new PortalRetentionSettings();
    }

    public DateTime LastModified { get; set; }
}
