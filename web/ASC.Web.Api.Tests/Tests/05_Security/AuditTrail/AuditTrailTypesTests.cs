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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// GET /api/2.0/security/audit/types — the catalogue of audit trail action, action type, product,
/// module and entry types the portal recognizes. Only an Owner or a DocSpaceAdmin may call it. The
/// controller returns a plain <c>object</c>, so the generated <see cref="ObjectWrapper.Response"/>
/// — itself typed <c>object</c> — deserializes it as a <c>Newtonsoft.Json.Linq.JObject</c>.
/// </summary>
[Trait("Category", "Security")]
public class AuditTrailTypesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAuditTrailTypes_Owner_ReturnsAllTypeCatalogues()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var types = await _auditTrailDataApi.GetAuditTrailTypesAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertCatalogues(types, requireNonEmpty: true);
    }

    [Fact]
    public async Task GetAuditTrailTypes_DocSpaceAdmin_ReturnsAllTypeCatalogues()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var types = await _auditTrailDataApi.GetAuditTrailTypesAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertCatalogues(types, requireNonEmpty: false);
    }

    [Fact]
    public async Task GetAuditTrailTypes_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.GetAuditTrailTypesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetAuditTrailTypes_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.GetAuditTrailTypesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    private static void AssertCatalogues(ObjectWrapper types, bool requireNonEmpty)
    {
        types.Response.Should().NotBeNull();
        var catalogues = (Newtonsoft.Json.Linq.JObject)types.Response;

        foreach (var property in new[] { "actions", "actionTypes", "productTypes", "moduleTypes", "entryTypes" })
        {
            var array = catalogues[property] as Newtonsoft.Json.Linq.JArray;
            array.Should().NotBeNull($"'{property}' should be an array");

            if (requireNonEmpty)
            {
                array!.Should().NotBeEmpty($"'{property}' should list at least one type");
            }
        }

        if (requireNonEmpty)
        {
            ((Newtonsoft.Json.Linq.JArray)catalogues["actions"]!).Select(t => t.ToString()).Should().Contain("FileCreated");
            ((Newtonsoft.Json.Linq.JArray)catalogues["productTypes"]!).Select(t => t.ToString()).Should().Contain("Documents");
        }
    }
}
