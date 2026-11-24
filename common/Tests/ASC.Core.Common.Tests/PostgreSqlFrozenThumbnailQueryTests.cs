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
/// The query Files.Worker runs to find files whose thumbnail build was left unfinished, on PostgreSQL: it once took the
/// age of a file with EF.Functions.DateDiffMinute, a MySQL function Npgsql cannot translate, so every pass of the
/// worker failed there. The query is run against a server that is not there: a query that translates gets as far as
/// opening the connection, one that does not fails before it.
/// </summary>
public class PostgreSqlFrozenThumbnailQueryTests
{
    [Fact]
    public async Task FrozenThumbnailFiles_TranslatesOnPostgreSql()
    {
        // No retrying strategy here, unlike the services: the refused connection must fail at once.
        var options = new DbContextOptionsBuilder<ASC.Files.Core.EF.FilesDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=docspace;Timeout=2")
            .ReplaceService<IModelCustomizer, ASC.Core.Common.EF.PostgreSqlModelCustomizer>()
            .Options;

        await using var context = new ASC.Files.Core.EF.FilesDbContext(options);

        var act = async () => await context.FrozenThumbnailFilesAsync(DateTime.UtcNow.AddMinutes(-11)).ToListAsync(TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<Exception>();

        // EF wraps the refused connection into "likely due to a transient failure"; a translation error has no Npgsql
        // exception inside.
        var chain = new List<Exception>();

        for (var exception = thrown.Which; exception is not null; exception = exception.InnerException)
        {
            chain.Add(exception);
        }

        chain.Should().Contain(exception => exception is Npgsql.NpgsqlException, "the query translates and only the connection to a server that is not there fails: {0}", string.Join(" / ", chain.Select(exception => exception.Message)));
    }
}
