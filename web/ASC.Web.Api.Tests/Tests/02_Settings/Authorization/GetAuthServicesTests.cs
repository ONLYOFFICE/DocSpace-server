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
/// GET /api/2.0/settings/authservice — the catalogue of third-party storage/authorization
/// services, including whichever keys were last saved for them.
///
/// The TypeScript suite calls <c>paymentsApi.setupPayment()</c> before this endpoint, which pays
/// for the portal through an external billing service. That call is a no-op against a local
/// portal (the TS helper itself early-returns when <c>isLocal</c>), which is exactly what this
/// Aspire-hosted integration portal is, so it is intentionally not ported.
/// </summary>
[Trait("Category", "Settings")]
public class GetAuthServicesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAuthServices_Owner_AfterSavingS3Keys_ReturnsSavedValues()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var s3AuthService = AuthorizationTestData.CreateS3AuthService();
        await _settingsAuthorizationApi.SaveAuthKeysAsync(s3AuthService, TestContext.Current.CancellationToken);

        // Act
        var services = await _settingsAuthorizationApi.GetAuthServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertServicesAndS3Keys(services, s3AuthService);
    }

    [Fact]
    public async Task GetAuthServices_DocSpaceAdmin_AfterOwnerSavesS3Keys_ReturnsSavedValues()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var s3AuthService = AuthorizationTestData.CreateS3AuthService();
        await _settingsAuthorizationApi.SaveAuthKeysAsync(s3AuthService, TestContext.Current.CancellationToken);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var services = await _settingsAuthorizationApi.GetAuthServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertServicesAndS3Keys(services, s3AuthService);
    }

    // The integration-test stack is the server version, so the external database offers SQLite: the type list has it and
    // the file path field is in the form. Every field that depends on the type lists the values it shows for, and its
    // single value is the first of them.
    [Fact]
    public async Task GetAuthServices_ServerVersion_ExternalDatabaseOffersSqliteAndListsDependsOnValues()
    {
        await _webApiClient.Authenticate(Owner);

        var services = await _settingsAuthorizationApi.GetAuthServicesAsync(TestContext.Current.CancellationToken);
        var externalDb = services.Response.Single(s => s.Name == "externaldb");
        externalDb.Props.Single(p => p.Name == "databaseType").Options.Should().Contain("sqlite");
        externalDb.Props.Should().Contain(p => p.Name == "sqliteFilePath");

        // The AuthKeyDto of the SDK 4.1.0 has no dependsOnValues yet, so the raw JSON is read.
        var raw = new RawApiClient(_webApiClient);
        using var response = await raw.GetAsync("api/2.0/settings/authservice", TestContext.Current.CancellationToken);
        var rawServices = await raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
        var dependent = rawServices.EnumerateArray().Single(s => s.GetProperty("name").GetString() == "externaldb")
            .GetProperty("props").EnumerateArray()
            .Where(p => p.TryGetProperty("dependsOn", out var dependsOn) && dependsOn.ValueKind == JsonValueKind.String)
            .ToList();

        dependent.Should().NotBeEmpty();
        foreach (var prop in dependent)
        {
            var name = prop.GetProperty("name").GetString();
            var values = prop.GetProperty("dependsOnValues").EnumerateArray().Select(v => v.GetString()).ToList();
            values.Should().NotBeEmpty("{0} depends on the database type", name);
            prop.GetProperty("dependsOnValue").GetString().Should().Be(values[0], "{0} keeps the first value for older clients", name);
        }
    }

    private static void AssertServicesAndS3Keys(AuthServiceArrayWrapper services, SaveAuthKeysRequestDto s3AuthService)
    {
        services.StatusCode.Should().Be(200);
        services.Response.Should().NotBeNullOrEmpty();

        foreach (var service in services.Response)
        {
            service.Name.Should().NotBeNull();
            service.Title.Should().NotBeNull();
        }

        var serviceNames = services.Response.Select(s => s.Name).ToList();
        serviceNames.Should().Contain(["s3", "dropbox", "box", "google", "googlecloud", "telegram"]);

        var s3Service = services.Response.Single(s => s.Name == "s3");
        var accessKey = s3Service.Props.Should().ContainSingle(p => p.Name == "acesskey").Subject;
        accessKey.Value.Should().Be(s3AuthService.Props[0].Value);
    }
}
