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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Statistics;

/// <summary>
/// GET /api/2.0/settings/statistics/spaceusage/{id} — the per-module storage usage breakdown, and its access
/// control. Needs the portal-settings right; an unknown module id answers with an empty list rather than an error.
/// </summary>
/// <remarks>
/// <see cref="PortalClients"/> does not expose <c>StatisticsApi</c> yet, so the typed client is built here on the
/// per-test Web.Api <see cref="HttpClient"/>. The TypeScript suite saw an empty list for the Documents module id
/// because that build registered no products; whether this host's Web.Api registers any is not grounded in the
/// repo, so the real module id is only asserted to succeed, and the empty list is asserted for a made-up id.
/// </remarks>
[Trait("Category", "Settings")]
public class SpaceUsageStatisticsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private static readonly Guid _documentsModuleId = new("e67be73d-f9ae-4ce1-8fec-1880cb518cb4");

    private StatisticsApi? _statisticsApi;

    private StatisticsApi Statistics => _statisticsApi ??= new StatisticsApi(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    [Fact]
    public async Task GetSpaceUsageStatistics_OwnerRealModuleId_ReturnsList()
    {
        // Act
        var statistics = await Statistics.GetSpaceUsageStatisticsAsync(_documentsModuleId, TestContext.Current.CancellationToken);

        // Assert
        statistics.StatusCode.Should().Be(200);
        statistics.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSpaceUsageStatistics_OwnerUnknownModuleId_ReturnsEmptyList()
    {
        // Act
        var statistics = await Statistics.GetSpaceUsageStatisticsAsync(
            new Guid("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        // Assert
        statistics.Response.Should().BeEmpty();
    }

    // The route is constrained to a Guid, which the typed client cannot violate, so this goes raw.
    [Fact]
    public async Task GetSpaceUsageStatistics_MalformedId_ReturnsNotFound()
    {
        // Act
        using var response = await _webApi.GetAsync(
            "api/2.0/settings/statistics/spaceusage/not-a-guid", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpaceUsageStatistics_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Statistics.GetSpaceUsageStatisticsAsync(_documentsModuleId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSpaceUsageStatistics_DocSpaceAdmin_ReturnsList()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var statistics = await Statistics.GetSpaceUsageStatisticsAsync(_documentsModuleId, TestContext.Current.CancellationToken);

        // Assert
        statistics.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSpaceUsageStatistics_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Statistics.GetSpaceUsageStatisticsAsync(_documentsModuleId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
