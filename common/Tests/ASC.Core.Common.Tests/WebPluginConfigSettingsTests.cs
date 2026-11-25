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
/// The <c>plugins</c> section (<c>WebPluginConfigSettings</c>) goes through the configuration readers
/// (<c>.claude/rules/configuration.md</c>): a missing key gives the default, a value that does not parse gives the
/// default and a warning, and a number out of its bounds gives the nearest bound and a warning.
/// </summary>
public class WebPluginConfigSettingsTests
{
    [Fact]
    public void Read_NothingSet_GivesTheDefaultsWithoutWarnings()
    {
        // Arrange
        var logger = new RecordingLogger<WebPluginConfigSettings>();

        // Act
        var settings = new WebPluginConfigSettings(Configuration([]), logger);

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.Upload.Should().BeFalse();
        settings.Delete.Should().BeFalse();
        settings.Personal.Should().BeTrue();
        settings.MaxCount.Should().Be(100);
        settings.PersonalMaxCount.Should().Be(10);
        settings.MaxSize.Should().Be(5L * 1024 * 1024);
        settings.Extension.Should().Be(".zip");
        settings.AssetExtensions.Should().Equal(".jpg", ".jpeg", ".png", ".svg");
        settings.AssetMaxCount.Should().Be(10);
        logger.Warnings.Should().BeEmpty();
    }

    /// <summary>The key, the configured text, the property it sets, the value read and whether a warning was written.</summary>
    public static TheoryData<string, string, string, object, bool> Values => new()
    {
        { "plugins:personal", "off", nameof(WebPluginConfigSettings.Personal), false, false },
        { "plugins:personal", "0", nameof(WebPluginConfigSettings.Personal), false, false },
        { "plugins:personal", "nope", nameof(WebPluginConfigSettings.Personal), true, true },
        { "plugins:enabled", "1", nameof(WebPluginConfigSettings.Enabled), true, false },
        { "plugins:maxCount", "many", nameof(WebPluginConfigSettings.MaxCount), 100, true },
        { "plugins:maxCount", "0", nameof(WebPluginConfigSettings.MaxCount), 100, true },
        { "plugins:maxCount", "1", nameof(WebPluginConfigSettings.MaxCount), 1, false },
        { "plugins:maxCount", "-5", nameof(WebPluginConfigSettings.MaxCount), 1, true },
        { "plugins:maxCount", "100000", nameof(WebPluginConfigSettings.MaxCount), 1000, true },
        { "plugins:personalMaxCount", "0", nameof(WebPluginConfigSettings.PersonalMaxCount), 10, true },
        { "plugins:personalMaxCount", "100000", nameof(WebPluginConfigSettings.PersonalMaxCount), 100, true },
        { "plugins:personalMaxCount", "-1", nameof(WebPluginConfigSettings.PersonalMaxCount), 1, true },
        { "plugins:personalMaxCount", "x", nameof(WebPluginConfigSettings.PersonalMaxCount), 10, true },
        { "plugins:maxSize", "0", nameof(WebPluginConfigSettings.MaxSize), 5L * 1024 * 1024, true },
        { "plugins:maxSize", "10", nameof(WebPluginConfigSettings.MaxSize), 1024L, true },
        { "plugins:maxSize", "1000000000000", nameof(WebPluginConfigSettings.MaxSize), 100L * 1024 * 1024, true },
        { "plugins:maxSize", "00", nameof(WebPluginConfigSettings.MaxSize), 5L * 1024 * 1024, true },
        { "plugins:maxSize", "-1", nameof(WebPluginConfigSettings.MaxSize), 1024L, true },
        { "plugins:maxSize", "x", nameof(WebPluginConfigSettings.MaxSize), 5L * 1024 * 1024, true },
        { "plugins:assetMaxCount", "0", nameof(WebPluginConfigSettings.AssetMaxCount), 10, true },
        { "plugins:assetMaxCount", "-1", nameof(WebPluginConfigSettings.AssetMaxCount), 1, true },
        { "plugins:assetMaxCount", "1000", nameof(WebPluginConfigSettings.AssetMaxCount), 100, true }
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void Read_ConfiguredValue_IsReadWithinItsBounds(string key, string configured, string property, object expected, bool warned)
    {
        // Arrange
        var logger = new RecordingLogger<WebPluginConfigSettings>();

        // Act
        var settings = new WebPluginConfigSettings(Configuration(new() { [key] = configured }), logger);

        // Assert
        typeof(WebPluginConfigSettings).GetProperty(property)!.GetValue(settings).Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    [Fact]
    public void Read_AssetExtensionsList_KeepsTheNonEmptyElements()
    {
        // Arrange
        var logger = new RecordingLogger<WebPluginConfigSettings>();
        var configuration = Configuration(new()
        {
            ["plugins:assetExtensions:0"] = ".png",
            ["plugins:assetExtensions:1"] = "",
            ["plugins:assetExtensions:2"] = ".svg"
        });

        // Act
        var settings = new WebPluginConfigSettings(configuration, logger);

        // Assert
        settings.AssetExtensions.Should().Equal(".png", ".svg");
        logger.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Read_AssetExtensionsAsOneValue_KeepsTheDefaultListWithAWarning()
    {
        // Arrange
        var logger = new RecordingLogger<WebPluginConfigSettings>();

        // Act
        var settings = new WebPluginConfigSettings(Configuration(new() { ["plugins:assetExtensions"] = ".html,.js" }), logger);

        // Assert
        settings.AssetExtensions.Should().Equal(".jpg", ".jpeg", ".png", ".svg");
        logger.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public void Read_AssetExtensionsEmptyList_KeepsTheDefaultList()
    {
        // Arrange
        var logger = new RecordingLogger<WebPluginConfigSettings>();

        // Act
        var settings = new WebPluginConfigSettings(Configuration(new() { ["plugins:assetExtensions:0"] = "" }), logger);

        // Assert
        settings.AssetExtensions.Should().Equal(".jpg", ".jpeg", ".png", ".svg");
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
