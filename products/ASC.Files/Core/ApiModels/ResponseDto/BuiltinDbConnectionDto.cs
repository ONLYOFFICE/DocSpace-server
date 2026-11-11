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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The read-only PostgreSQL connection details for the portal's built-in forms database.
/// </summary>
public class BuiltinDbConnectionDto
{
    /// <summary>
    /// Host of the PostgreSQL server, as the DocSpace server reaches it; a name inside the server's own network may need
    /// a route or a tunnel from outside. Empty when the server's connection has no host.
    /// </summary>
    /// <example>db.example.com</example>
    public string Host { get; set; }

    /// <summary>
    /// Port of the PostgreSQL server, as the DocSpace server reaches it.
    /// </summary>
    /// <example>5432</example>
    public int Port { get; set; }

    /// <summary>
    /// The PostgreSQL database of this portal alone; no other portal's tables are in it or visible from it. The
    /// portal's tables are in its own schema there.
    /// </summary>
    /// <example>forms_t42</example>
    public string Database { get; set; }

    /// <summary>
    /// The read-only account for this portal's schema; it can read the form tables but not change them.
    /// </summary>
    /// <example>forms_t42_ro</example>
    public string User { get; set; }

    /// <summary>
    /// Password of the read-only account. It changes when the account has to be set up again; ask for the details again
    /// when it stops signing in.
    /// </summary>
    /// <example>Qp7vT2k9LmXa</example>
    public string Password { get; set; }

    /// <summary>
    /// The same details as an Npgsql connection string, with the portal's schema as the search path so that tables are
    /// found by their names alone.
    /// </summary>
    /// <example>Host=db.example.com;Port=5432;Database=forms_t42;Username=forms_t42_ro;Password=Qp7vT2k9LmXa;Search Path=tenant_42;Timezone=UTC;Maximum Pool Size=5;Connection Idle Lifetime=60</example>
    public string ConnectionString { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class BuiltinDbConnectionDtoMapper
{
    [MapProperty(nameof(FormsDbCredentials.RoUser), nameof(BuiltinDbConnectionDto.User))]
    [MapProperty(nameof(FormsDbCredentials.RoPassword), nameof(BuiltinDbConnectionDto.Password))]
    [MapProperty(nameof(FormsDbCredentials.RoConnectionString), nameof(BuiltinDbConnectionDto.ConnectionString))]
    public static partial BuiltinDbConnectionDto Map(this FormsDbCredentials source);
}
