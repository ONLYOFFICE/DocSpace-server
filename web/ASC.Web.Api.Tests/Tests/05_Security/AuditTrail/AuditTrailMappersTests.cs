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
/// GET /api/2.0/security/audit/mappers — the mapping of product/module to the audit actions
/// raised in it. The controller returns a plain <c>object</c> (a nested anonymous-type tree), so
/// the generated <see cref="ObjectWrapper.Response"/> deserializes it as a
/// <c>Newtonsoft.Json.Linq.JObject</c>.
/// </summary>
[Trait("Category", "Security")]
public class AuditTrailMappersTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAuditTrailMappers_Owner_ReturnsMappers()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var mappers = await _auditTrailDataApi.GetAuditTrailMappersAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        mappers.Response.Should().NotBeNull();
        mappers.Response.Should().BeAssignableTo<Newtonsoft.Json.Linq.JToken>();
    }

    [Fact]
    public async Task GetAuditTrailMappers_DocSpaceAdmin_ReturnsMappers()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var mappers = await _auditTrailDataApi.GetAuditTrailMappersAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        mappers.Response.Should().NotBeNull();
        mappers.Response.Should().BeAssignableTo<Newtonsoft.Json.Linq.JToken>();
    }
}
