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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The connection parameters of an external database to test.
/// </summary>
public class ExternalDatabaseConnectionRequestDto
{
    /// <summary>
    /// The engine of the external database: `mysql`, or `sqlite` on a Standalone installation.
    /// </summary>
    /// <example>mysql</example>
    [JsonPropertyName("databaseType")]
    public string DatabaseType { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the engine is taken from `databaseType`.
    /// </summary>
    /// <example>0</example>
    public ExternalDatabaseType? DatabaseTypeEnum { get; set; }

    /// <summary>
    /// The host name or the IP address of the database server.
    /// </summary>
    /// <example>localhost</example>
    [JsonPropertyName("dbHost")]
    public string Host { get; set; }

    /// <summary>
    /// The port the database server listens on.
    /// </summary>
    /// <example>3306</example>
    [JsonPropertyName("dbPort")]
    public int Port { get; set; }

    /// <summary>
    /// The name of the database to connect to.
    /// </summary>
    /// <example>docspace</example>
    [JsonPropertyName("dbName")]
    public string DatabaseName { get; set; }

    /// <summary>
    /// The user name to connect with.
    /// </summary>
    /// <example>root</example>
    [JsonPropertyName("dbUser")]
    public string User { get; set; }

    /// <summary>
    /// The password to connect with.
    /// </summary>
    /// <example>my-secret-password</example>
    [JsonPropertyName("dbPassword")]
    public string Password { get; set; }

    /// <summary>
    /// Whether the connection is secured with SSL.
    /// </summary>
    /// <example>false</example>
    [JsonPropertyName("dbSsl")]
    public bool UseSsl { get; set; }

    /// <summary>
    /// The path to the database file, used by the SQLite engine only.
    /// </summary>
    /// <example>/var/lib/docspace/external.db</example>
    [JsonPropertyName("sqliteFilePath")]
    public string SqliteFilePath { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class ExternalDatabaseConnectionRequestDtoMapper
{
    [MapperIgnoreSource(nameof(ExternalDatabaseConnectionRequestDto.DatabaseTypeEnum))]
    public static partial ExternalDatabaseSettings Map(this ExternalDatabaseConnectionRequestDto source);
}
