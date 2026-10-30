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
/// Covers how the dump restore splits the data file of webhooks_logs into statements: each one is committed together
/// with the update that takes its rows off the delivery schedule, so a statement cut in the wrong place would load
/// rows without that update, or fail and lose them.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class RestorePortalTaskDumpStatementsTests
{
    private static async Task<List<string>> ReadAsync(string text)
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)), Encoding.UTF8);

        var statements = new List<string>();
        await foreach (var statement in RestorePortalTask.ReadDumpStatementsAsync(reader))
        {
            statements.Add(statement);
        }

        return statements;
    }

    [Fact]
    public async Task ReadDumpStatements_TheBatchesOfTheDumpWriter_AreOneStatementEach()
    {
        // Arrange: two batches as BackupPortalTask.SaveToFile writes them, a row per line, the last one ending with ';'
        // and a body whose own semicolon and escaped line break stay inside the row's line
        const string dump =
            "REPLACE INTO `webhooks_logs` (`id`,`request_payload`) VALUES \n" +
            "(1,'{\"a\":1};\\n'),\n" +
            "(2,'b');\n" +
            "REPLACE INTO `webhooks_logs` (`id`,`request_payload`) VALUES \n" +
            "(3,'c');\n";

        // Act
        var statements = await ReadAsync(dump);

        // Assert
        statements.Should().Equal(
            "REPLACE INTO `webhooks_logs` (`id`,`request_payload`) VALUES (1,'{\"a\":1};\\n'),(2,'b');",
            "REPLACE INTO `webhooks_logs` (`id`,`request_payload`) VALUES (3,'c');");
    }

    [Fact]
    public async Task ReadDumpStatements_EmptyFile_HasNoStatements()
    {
        // Act
        var statements = await ReadAsync("");

        // Assert
        statements.Should().BeEmpty();
    }
}
