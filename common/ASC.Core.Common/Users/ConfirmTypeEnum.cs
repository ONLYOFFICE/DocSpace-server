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

namespace ASC.Web.Studio.Utility;

/// <summary>
/// The confirmation email type.
/// </summary>
//  emp-invite - confirm ivite by email
//  portal-suspend - confirm portal suspending - Tenant.SetStatus(TenantStatus.Suspended)
//  portal-continue - confirm portal continuation  - Tenant.SetStatus(TenantStatus.Active)
//  portal-remove - confirm portal deletation - Tenant.SetStatus(TenantStatus.RemovePending)
//  DnsChange - change Portal Address and/or Custom domain name
[JsonConverter(typeof(JsonStringEnumConverter<ConfirmType>))]
[EnumExtensions]
public enum ConfirmType
{
    [Description("Emp invite")]
    EmpInvite,

    [Description("Link invite")]
    LinkInvite,

    [Description("Portal suspend")]
    PortalSuspend,

    [Description("Portal continue")]
    PortalContinue,

    [Description("Portal remove")]
    PortalRemove,

    [Description("Dns change")]
    DnsChange,

    [Description("Portal owner change")]
    PortalOwnerChange,

    [Description("Activation")]
    Activation,

    [Description("Email change")]
    EmailChange,

    [Description("Email activation")]
    EmailActivation,

    [Description("Password change")]
    PasswordChange,

    [Description("Profile remove")]
    ProfileRemove,

    [Description("Phone activation")]
    PhoneActivation,

    [Description("Phone auth")]
    PhoneAuth,

    [Description("Auth")]
    Auth,

    [Description("Tfa activation")]
    TfaActivation,

    [Description("Tfa auth")]
    TfaAuth,

    [Description("Wizard")]
    Wizard,

    [Description("Guest share link")]
    GuestShareLink
}