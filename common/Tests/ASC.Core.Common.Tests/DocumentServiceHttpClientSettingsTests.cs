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
/// What <see cref="DocumentServiceHttpClientSettings"/> makes of each value of <c>files:docservice:timeout</c> and
/// <c>files:docservice:try</c>: a missing key gives the default, an unreadable value the default with a warning, a
/// value out of bounds the nearest bound with a warning (<c>.claude/rules/configuration.md</c> §8).
/// </summary>
public sealed class DocumentServiceHttpClientSettingsTests
{
    [Theory]
    [InlineData(null, 100_000, false)]
    [InlineData("30000", 30_000, false)]
    [InlineData("x", 100_000, true)]
    [InlineData("500", 1_000, true)]
    [InlineData("0", 1_000, true)]
    [InlineData("-1", 1_000, true)]
    [InlineData("300000", 100_000, true)]
    public void Timeout_IsReadInMillisecondsWithinItsBounds(string? value, int expectedMilliseconds, bool warned)
    {
        var logger = new RecordingLogger<DocumentServiceHttpClientSettings>();

        var settings = new DocumentServiceHttpClientSettings(Configuration(DocumentServiceHttpClientSettings.TimeoutKey, value), logger);

        settings.AttemptTimeout.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    [Theory]
    [InlineData(null, 6, false)]
    [InlineData("3", 3, false)]
    [InlineData("x", 6, true)]
    [InlineData("0", 1, true)]
    [InlineData("-1", 1, true)]
    [InlineData("1000", 20, true)]
    public void RetryCount_IsReadWithinItsBounds(string? value, int expected, bool warned)
    {
        var logger = new RecordingLogger<DocumentServiceHttpClientSettings>();

        var settings = new DocumentServiceHttpClientSettings(Configuration(DocumentServiceHttpClientSettings.TryKey, value), logger);

        settings.RetryCount.Should().Be(expected);
        settings.RetryOptions.MaxRetryAttempts.Should().Be(expected);
        logger.Warnings.Should().HaveCount(warned ? 1 : 0);
    }

    private static IConfiguration Configuration(string key, string? value)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();
    }
}
