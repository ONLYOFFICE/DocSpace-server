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

namespace ASC.Web.Api.Tests.Tests._02_Settings.IpRestrictions;

/// <summary>
/// The rules both save routes share (<c>PUT settings/iprestrictions</c> and <c>PUT settings/iprestrictions/settings</c>):
/// an omitted <c>enable</c> means "on when the list is not empty", <c>enable</c> true with an empty list and an
/// entry that is not a single address are 400; and the two reads answer 304 to their conditional headers.
/// A restriction switched on here only binds members: the owner, who makes every call, is exempt
/// (<c>IPSecurity.VerifyAsync</c>), and each test owns its portal.
/// </summary>
[Trait("Category", "Settings")]
public class IpRestrictionsRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string ListPath = "api/2.0/settings/iprestrictions";
    private const string SettingsPath = "api/2.0/settings/iprestrictions/settings";

    [Fact]
    public async Task SaveIpRestrictions_EnableOmittedWithAddresses_SwitchesRestrictionOn()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto("198.51.100.7", false)]), TestContext.Current.CancellationToken);

        // Assert
        var settings = await _ipRestrictionsApi.ReadIpRestrictionsSettingsAsync(TestContext.Current.CancellationToken);
        settings.Response.Enable.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateIpRestrictionsSettings_EnableOmittedWithEmptyList_LeavesRestrictionOff()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _ipRestrictionsApi.UpdateIpRestrictionsSettingsAsync(
            new IpRestrictionsDto([]), TestContext.Current.CancellationToken);

        // Assert
        var settings = await _ipRestrictionsApi.ReadIpRestrictionsSettingsAsync(TestContext.Current.CancellationToken);
        settings.Response.Enable.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveIpRestrictions_EnableTrueWithEmptyList_ThrowsBadRequest(bool settingsRoute)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = new IpRestrictionsDto([], true);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(dto, settingsRoute));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = await _ipRestrictionsApi.ReadIpRestrictionsSettingsAsync(TestContext.Current.CancellationToken);
        settings.Response.Enable.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-an-address", false)]
    [InlineData("192.168.1.0/24", false)]
    [InlineData("10.0.0.1-10.0.0.5", true)]
    [InlineData("", true)]
    public async Task SaveIpRestrictions_EntryNotASingleAddress_ThrowsBadRequestAndStoresNothing(string entry, bool settingsRoute)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = new IpRestrictionsDto([new IpRestrictionEntryDto(entry, false)], false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(dto, settingsRoute));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var stored = await _ipRestrictionsApi.GetIpRestrictionsAsync(TestContext.Current.CancellationToken);
        stored.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIpRestrictions_IfNoneMatchIsTheETagHandedOut_ReturnsNotModified()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto("192.168.1.1", false)], false), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, ListPath);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = ConditionalGet.Header(first, "ETag");
        etag.Should().StartWith("W/");

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, ListPath, "If-None-Match", etag!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    // AC-11: HttpContextExtension.CalculateEtagAsync hashes its MemoryStream without rewinding it, so every list
    // gets the ETag of zero bytes (W/D41D8CD98F00B204E9800998ECF8427E) and a client holding any ETag is told
    // "not modified" after the list changed.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task GetIpRestrictions_ListChangedAfterTheETag_ReturnsList()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto("192.168.1.1", false)], false), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, ListPath);
        var etag = ConditionalGet.Header(first, "ETag");

        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto("192.168.1.2", false)], false), TestContext.Current.CancellationToken);

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, ListPath, "If-None-Match", etag!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadIpRestrictionsSettings_IfModifiedSinceIsTheLastModifiedHandedOut_ReturnsNotModified()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto("192.168.1.1", false)], false), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, SettingsPath);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, SettingsPath, "If-Modified-Since", lastModified!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    private async Task SaveAsync(IpRestrictionsDto dto, bool settingsRoute)
    {
        if (settingsRoute)
        {
            await _ipRestrictionsApi.UpdateIpRestrictionsSettingsAsync(dto, TestContext.Current.CancellationToken);
        }
        else
        {
            await _ipRestrictionsApi.SaveIpRestrictionsAsync(dto, TestContext.Current.CancellationToken);
        }
    }
}
