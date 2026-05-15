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

using System.Security.Claims;

namespace ASC.Common.Security.Authorizing;

public static class AuthConstants
{
    public static readonly Role DocSpaceAdmin = new(new Guid("cd84e66b-b803-40fc-99f9-b2969a54a1de"), "DocSpaceAdmin");
    public static readonly Role Everyone = new(new Guid("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e"), "Everyone");
    public static readonly Role RoomAdmin = new(new Guid("abef62db-11a8-4673-9d32-ef1d8af19dc0"), "RoomAdmin");
    public static readonly Role User = new(new Guid("88f11e7c-7407-4bea-b4cb-070010cdbb6b"), "User");
    public static readonly Role Guest = new(new Guid("aced04fa-dd96-4b35-af3e-346bf1eb972d"), "Guest");
    public static readonly Role Member = new(new Guid("ba74ca02-873f-43dc-8470-8620c156bc67"), "Member");
    public static readonly Role Owner = new(new Guid("bba32183-a14d-48ed-9d39-c6b4d8925fbf"), "Owner");
    public static readonly Role Self = new(new Guid("5d5b7260-f7f7-49f1-a1c9-95fbb6a12604"), "Self");

    public static readonly Claim Claim_ScopeGlobalWrite = new("scope", "*:write");
    public static readonly Claim Claim_ScopeGlobalRead = new("scope", "*:read");
}