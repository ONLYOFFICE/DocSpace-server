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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The configuration readers every tuning value goes through (<c>.claude/rules/configuration.md</c>): a missing key
/// gives the default, a value that does not parse gives the default and a warning, a value outside the bounds is
/// moved to the nearest bound with a warning, and none of it throws. The cases come from review findings: a typo in
/// a cleaner period that failed the hosted service, <c>enabled=1</c> that left a cleanup off without a word, an idle
/// interval of zero that would drop every table in use.
/// </summary>
public class ConfigurationValueExtensionsTests
{
    private const string Key = "section:value";

    private static readonly TimeSpan _defaultInterval = TimeSpan.FromDays(30);
    private static readonly TimeSpan _minInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan _maxInterval = TimeSpan.FromDays(365);

    /// <summary>
    /// The configured text, the interval read from it and whether a warning was written. Two spellings read as days
    /// that may have been meant otherwise, so they are kept as read and get a warning: a bare number (<c>"60"</c> is
    /// 60 days) and three parts whose first is 24 or more, read as days:hours:minutes (<c>"24:00:00"</c> is 24 days,
    /// <c>"36:15:00"</c> is 36 days and 15 hours). <c>"24:30:00"</c> and <c>"24:00"</c> do not parse at all. A value
    /// that is also out of bounds gets the one warning about the bound.
    /// </summary>
    public static TheoryData<string?, TimeSpan, bool> Intervals => new()
    {
        { null, TimeSpan.FromDays(30), false },
        { "", TimeSpan.FromDays(30), false },
        { "   ", TimeSpan.FromDays(30), false },
        { "7.00:00:00", TimeSpan.FromDays(7), false },
        { " 2.12:00:00 ", new TimeSpan(2, 12, 0, 0), false },
        { "24.00:00:00", TimeSpan.FromDays(24), false },
        { "60", TimeSpan.FromDays(60), true },
        { "24:00:00", TimeSpan.FromDays(24), true },
        { "48:00:00", TimeSpan.FromDays(48), true },
        { "36:15:00", new TimeSpan(36, 15, 0, 0), true },
        { "1000", TimeSpan.FromDays(365), true },
        { "24:30:00", TimeSpan.FromDays(30), true },
        { "24:00", TimeSpan.FromDays(30), true },
        { "30d", TimeSpan.FromDays(30), true },
        { "1 day", TimeSpan.FromDays(30), true },
        { "00:30:00", TimeSpan.FromDays(1), true },
        { "0", TimeSpan.FromDays(1), true },
        { "-1:00:00", TimeSpan.FromDays(1), true },
        { "00:00:00.001", TimeSpan.FromDays(1), true },
        { "1000.00:00:00", TimeSpan.FromDays(365), true }
    };

    [Theory]
    [MemberData(nameof(Intervals))]
    public void ReadInterval_ReturnsTheValueWithinItsBounds(string? configured, TimeSpan expected, bool warned)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadInterval(Key, _defaultInterval, _minInterval, _maxInterval, logger);

        result.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    /// <summary>
    /// The value is read in the invariant culture, so one text means one interval on every host. Parsed in the
    /// host's culture, <c>00:00:01,5</c> would be 1.5 seconds on a Russian host and unreadable on an English one.
    /// </summary>
    [Fact]
    public void ReadInterval_WithTheHostCultureSeparator_IsNotReadInThatCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

        try
        {
            var logger = new RecordingLogger();

            var result = Configuration("00:00:01,5").ReadInterval(Key, TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromMinutes(1), logger);

            result.Should().Be(TimeSpan.FromSeconds(1));
            logger.Warnings.Should().ContainSingle();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null, 100, false)]
    [InlineData("250", 250, false)]
    [InlineData(" 10 ", 10, false)]
    [InlineData("1000", 1000, false)]
    [InlineData("abc", 100, true)]
    [InlineData("10.5", 100, true)]
    [InlineData("99999999999", 100, true)]
    [InlineData("0", 1, true)]
    [InlineData("-5", 1, true)]
    [InlineData("1001", 1000, true)]
    public void ReadInt_ReturnsTheValueWithinItsBounds(string? configured, int expected, bool warned)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadInt(Key, 100, 1, 1000, logger);

        result.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    /// <summary>
    /// A size limit that protects the host (an extraction ceiling) has a finite default and a finite maximum, so
    /// <see cref="long.MaxValue"/> in the configuration cannot switch the protection off.
    /// </summary>
    [Theory]
    [InlineData(null, 10_737_418_240L, false)]
    [InlineData("1073741824", 1_073_741_824L, false)]
    [InlineData("9223372036854775807", 107_374_182_400L, true)]
    [InlineData("-1", 1_048_576L, true)]
    [InlineData("10GB", 10_737_418_240L, true)]
    public void ReadLong_ReturnsTheValueWithinItsBounds(string? configured, long expected, bool warned)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadLong(Key, 10_737_418_240L, 1_048_576L, 107_374_182_400L, logger);

        result.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    /// <summary>
    /// A ratio or a factor. <c>NaN</c> and the infinities parse as numbers but bound nothing, so they are unreadable,
    /// and a comma is not a decimal separator on any host.
    /// </summary>
    [Theory]
    [InlineData(null, 0.5, false)]
    [InlineData("0.25", 0.25, false)]
    [InlineData(" 1 ", 1.0, false)]
    [InlineData("1e-1", 0.1, false)]
    [InlineData("0,25", 0.5, true)]
    [InlineData("NaN", 0.5, true)]
    [InlineData("Infinity", 0.5, true)]
    [InlineData("-Infinity", 0.5, true)]
    [InlineData("half", 0.5, true)]
    [InlineData("0", 0.01, true)]
    [InlineData("-0.5", 0.01, true)]
    [InlineData("1.5", 1.0, true)]
    public void ReadDouble_ReturnsTheValueWithinItsBounds(string? configured, double expected, bool warned)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadDouble(Key, 0.5, 0.01, 1.0, logger);

        result.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData(" ON ", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("Off", false)]
    public void ReadBool_ReadsEveryCommonSpelling(string configured, bool expected)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadBool(Key, !expected, logger);

        result.Should().Be(expected);
        logger.Warnings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("maybe", true)]
    [InlineData("enabled", true)]
    public void ReadBool_WithoutAReadableValue_KeepsTheDefault(string? configured, bool warned)
    {
        var logger = new RecordingLogger();

        var result = Configuration(configured).ReadBool(Key, true, logger);

        result.Should().BeTrue();
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    /// <summary>
    /// The default a flag falls back to is written as the configuration spells it, so it can be copied back as is.
    /// </summary>
    [Theory]
    [InlineData(true, "the default true is used")]
    [InlineData(false, "the default false is used")]
    public void ReadBool_Warning_SpellsTheDefaultAsTheConfigurationDoes(bool @default, string expected)
    {
        var logger = new RecordingLogger();

        Configuration("maybe").ReadBool(Key, @default, logger);

        logger.Warnings.Should().ContainSingle().Which.Should().Contain(expected);
    }

    /// <summary>
    /// The warning names the key and the text that was found, so the administrator can find the line to fix.
    /// </summary>
    [Fact]
    public void Warning_NamesTheKeyTheValueAndWhatIsUsed()
    {
        var logger = new RecordingLogger();

        Configuration("30d").ReadInterval(Key, _defaultInterval, _minInterval, _maxInterval, logger);

        logger.Warnings.Should().ContainSingle()
            .Which.Should().Contain(Key).And.Contain("30d").And.Contain("30.00:00:00");
    }

    /// <summary>
    /// <c>"24:00:00"</c> may mean 24 days or one day. The value is kept as <see cref="TimeSpan"/> reads it, and the
    /// warning shows that reading in the unambiguous form, so whoever meant hours sees it in the log.
    /// </summary>
    [Fact]
    public void Warning_ForAnAmbiguousInterval_ShowsHowItWasRead()
    {
        var logger = new RecordingLogger();

        var result = Configuration("24:00:00").ReadInterval(Key, _defaultInterval, _minInterval, _maxInterval, logger);

        result.Should().Be(TimeSpan.FromDays(24));
        logger.Warnings.Should().ContainSingle()
            .Which.Should().Contain(Key).And.Contain("\"24:00:00\"").And.Contain("24.00:00:00");
    }

    /// <summary>
    /// The list recipe of <c>configuration.md</c> §1: every element goes through the reader under its full path, so
    /// the unreadable one gets a warning that names it. The binder would drop it without a word
    /// (<c>Get&lt;int[]&gt;()</c> gives <c>[5]</c>).
    /// </summary>
    [Fact]
    public void ReadInt_OverTheElementsOfAList_NamesTheUnreadableOne()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["section:list:0"] = "5", ["section:list:1"] = "x" })
            .Build();
        var logger = new RecordingLogger();

        var values = configuration.GetSection("section:list").GetChildren()
            .Select(element => configuration.ReadInt(element.Path, 1, 1, 10, logger))
            .ToList();

        values.Should().Equal(5, 1);
        logger.Warnings.Should().ContainSingle().Which.Should().Contain("section:list:1").And.Contain("\"x\"");
    }

    /// <summary>
    /// The bounds and the default are code. A default outside the bounds is a programming error and is reported at
    /// once, whatever the configuration holds.
    /// </summary>
    [Fact]
    public void Read_WithTheDefaultOutsideTheBounds_Throws()
    {
        var read = () => Configuration(null).ReadInt(Key, 0, 1, 10, new RecordingLogger());

        read.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Read_WithTheMinimumAboveTheMaximum_Throws()
    {
        var read = () => Configuration(null).ReadInterval(Key, TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(1), new RecordingLogger());

        read.Should().Throw<ArgumentException>();
    }

    private static IConfiguration Configuration(string? value)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [Key] = value })
            .Build();
    }
}
