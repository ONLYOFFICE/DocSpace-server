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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// The rules of the deep link settings beyond the administrators' happy path: who else may set the mode, an
/// unknown mode, the anonymous read and the conditional read (<c>SettingsController.ConfigureDeepLink</c>,
/// <c>GetDeepLinkSettings</c>).
/// </summary>
[Trait("Category", "Settings")]
public class DeepLinkRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string DeepLinkPath = "/api/2.0/settings/deeplink";

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ConfigureDeepLink_MemberWithoutSettingsRight_ThrowsForbiddenAndModeStays(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.Web)), TestContext.Current.CancellationToken);
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.ConfigureDeepLinkAsync(
                new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.App)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        var stored = await _commonSettingsApi.GetDeepLinkSettingsAsync(TestContext.Current.CancellationToken);
        stored.Response.HandlingMode.Should().Be(DeepLinkHandlingMode.Web);
    }

    [Fact]
    public async Task ConfigureDeepLink_ModeOutsideTheEnum_ThrowsBadRequestAndModeStays()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.App)), TestContext.Current.CancellationToken);

        // Act: the SDK enum is sent as its number, so an undefined value reaches the server as 99
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.ConfigureDeepLinkAsync(
                new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto((DeepLinkHandlingMode)99)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var stored = await _commonSettingsApi.GetDeepLinkSettingsAsync(TestContext.Current.CancellationToken);
        stored.Response.HandlingMode.Should().Be(DeepLinkHandlingMode.App);
    }

    [Fact]
    public async Task GetDeepLinkSettings_Anonymous_ReturnsTheStoredMode()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.App)), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(null);

        // Act
        var settings = await _commonSettingsApi.GetDeepLinkSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.HandlingMode.Should().Be(DeepLinkHandlingMode.App);
    }

    [Fact]
    public async Task GetDeepLinkSettings_IfModifiedSinceIsTheLastModifiedHandedOut_ReturnsNotModifiedWithEmptyBody()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.Web)), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(null);

        using var first = await ConditionalGet.GetAsync(_webApiClient, DeepLinkPath);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, DeepLinkPath, "If-Modified-Since", lastModified!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeepLinkSettings_ModeChangedAfterTheLastModified_ReturnsTheNewMode()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.Web)), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, DeepLinkPath);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");

        // Last-Modified has a resolution of one second
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.App)), TestContext.Current.CancellationToken);

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, DeepLinkPath, "If-Modified-Since", lastModified!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("handlingMode");
    }
}
