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
/// Covers how <see cref="ColumnMapper"/> maps the date columns of a restored row: a date that is not kept
/// relative to the portal is mapped to the moment of the restore, and an empty date stays empty.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class ColumnMapperTests
{
    private const string Table = "files_file";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetDateMapping_EmptyDate_StaysEmpty(bool relative)
    {
        // Arrange
        var columnMapper = new ColumnMapper();

        // Act
        columnMapper.SetDateMapping(Table, new KeyValuePair<string, bool>("modified_on", relative), DBNull.Value);
        columnMapper.SetDateMapping(Table, new KeyValuePair<string, bool>("create_on", relative), null);
        columnMapper.Commit();

        // Assert — no mapping: the restored value is the empty one of the archive, not a date
        columnMapper.GetMapping(Table, "modified_on", DBNull.Value).Should().BeNull();
        columnMapper.GetMapping(Table, "create_on").Should().BeNull();
    }

    [Fact]
    public void SetDateMapping_DateNotKeptRelative_IsMappedToTheMomentOfTheRestore()
    {
        // Arrange
        var columnMapper = new ColumnMapper();
        var archived = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var before = DateTime.UtcNow;

        // Act
        columnMapper.SetDateMapping(Table, new KeyValuePair<string, bool>("modified_on", false), archived);
        columnMapper.Commit();

        // Assert
        var mapped = columnMapper.GetMapping(Table, "modified_on", archived);
        mapped.Should().BeOfType<DateTime>();
        ((DateTime)mapped!).Should().BeCloseTo(before, TimeSpan.FromMinutes(1));
    }
}
