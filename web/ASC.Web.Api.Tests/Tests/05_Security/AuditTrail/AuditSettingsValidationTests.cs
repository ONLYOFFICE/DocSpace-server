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
/// POST and GET /api/2.0/security/audit/settings/lifetime — the 1..180 day range, that a rejected
/// value stores nothing, and the <c>lastModified</c> both answers carry.
/// </summary>
[Trait("Category", "Security")]
public class AuditSettingsValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(0, 30)]
    [InlineData(181, 30)]
    [InlineData(30, 0)]
    [InlineData(30, 181)]
    public async Task SetAuditSettings_ValueOutsideRange_Returns400AndKeepsStoredValues(int loginHistory, int auditTrail)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _auditTrailDataApi.SetAuditSettingsAsync(new TenantAuditSettingsRequestDto(new SetAuditLifetimeSettingsRequest(20, 25)), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.SetAuditSettingsAsync(
                new TenantAuditSettingsRequestDto(new SetAuditLifetimeSettingsRequest(loginHistory, auditTrail)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var stored = (await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken)).Response;
        stored.LoginHistoryLifeTime.Should().Be(20);
        stored.AuditTrailLifeTime.Should().Be(25);
    }

    [Fact]
    public async Task GetAuditSettings_NeverStored_Defaults180()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = (await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.LoginHistoryLifeTime.Should().Be(180);
        settings.AuditTrailLifeTime.Should().Be(180);
        settings.LastModified.Should().NotBe(default);
    }

    [Fact]
    public async Task SetAuditSettings_BothAnswersCarryLastModifiedOfTheSave()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var saved = (await _auditTrailDataApi.SetAuditSettingsAsync(
            new TenantAuditSettingsRequestDto(new SetAuditLifetimeSettingsRequest(1, 180)),
            TestContext.Current.CancellationToken)).Response;
        var read = (await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        saved.LoginHistoryLifeTime.Should().Be(1);
        saved.AuditTrailLifeTime.Should().Be(180);
        saved.LastModified.Should().NotBe(default);
        read.LastModified.Should().BeCloseTo(saved.LastModified, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetAuditSettings_RecordsAuditSettingsUpdatedEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _auditTrailDataApi.SetAuditSettingsAsync(
            new TenantAuditSettingsRequestDto(new SetAuditLifetimeSettingsRequest(90, 90)),
            TestContext.Current.CancellationToken);

        // Assert
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<AuditEventDto> events;

        while (true)
        {
            events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(
                action: MessageAction.AuditSettingsUpdated,
                cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];

            if (events.Count > 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        events.Should().ContainSingle().Which.UserId.Should().Be(Owner.Id);
    }

    /// <summary>
    /// H4: a request without <c>settings</c> was reported as an out-of-range <c>LoginHistoryLifeTime</c>
    /// rather than as a missing body.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task SetAuditSettings_NoSettings_Returns400NamingTheMissingBody()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act — `settings` is not emitted when left at its default, so the body is `{}`
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.SetAuditSettingsAsync(new TenantAuditSettingsRequestDto(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().NotContain("LoginHistoryLifeTime");
    }
}
