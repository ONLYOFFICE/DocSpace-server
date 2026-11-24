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


namespace ASC.Data.Backup.Core.Tests;

/// <summary>
/// Covers the files of the webplugins storage module on restore: a personal plugin lies under the folder of its
/// owner, whose user id the restore can change.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class WebPluginsModuleSpecificsTests
{
    private const string OldUserId = "6c5b1b3e-0c7f-4d1a-9a55-2f1f0f6a1b01";
    private const string NewUserId = "0b7e8c2d-5a4f-4f3e-8c1b-9d2e3f4a5b6c";

    private static readonly WebPluginsModuleSpecifics _module =
        new(new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build()))));

    [Theory]
    [InlineData($".personal/{OldUserId}/hello/plugin.js")]
    [InlineData($".personal\\{OldUserId}\\hello\\plugin.js")]
    public void TryAdjustFilePath_PersonalPluginOfARemappedUser_MovesToTheNewId(string path)
    {
        // Arrange
        var columnMapper = new ColumnMapper();
        columnMapper.SetMapping("core_user", "id", OldUserId, NewUserId);
        columnMapper.Commit();

        // Act
        var adjusted = _module.TryAdjustFilePath(false, columnMapper, ref path);

        // Assert
        adjusted.Should().BeTrue();
        path.Should().Be($".personal/{NewUserId}/hello/plugin.js");
    }

    [Fact]
    public void TryAdjustFilePath_PortalPlugin_KeepsItsPath()
    {
        // Arrange
        var path = "hello/plugin.js";

        // Act
        var adjusted = _module.TryAdjustFilePath(false, new ColumnMapper(), ref path);

        // Assert
        adjusted.Should().BeTrue();
        path.Should().Be("hello/plugin.js");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void TryAdjustFilePath_PersonalPluginOfAUserNotRestored_IsKeptOnlyInADump(bool dump, bool expected)
    {
        // Arrange
        var path = $".personal/{OldUserId}/hello/plugin.js";

        // Act
        var adjusted = _module.TryAdjustFilePath(dump, new ColumnMapper(), ref path);

        // Assert
        adjusted.Should().Be(expected);
        path.Should().Be($".personal/{OldUserId}/hello/plugin.js");
    }

    [Fact]
    public void GetByStorageModule_WebPlugins_IsTheModuleThatAdjustsThePaths()
    {
        // Arrange
        var helpers = new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build())));
        var moduleProvider = new ModuleProvider(NullLogger<ModuleProvider>.Instance, helpers, null!);

        // Act
        var module = moduleProvider.GetByStorageModule("webplugins");

        // Assert
        module.Should().BeOfType<WebPluginsModuleSpecifics>();
    }
}
