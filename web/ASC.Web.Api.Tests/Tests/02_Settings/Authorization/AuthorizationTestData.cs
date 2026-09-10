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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Authorization;

/// <summary>
/// Request bodies shared by the authorization-service suites (<c>GetAuthServicesTests</c>,
/// <c>SaveAuthKeysTests</c>, <c>TestExternalDatabaseConnectionTests</c> and their permission
/// counterparts) — kept here, one level up, so both a child test and any sibling in this folder
/// can reach it without a <c>using</c>.
/// </summary>
internal static class AuthorizationTestData
{
    /// <summary>
    /// A fresh S3 authorization service payload with unique key values, so a test can assert the
    /// values it saved are exactly the ones it reads back.
    /// </summary>
    public static AuthServiceRequestsDto CreateS3AuthService()
    {
        var accessKey = Initializer.Faker.Random.AlphaNumeric(20);
        var secretKey = Initializer.Faker.Random.AlphaNumeric(40);

        return new AuthServiceRequestsDto(
            name: "s3",
            props:
            [
                new AuthKey(name: "acesskey", value: accessKey),
                new AuthKey(name: "secretaccesskey", value: secretKey)
            ]);
    }

    /// <summary>
    /// A syntactically valid but unreachable MySQL connection, so
    /// <c>TestExternalDatabaseConnection</c> always resolves with <c>success: false</c> without
    /// depending on a real database being reachable from the portal host.
    /// </summary>
    public static ExternalDatabaseSettings CreateInvalidMysqlSettings()
    {
        return new ExternalDatabaseSettings(
            databaseType: "mysql",
            dbHost: "invalid-host",
            dbPort: 3306,
            dbName: "testdb",
            dbUser: "user",
            dbPassword: "password",
            dbSsl: false);
    }
}
