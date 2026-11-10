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

namespace ASC.ClearEvents.Tests.Tests.AuditTrail;

/// <summary>
/// How the <c>clearEvents:auditTrail</c> section is read. A value that does not convert must not throw: the
/// service, and the monolith it is part of, would not start, whether the cleanup is enabled or not.
/// </summary>
public class AuditTrailRetentionReadTests
{
    [Theory]
    [InlineData("period", "1d")]
    [InlineData("batchSize", "many")]
    [InlineData("freeLifeTimeDays", "1y")]
    public void Read_ValueThatDoesNotConvert_ReturnsUnusableSettingsThatKeepEnabled(string key, string value)
    {
        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), (key, value)));

        // Assert
        retention.Enabled.Should().BeTrue("an enabled cleanup has to stay registered to report the error on every pass");
        retention.BindingError.Should().NotBeNullOrEmpty();
        retention.IsUsable.Should().BeFalse();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("on")]
    [InlineData("0")]
    public void Read_EnabledThatDoesNotConvert_ReturnsUnusableSettingsThatAreEnabled(string enabled)
    {
        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", enabled)));

        // Assert: read as disabled, the cleanup would not be registered and would say nothing about why.
        retention.Enabled.Should().BeTrue("a cleanup that was meant to be switched on has to stay registered to report the error on every pass");
        retention.BindingError.Should().ContainEquivalentOf("enabled");
        retention.IsUsable.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData(" False ")]
    public void Read_NotEnabledWithAnotherValueThatDoesNotConvert_StaysDisabled(string? enabled)
    {
        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", enabled), ("period", "1d")));

        // Assert
        retention.Enabled.Should().BeFalse();
        retention.BindingError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Read_ValidSection_ReturnsItsValues()
    {
        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(
            ("enabled", "true"), ("paidLifeTimeDays", "2000"), ("freeLifeTimeDays", "400"), ("period", "12:00:00"), ("batchSize", "500")));

        // Assert
        retention.Should().BeEquivalentTo(new
        {
            Enabled = true,
            PaidLifeTimeDays = 2000,
            FreeLifeTimeDays = 400,
            Period = TimeSpan.FromHours(12),
            BatchSize = 500,
            BindingError = (string?)null,
            IsUsable = true
        });
    }
}
