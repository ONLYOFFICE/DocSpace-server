// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// The connection settings of the built-in forms database (<see cref="BuiltinFormsDbOptions"/>): on a server shared by
/// many portals a bad value must never stop the service or leave a portal's account unable to open its own pools. A
/// value it cannot use falls back with a warning. Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class BuiltinFormsDbOptionsTests
{
    private const string PoolKey = "files:builtinFormsDb:maxPoolSize";
    private const string LimitKey = "files:builtinFormsDb:rwConnectionLimit";

    [Fact]
    public void Read_NothingConfigured_TakesTheDefaultsWithoutAWarning()
    {
        var (options, warnings) = Read([]);

        (options.MaxPoolSize, options.RwConnectionLimit).Should().Be((5, 50));
        warnings.Should().Be(0);
    }

    [Theory]
    [InlineData("abc", 5)]
    [InlineData("0", 1)]
    [InlineData("-3", 1)]
    [InlineData("500", 20)]
    public void Read_PoolSizeItCannotUse_FallsBackWithAWarning(string value, int expected)
    {
        var (options, warnings) = Read([new(PoolKey, value)]);

        options.MaxPoolSize.Should().Be(expected);
        warnings.Should().Be(1);
    }

    [Theory]
    [InlineData("abc", 50)]
    [InlineData("0", 10)]
    [InlineData("-1", 10)]
    [InlineData("100000", 1000)]
    public void Read_ConnectionLimitItCannotUse_FallsBackWithAWarning(string value, int expected)
    {
        var (options, warnings) = Read([new(LimitKey, value)]);

        options.RwConnectionLimit.Should().Be(expected);
        warnings.Should().Be(1);
    }

    // A process holds two pools of a portal, one for the submissions and one for the analysis: a limit below them would
    // refuse the portal's own connections, so it is raised to what the configured pools need.
    [Fact]
    public void Read_LimitBelowTwoPools_IsRaisedToThem()
    {
        var (options, warnings) = Read([new(PoolKey, "8"), new(LimitKey, "10")]);

        (options.MaxPoolSize, options.RwConnectionLimit).Should().Be((8, 16));
        warnings.Should().Be(1);
    }

    // The default limit has to fit the pools of Files, Files.Worker and AI as configured, not only those of size 5.
    [Fact]
    public void Read_LargePoolsWithoutALimit_DefaultCoversThreeServices()
    {
        var (options, warnings) = Read([new(PoolKey, "20")]);

        (options.MaxPoolSize, options.RwConnectionLimit).Should().Be((20, 120));
        warnings.Should().Be(0);
    }

    [Fact]
    public void Read_SaasValues_AreTakenAsConfigured()
    {
        var (options, warnings) = Read([new(PoolKey, "2"), new(LimitKey, "12")]);

        (options.MaxPoolSize, options.RwConnectionLimit).Should().Be((2, 12));
        warnings.Should().Be(0);
    }

    private static (BuiltinFormsDbOptions Options, int Warnings) Read(KeyValuePair<string, string?>[] values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var logger = new CountingLogger();

        return (new BuiltinFormsDbOptions(configuration, logger), logger.Warnings);
    }

    private sealed class CountingLogger : Microsoft.Extensions.Logging.ILogger<BuiltinFormsDbOptions>
    {
        public int Warnings { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == Microsoft.Extensions.Logging.LogLevel.Warning)
            {
                Warnings++;
            }
        }
    }
}
