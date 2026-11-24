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
/// The SQL that DbFunctionsExtension.SubstringIndex becomes on PostgreSQL, in the two shapes FileDao and FolderDao sort
/// by: the part before the first dot cast to a number (the AZ order) and the part after the last one (the Type order).
/// A mapped name the server does not know ("SPLIT_PART", quoted) failed every listing sorted that way, and a cast of a
/// name that is no number fails on PostgreSQL where MySQL gives 0. Only the SQL text is checked: no connection is opened.
/// </summary>
public class PostgreSqlSubstringIndexTests
{
    private static ASC.Core.Common.EF.Context.WebstudioDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:default:name"] = "default",
                ["ConnectionStrings:default:connectionString"] = "Host=localhost;Database=docspace",
                ["ConnectionStrings:default:providerName"] = "Npgsql"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<ASC.Common.Logging.EFLoggerFactory>()
            .BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<ASC.Core.Common.EF.Context.WebstudioDbContext>();
        new ASC.Core.Common.EF.InstallerOptionsAction("current", "default").OptionsAction(services, optionsBuilder);

        return new ASC.Core.Common.EF.Context.WebstudioDbContext(optionsBuilder.Options);
    }

    [Fact]
    public void FirstPart_CastToNumber_KeepsTheLeadingDigitsOrZero()
    {
        using var context = CreateContext();

        var sql = context.WebstudioSettings
            .OrderBy(r => Convert.ToInt32(ASC.Core.Common.EF.DbFunctionsExtension.SubstringIndex(r.Data, '.', 1)))
            .ToQueryString();

        sql.Should().Contain("COALESCE(substring(split_part(w.data, '.', 1), '^[+-]?[0-9]{1,9}'), '0')::int");
        sql.Should().NotContain("\"SPLIT_PART\"");
    }

    [Fact]
    public void LastPart_IsTheFieldAfterTheLastDelimiter()
    {
        using var context = CreateContext();

        var sql = context.WebstudioSettings
            .OrderBy(r => ASC.Core.Common.EF.DbFunctionsExtension.SubstringIndex(r.Data, '.', -1))
            .ToQueryString();

        sql.Should().Contain("split_part(w.data, '.', -1)");
        sql.Should().NotContain("COALESCE");
    }
}
