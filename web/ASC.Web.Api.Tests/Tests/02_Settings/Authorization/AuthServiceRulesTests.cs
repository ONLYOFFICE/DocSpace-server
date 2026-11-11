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
/// The rules of <c>POST settings/authservice</c> past the role check (all-empty values clear the provider, an
/// unknown provider is refused with 403) and the success path of <c>POST settings/authservice/externaldb/test</c>,
/// which on this standalone stand can reach the stack's own MySQL. The keys that fail a provider's own validation
/// are not covered: every validating provider asks its outside service.
/// </summary>
[Trait("Category", "Settings")]
public class AuthServiceRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // The root password the AppHost gives the MySQL container ("mysql-root-password" in
    // common/ASC.AppHost/Configuration/ConnectionStringManager.cs); the fixture's open connection no longer
    // carries it in its connection string.
    private const string StackMySqlPassword = "root";

    [Fact]
    public async Task SaveAuthKeys_AllValuesEmpty_ClearsStoredKeys()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _settingsAuthorizationApi.SaveAuthKeysAsync(AuthorizationTestData.CreateS3AuthService(), TestContext.Current.CancellationToken);
        var empty = new SaveAuthKeysRequestDto(
            name: "s3",
            props: [new AuthKeyRequest(name: "acesskey", value: ""), new AuthKeyRequest(name: "secretaccesskey", value: "")]);

        // Act
        var result = await _settingsAuthorizationApi.SaveAuthKeysAsync(empty, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
        var services = await _settingsAuthorizationApi.GetAuthServicesAsync(TestContext.Current.CancellationToken);
        services.Response.Single(s => s.Name == "s3").Props.Should().OnlyContain(p => string.IsNullOrEmpty(p.Value));
    }

    [Fact]
    public async Task SaveAuthKeys_UnknownProvider_ThrowsForbidden()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var unknown = new SaveAuthKeysRequestDto(
            name: "no-such-provider",
            props: [new AuthKeyRequest(name: "key", value: "value")]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsAuthorizationApi.SaveAuthKeysAsync(unknown, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task TestExternalDatabaseConnection_ReachableMySql_ReturnsSuccess()
    {
        // Arrange: the stack's own database, as the services reach it
        await _webApiClient.Authenticate(Owner);
        string server;
        string database;
        int port;
        string user;

        await using (var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken))
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
            server = connection.DataSource;
            database = connection.Database;
            port = builder.TryGetValue("Port", out var p) ? Convert.ToInt32(p, CultureInfo.InvariantCulture) : 3306;
            user = builder.TryGetValue("User ID", out var u) ? (string)u : "root";
        }

        var settings = new ExternalDatabaseConnectionRequestDto(
            databaseType: "mysql",
            dbHost: server,
            dbPort: port,
            dbName: database,
            dbUser: user,
            dbPassword: StackMySqlPassword,
            dbSsl: false);

        // Act
        var result = await _settingsAuthorizationApi.TestExternalDatabaseConnectionAsync(settings, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Error.Should().BeNullOrEmpty();
        result.Response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TestExternalDatabaseConnection_SqliteOnStandalone_IsTriedNotRefused()
    {
        // Arrange: a hosted portal refuses SQLite outright; a standalone one looks for the file
        await _webApiClient.Authenticate(Owner);
        var settings = new ExternalDatabaseConnectionRequestDto(databaseType: "sqlite", sqliteFilePath: "missing.db");

        // Act
        var result = await _settingsAuthorizationApi.TestExternalDatabaseConnectionAsync(settings, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Success.Should().BeFalse();
        result.Response.Error.Should().Be("SQLite file not found.");
    }
}
