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
/// The largest Content-Security-Policy header the portal serves (<c>csp:default:maxSize</c>, <c>CspConfigSettings</c>):
/// 1024 to 65536 bytes, 15360 when the key is missing or 0, read through the configuration readers.
/// </summary>
public class CspConfigSettingsTests
{
    [Theory]
    [InlineData(null, 15 * 1024, false)]
    [InlineData("0", 15 * 1024, true)]
    [InlineData("20000", 20000, false)]
    [InlineData("-1", 1024, true)]
    [InlineData("100", 1024, true)]
    [InlineData("1000000", 64 * 1024, true)]
    [InlineData("large", 15 * 1024, true)]
    public void MaxSize_ConfiguredValue_IsReadWithinItsBounds(string? configured, int expected, bool warned)
    {
        // Arrange
        var logger = new RecordingLogger<CspConfigSettings>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["csp:default:maxSize"] = configured })
            .Build();

        // Act
        var settings = new CspConfigSettings(configuration, logger);

        // Assert
        settings.MaxSize.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }
}
