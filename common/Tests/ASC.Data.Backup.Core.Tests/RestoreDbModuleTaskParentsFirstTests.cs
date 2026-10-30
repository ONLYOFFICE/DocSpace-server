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
/// Covers the order <see cref="RestoreDbModuleTask"/> inserts the rows of a table that refers to itself in
/// (webhooks_logs.retry_of points at webhooks_logs.id): every parent before its children, the rows that arrive in
/// order streamed through untouched.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class RestoreDbModuleTaskParentsFirstTests
{
    private const string Table = "webhooks_logs";

    private static readonly RelationInfo _selfRelation = new(Table, "id", Table, "retry_of");

    private static DataRowInfo Row(object id, object? retryOf = null)
    {
        var row = new DataRowInfo(Table);
        row.SetValue("id", id);
        row.SetValue("retry_of", retryOf);

        return row;
    }

    private static List<object> IdsInOrder(IEnumerable<DataRowInfo> rows)
    {
        return RestoreDbModuleTask.ParentsFirst(rows, _selfRelation).Select(r => r["id"]).ToList();
    }

    [Fact]
    public void ParentsFirst_RowsWithoutAParent_KeepTheirOrder()
    {
        // Arrange: roots, empty either as null or as the DBNull a data reader gives
        var rows = new[] { Row(3), Row(1, DBNull.Value), Row(2) };

        // Act
        var ids = IdsInOrder(rows);

        // Assert
        ids.Should().Equal(3, 1, 2);
    }

    [Fact]
    public void ParentsFirst_ChildBeforeItsParent_IsHeldBackUntilAfterTheParent()
    {
        // Arrange: a retry that the archive holds ahead of the record it retries
        var rows = new[] { Row(5, 1), Row(1), Row(2), Row(3, 2) };

        // Act
        var ids = IdsInOrder(rows);

        // Assert: a child whose parent has already passed goes on at once; only the early one waits for the end
        ids.Should().Equal(1, 2, 3, 5);
    }

    [Fact]
    public void ParentsFirst_ChainHeldBack_ComesOutParentsFirst()
    {
        // Arrange: grandchild, then child, then the root they descend from
        var rows = new[] { Row(3, 2), Row(2, 1), Row(1) };

        // Act
        var ids = IdsInOrder(rows);

        // Assert
        ids.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ParentsFirst_ParentMissingFromTheInput_KeepsTheRowAsARoot()
    {
        // Arrange: a retry whose original record is not in the archive, with a retry of its own ahead of it
        var rows = new[] { Row(1), Row(8, 7), Row(7, 99) };

        // Act
        var ids = IdsInOrder(rows);

        // Assert: nothing is dropped, and the orphan still precedes its own child
        ids.Should().Equal(1, 7, 8);
    }

    [Fact]
    public void ParentsFirst_ArchiveWithoutTheParentColumn_KeepsTheRowsAsRoots()
    {
        // Arrange: an archive taken before webhooks_logs referred to itself has no retry_of column at all
        var rows = new[] { 2, 1, 3 }.Select(id =>
        {
            var row = new DataRowInfo(Table);
            row.SetValue("id", id);

            return row;
        });

        // Act
        var ids = IdsInOrder(rows);

        // Assert: reading the missing column must not fail the restore of the table
        ids.Should().Equal(2, 1, 3);
    }

    [Fact]
    public void ParentsFirst_RowsReadFromAnArchive_MatchTheParentAndChildKeys()
    {
        // Arrange: the reader gives both columns as strings, so a passed parent is recognised by its child only when
        // the two keys come out of it as the same type; a mismatch would hold back every child of a passed parent
        const string xml = """
            <NewDataSet>
              <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                <xs:element name="NewDataSet">
                  <xs:complexType>
                    <xs:sequence>
                      <xs:element name="id" type="xs:int" />
                      <xs:element name="retry_of" type="xs:int" minOccurs="0" />
                    </xs:sequence>
                  </xs:complexType>
                </xs:element>
              </xs:schema>
              <webhooks_logs>
                <id>1</id>
              </webhooks_logs>
              <webhooks_logs>
                <id>2</id>
                <retry_of>1</retry_of>
              </webhooks_logs>
              <webhooks_logs>
                <id>4</id>
                <retry_of>3</retry_of>
              </webhooks_logs>
              <webhooks_logs>
                <id>3</id>
              </webhooks_logs>
            </NewDataSet>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var ids = IdsInOrder(DataRowInfoReader.ReadFromStream(stream).ToList());

        // Assert: 2 follows its parent straight away, 4 is held back until after 3
        ids.Should().Equal("1", "2", "3", "4");
    }
}
