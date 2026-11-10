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

namespace ASC.Web.Api.Tests.Tests._07_Migration;

/// <summary>
/// The limits the migration operation puts on what an uploaded backup may unpack to. The integration-test profile
/// configures <c>migration:maxExtractedSize</c> and <c>migration:maxExtractedEntries</c> for the whole stack, so what a deployment gets without that setting
/// is checked here, on the methods the operation reads them through.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationExtractionLimitsTests
{
    /// <summary>
    /// A portal whose quota sets no limit - an unlimited plan, standalone - still gets a finite ceiling when
    /// the deployment configures none, so a small archive that unpacks to hundreds of gigabytes cannot fill the disk.
    /// </summary>
    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void GetMaxExtractedSize_NotConfiguredAndQuotaUnlimited_IsFinite(long maxTotalSize)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var maxExtractedSize = ASC.Migration.Core.MigrationOperation.GetMaxExtractedSize(configuration, maxTotalSize);

        // Assert
        maxExtractedSize.Should().Be(ASC.Migration.Core.MigrationOperation.DefaultMaxExtractedSize);
        maxExtractedSize.Should().BeLessThan(long.MaxValue);
    }

    [Fact]
    public void GetMaxExtractedSize_QuotaBelowCeiling_IsQuota()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var maxExtractedSize = ASC.Migration.Core.MigrationOperation.GetMaxExtractedSize(configuration, 1024);

        // Assert
        maxExtractedSize.Should().Be(1024);
    }

    [Fact]
    public void GetMaxExtractedSize_Configured_OverridesDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["migration:maxExtractedSize"] = "2048" })
            .Build();

        // Act
        var maxExtractedSize = ASC.Migration.Core.MigrationOperation.GetMaxExtractedSize(configuration, long.MaxValue);

        // Assert
        maxExtractedSize.Should().Be(2048);
    }

    /// <summary>
    /// The byte limit does not see directories or empty files, so the number of entries has a finite ceiling of
    /// its own when the deployment configures none.
    /// </summary>
    [Fact]
    public void GetMaxExtractedEntries_NotConfigured_IsFinite()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var maxExtractedEntries = ASC.Migration.Core.MigrationOperation.GetMaxExtractedEntries(configuration);

        // Assert
        maxExtractedEntries.Should().Be(ASC.Migration.Core.MigrationOperation.DefaultMaxExtractedEntries);
        maxExtractedEntries.Should().BeLessThan(long.MaxValue);
    }
}
