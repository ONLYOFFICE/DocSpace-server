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
/// How the <c>clearEvents:auditTrail</c> section is read: every value through <c>ConfigurationValueExtensions</c>, so a
/// missing key gives the default, an unreadable one the default and a warning, one out of bounds the nearest bound and
/// a warning. Nothing throws, so the service and the monolith it is part of start whatever the section holds, and the
/// settings that come back are always usable.
/// </summary>
public class AuditTrailRetentionReadTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData(" TRUE ", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("on", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("off", false)]
    public void Read_Enabled_TakesTheSpellingsOfAFlag(string enabled, bool expected)
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", enabled)), logger);

        // Assert
        retention.Enabled.Should().Be(expected);
        retention.IsUsable.Should().BeTrue();
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Read_EnabledThatIsNoFlag_IsOffWithAWarning()
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "maybe")), logger);

        // Assert
        retention.Enabled.Should().BeFalse();
        WarningsAbout(logger, "Enabled").Should().ContainSingle();
    }

    [Theory]
    [InlineData("period", "1d")]
    [InlineData("batchSize", "many")]
    [InlineData("readPageSize", "10k")]
    [InlineData("freeLifeTimeDays", "1y")]
    [InlineData("paidGracePeriodDays", "a week")]
    public void Read_ValueThatDoesNotParse_KeepsTheDefaultWithAWarning(string key, string value)
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), (key, value)), logger);

        // Assert
        retention.Should().BeEquivalentTo(Usable(new AuditTrailRetentionConfiguration { Enabled = true }));
        WarningsAbout(logger, key).Should().ContainSingle();
    }

    [Theory]
    [InlineData("readPageSize", "200000", nameof(AuditTrailRetentionConfiguration.ReadPageSize), AuditTrailRetentionConfiguration.MaxReadPageSize)]
    [InlineData("readPageSize", "0", nameof(AuditTrailRetentionConfiguration.ReadPageSize), 1)]
    [InlineData("batchSize", "20000", nameof(AuditTrailRetentionConfiguration.BatchSize), AuditTrailRetentionConfiguration.MaxBatchSize)]
    [InlineData("batchSize", "-5", nameof(AuditTrailRetentionConfiguration.BatchSize), 1)]
    [InlineData("paidGracePeriodDays", "-1", nameof(AuditTrailRetentionConfiguration.PaidGracePeriodDays), 0)]
    [InlineData("paidGracePeriodDays", "36500", nameof(AuditTrailRetentionConfiguration.PaidGracePeriodDays), AuditTrailRetentionConfiguration.MaxPaidGracePeriodDays)]
    [InlineData("paidLifeTimeDays", "1000000", nameof(AuditTrailRetentionConfiguration.PaidLifeTimeDays), AuditTrailRetentionConfiguration.MaxLifeTimeDays)]
    [InlineData("freeLifeTimeDays", "30", nameof(AuditTrailRetentionConfiguration.FreeLifeTimeDays), AuditTrailRetentionConfiguration.MinLifeTimeDays)]
    public void Read_ValueOutOfBounds_TakesTheNearestBoundWithAWarning(string key, string value, string property, int expected)
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), (key, value)), logger);

        // Assert
        typeof(AuditTrailRetentionConfiguration).GetProperty(property)!.GetValue(retention).Should().Be(expected);
        retention.IsUsable.Should().BeTrue();
        WarningsAbout(logger, key).Should().ContainSingle();
    }

    [Theory]
    [InlineData("00:30:00", true)]
    [InlineData("100.00:00:00", false)]
    public void Read_PeriodOutOfBounds_TakesTheNearestBoundWithAWarning(string period, bool belowTheMinimum)
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), ("period", period)), logger);

        // Assert: an hour at the least, and at most what Task.Delay accepts (about 49.7 days).
        retention.Period.Should().Be(belowTheMinimum ? AuditTrailRetentionConfiguration.MinPeriod : AuditTrailRetentionConfiguration.MaxPeriod);
        WarningsAbout(logger, "period").Should().ContainSingle();
    }

    [Theory]
    [InlineData("24:00:00", 24)]
    [InlineData("7", 7)]
    public void Read_PeriodThatReadsAsDays_IsKeptWithAWarning(string period, int days)
    {
        // Arrange: written as a day or as a number of hours, but read by TimeSpan as days.
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), ("period", period)), logger);

        // Assert
        retention.Period.Should().Be(TimeSpan.FromDays(days));
        WarningsAbout(logger, "period").Should().ContainSingle();
    }

    [Fact]
    public void Read_FreeLifeTimeAbovePaid_RaisesThePaidOneWithAWarning()
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(
            RetentionSection.Of(("enabled", "true"), ("freeLifeTimeDays", "800"), ("paidLifeTimeDays", "400")),
            logger);

        // Assert: the cleanup deletes, so of two conflicting life times it keeps the longer for both.
        retention.FreeLifeTimeDays.Should().Be(800);
        retention.PaidLifeTimeDays.Should().Be(800);
        retention.IsUsable.Should().BeTrue();
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Has("freeLifeTimeDays"));
    }

    [Fact]
    public void Read_EmptySection_GivesTheDefaultsWithoutAWarning()
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(), logger);

        // Assert
        retention.Should().BeEquivalentTo(Usable(new AuditTrailRetentionConfiguration()));
        retention.PaidGracePeriodDays.Should().Be(7);
        retention.ReadPageSize.Should().Be(10 * retention.BatchSize);
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Read_ValidSection_ReturnsItsValues()
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailRetentionReadTests>();

        // Act
        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(
            ("enabled", "true"), ("paidLifeTimeDays", "2000"), ("freeLifeTimeDays", "400"), ("paidGracePeriodDays", "14"), ("period", "12:00:00"), ("batchSize", "500"), ("readPageSize", "5000")),
            logger);

        // Assert
        retention.Should().BeEquivalentTo(new
        {
            Enabled = true,
            PaidLifeTimeDays = 2000,
            FreeLifeTimeDays = 400,
            PaidGracePeriodDays = 14,
            Period = TimeSpan.FromHours(12),
            BatchSize = 500,
            ReadPageSize = 5000,
            IsUsable = true
        });
        logger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("false", false)]
    [InlineData(" OFF ", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData(" Yes ", true)]
    [InlineData("maybe", true)]
    public void IsRequested_RegistersEveryValueButAnOffOne(string? enabled, bool expected)
    {
        // Act
        var requested = AuditTrailRetentionConfiguration.IsRequested(RetentionSection.Of(("enabled", enabled)));

        // Assert: an unreadable value is registered, so that the service says why the cleanup stays off.
        requested.Should().Be(expected);
    }

    /// <summary>The warnings of the configuration reader about <paramref name="key"/>, whatever its case.</summary>
    private static List<LogEntry> WarningsAbout(RecordingLogger<AuditTrailRetentionReadTests> logger, string key)
    {
        return logger.Entries
            .Where(e => e.Level == LogLevel.Warning && e.Values.GetValueOrDefault("key") is string read && read.EndsWith(":" + key, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>What <paramref name="expected"/> looks like once it is checked: usable.</summary>
    private static object Usable(AuditTrailRetentionConfiguration expected)
    {
        expected.IsUsable.Should().BeTrue();

        return expected;
    }
}
