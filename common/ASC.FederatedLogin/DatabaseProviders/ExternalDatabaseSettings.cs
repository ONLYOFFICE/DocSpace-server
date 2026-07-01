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

#nullable enable
namespace ASC.FederatedLogin.DatabaseProviders;

[EnumExtensions]
public enum ExternalDatabaseType { MySql, Sqlite, PostgreSql }

public record ConnectionTestResult(bool Success, string? Error = null)
{
    public static ConnectionTestResult Ok() => new(true);
    public static ConnectionTestResult Failure(string error) => new(false, error);
}
#nullable disable

public class ExternalDatabaseSettings
{
    [JsonPropertyName("databaseType")]
    public string DatabaseType { get; set; }

    public ExternalDatabaseType? DatabaseTypeEnum =>
        ExternalDatabaseTypeExtensions.TryParse(DatabaseType, ignoreCase: true, out var t) ? t : null;

    [JsonPropertyName("dbHost")]
    public string Host { get; set; }

    [JsonPropertyName("dbPort")]
    public int Port { get; set; }

    [JsonPropertyName("dbName")]
    public string DatabaseName { get; set; }

    [JsonPropertyName("dbUser")]
    public string User { get; set; }

    [JsonPropertyName("dbPassword")]
    public string Password { get; set; }

    [JsonPropertyName("dbSsl")]
    public bool UseSsl { get; set; }

    [JsonPropertyName("sqliteFilePath")]
    public string SqliteFilePath { get; set; }
}

