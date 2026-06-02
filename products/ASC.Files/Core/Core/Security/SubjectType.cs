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

namespace ASC.Files.Core.Security;

/// <summary>
/// The filter type of the access rights.
/// </summary>
[Flags]
public enum ShareFilterType
{
    [Description("User or group")]
    UserOrGroup = 0,

    [Description("Invitation link")]
    InvitationLink = 1,

    [Description("External link")]
    ExternalLink = 2,

    [Description("Additional external link")]
    AdditionalExternalLink = 4,

    [Description("Primary external link")]
    PrimaryExternalLink = 8,
    Link = InvitationLink | ExternalLink | AdditionalExternalLink | PrimaryExternalLink,

    [Description("User")]
    User = 16,

    [Description("Group")]
    Group = 32
}

/// <summary>
/// The subject type of the access right.
/// </summary>
public enum SubjectType
{
    [Description("User")]
    User = 0,

    [Description("External link")]
    ExternalLink = 1,

    [Description("Group")]
    Group = 2,

    [Description("Invitation link")]
    InvitationLink = 3,

    [Description("Primary external link")]
    PrimaryExternalLink = 4
}

/// <summary>
/// The projected share information of an entry used to derive the Shared, SharedForUser and SharedExternal flags.
/// </summary>
public class UserShareInfo
{
    /// <summary>
    /// The subject type of the share record.
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// The internal flag of the share link options. Null when the share record has no options.
    /// </summary>
    public bool? Internal { get; set; }
}