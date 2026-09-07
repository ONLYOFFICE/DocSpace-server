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

namespace ASC.Tests.Common.ApiFactories;

/// <summary>
/// The Aspire resource names, as declared in <c>common/ASC.AppHost/Program.cs</c>. A fixture waits
/// for these to go healthy and resolves their base addresses.
/// </summary>
public static class ResourceNames
{
    /// <summary>Portal registration. Always started: every test needs its own portal.</summary>
    public const string ApiSystem = "onlyoffice-apisystem";

    /// <summary>Authentication and portal settings. Always started: every test signs in.</summary>
    public const string WebApi = "onlyoffice-web-api";

    public const string Files = "onlyoffice-files";
    public const string People = "onlyoffice-people";
    public const string Ai = "onlyoffice-ai";

    /// <summary>The identity (OAuth2) containers — Spring services built from common/ASC.Identity.</summary>
    public const string IdentityRegistration = "onlyoffice-identity-registration";
    public const string IdentityAuthorization = "onlyoffice-identity-authorization";

    /// <summary>
    /// The DocSpace database, for a suite that talks to it directly rather than through a service —
    /// also the name its connection string is published under.
    /// </summary>
    public const string Database = "docspace";

    /// <summary>The mail server the letter tests deliver to. Endpoints: <c>smtp</c> and <c>http</c>.</summary>
    public const string MailPit = "mailpit";
}
