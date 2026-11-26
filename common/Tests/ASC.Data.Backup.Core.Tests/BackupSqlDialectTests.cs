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
/// Pins the SQL of <see cref="BackupSqlDialect"/>: the MySQL dialect writes exactly the statements the portal
/// backup wrote before it had a PostgreSQL one, and the PostgreSQL dialect writes their equivalents and turns the
/// text values of an archive into the types of the target columns.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "PostgreSql")]
public class BackupSqlDialectTests
{
    private static readonly string[] _columns = ["id", "tenant_id", "title"];

    [Theory]
    [InlineData("Npgsql", "PostgreSql")]
    [InlineData("npgsql", "MySql")]
    [InlineData("MySql.Data.MySqlClient", "MySql")]
    [InlineData(null, "MySql")]
    [InlineData("", "MySql")]
    public void ForProvider_ProviderName_PicksTheDialectTheEfContextsPick(string? providerName, string expected)
    {
        var dialect = BackupSqlDialect.ForProvider(providerName!);

        dialect.Should().BeSameAs(expected == "PostgreSql" ? BackupSqlDialect.PostgreSql : BackupSqlDialect.MySql);
    }

    [Theory]
    [InlineData(InsertMethod.Insert, "insert into files_tag(`id`,`tenant_id`,`title`) values(@id,@tenant_id,@title);")]
    [InlineData(InsertMethod.Replace, "replace into files_tag(`id`,`tenant_id`,`title`) values(@id,@tenant_id,@title);")]
    [InlineData(InsertMethod.Ignore, "insert ignore into files_tag(`id`,`tenant_id`,`title`) values(@id,@tenant_id,@title);")]
    public void MySqlInsert_EachMethod_IsTheStatementOfBefore(InsertMethod method, string expected)
    {
        var table = new TableInfo("files_tag", "tenant_id", "id") { InsertMethod = method };

        BackupSqlDialect.MySql.Insert(table, _columns).Should().Be(expected);
    }

    [Fact]
    public void MySqlPageDeleteAndText_AreTheStatementsOfBefore()
    {
        var table = new TableInfo("files_tag", "tenant_id", "id");

        BackupSqlDialect.MySql.Page(null!, table, 10, 5).Should().Be("limit 10,5");
        BackupSqlDialect.MySql.Delete(table, "where t.tenant_id = 1").Should().Be("delete t.* from files_tag as t where t.tenant_id = 1;");
        BackupSqlDialect.MySql.AsText("t.id").Should().Be("t.id");
        BackupSqlDialect.MySql.LastInsertIdQuery.Should().Be("select Last_Insert_Id();");
    }

    [Fact]
    public void MySqlConvertValue_AnyValue_IsLeftAsItIs()
    {
        var table = PostgreSqlTable(InsertMethod.Insert);

        BackupSqlDialect.MySql.ConvertValue(table, "id", "5").Should().Be("5");
    }

    [Theory]
    [InlineData(InsertMethod.Insert, "insert into \"files_tag\"(\"id\",\"tenant_id\",\"title\") values(@id,@tenant_id,@title);")]
    [InlineData(InsertMethod.Ignore, "insert into \"files_tag\"(\"id\",\"tenant_id\",\"title\") values(@id,@tenant_id,@title) on conflict do nothing;")]
    [InlineData(InsertMethod.Replace, "insert into \"files_tag\"(\"id\",\"tenant_id\",\"title\") values(@id,@tenant_id,@title) on conflict (\"tenant_id\",\"id\") do update set \"title\" = excluded.\"title\";")]
    public void PostgreSqlInsert_EachMethod_IsItsPostgreSqlEquivalent(InsertMethod method, string expected)
    {
        BackupSqlDialect.PostgreSql.Insert(PostgreSqlTable(method), _columns).Should().Be(expected);
    }

    [Fact]
    public void PostgreSqlInsert_ReplaceOfKeyColumnsOnly_LeavesTheRowAsItIs()
    {
        var table = PostgreSqlTable(InsertMethod.Replace);

        BackupSqlDialect.PostgreSql.Insert(table, ["tenant_id", "id"])
            .Should().Be("insert into \"files_tag\"(\"tenant_id\",\"id\") values(@tenant_id,@id) on conflict (\"tenant_id\",\"id\") do nothing;");
    }

    [Fact]
    public void PostgreSqlInsert_ReplaceOnATableWithoutAPrimaryKey_IsAPlainInsert()
    {
        var table = new TableInfo("files_tag") { InsertMethod = InsertMethod.Replace, PrimaryKey = [] };

        BackupSqlDialect.PostgreSql.Insert(table, ["id", "title"])
            .Should().Be("insert into \"files_tag\"(\"id\",\"title\") values(@id,@title);");
    }

    [Fact]
    public void PostgreSqlPage_TableWithoutAPrimaryKey_IsOrderedByAllItsColumns()
    {
        var table = new TableInfo("files_tag") { PrimaryKey = [], Columns = ["tenant_id", "title"] };

        BackupSqlDialect.PostgreSql.Page(null!, table, 10, 5).Should().Be("order by t.\"tenant_id\",t.\"title\" limit 5 offset 10");
    }

    [Fact]
    public void InsertRows_EachDatabase_WritesOneStatementWithAParameterPerRowAndColumn()
    {
        var table = new TableInfo("webhooks_logs")
        {
            ColumnTypes = new Dictionary<string, string> { ["id"] = "int4", ["payload"] = "jsonb" }
        };

        BackupSqlDialect.PostgreSql.InsertRows(table, ["id", "payload"], 2)
            .Should().Be("insert into \"webhooks_logs\"(\"id\",\"payload\") values (@r0_0,@r0_1::jsonb),(@r1_0,@r1_1::jsonb);");
        BackupSqlDialect.MySql.InsertRows(table, ["id", "payload"], 2)
            .Should().Be("insert into `webhooks_logs`(`id`,`payload`) values (@r0_0,@r0_1),(@r1_0,@r1_1);");
    }

    [Fact]
    public void PostgreSqlCursor_ReadsTheTableInPagesOfTheLimit()
    {
        var table = new TableInfo("files_tag") { Columns = ["id"] };

        BackupSqlDialect.PostgreSql.DeclareCursor(table).Should().Be("declare dump_rows no scroll cursor for select t.\"id\" from \"files_tag\" as t;");
        BackupSqlDialect.PostgreSql.FetchCursor(500).Should().Be("fetch forward 500 from dump_rows;");
        BackupSqlDialect.PostgreSql.CloseCursor().Should().Be("close dump_rows;");
    }

    [Fact]
    public void PostgreSqlInsert_JsonColumns_CastTheirParameters()
    {
        var table = new TableInfo("webhooks_logs", "tenant_id", "id")
        {
            ColumnTypes = new Dictionary<string, string> { ["id"] = "int4", ["payload"] = "jsonb", ["headers"] = "json" },
            PrimaryKey = ["id"]
        };

        BackupSqlDialect.PostgreSql.Insert(table, ["id", "payload", "headers"])
            .Should().Be("insert into \"webhooks_logs\"(\"id\",\"payload\",\"headers\") values(@id,@payload::jsonb,@headers::json);");
    }

    [Fact]
    public void PostgreSqlPageDeleteAndText_AreOrderedPortable()
    {
        var table = PostgreSqlTable(InsertMethod.Insert);

        BackupSqlDialect.PostgreSql.Page(null!, table, 10, 5).Should().Be("order by t.\"tenant_id\",t.\"id\" limit 5 offset 10");
        BackupSqlDialect.PostgreSql.Delete(table, "inner join files_folder as t1 on t1.id = t.folder_id where t1.tenant_id = 1")
            .Should().Be("delete from \"files_tag\" where ctid in (select t.ctid from \"files_tag\" as t inner join files_folder as t1 on t1.id = t.folder_id where t1.tenant_id = 1);");
        BackupSqlDialect.PostgreSql.AsText("t.id").Should().Be("t.id::text");
        BackupSqlDialect.PostgreSql.LastInsertIdQuery.Should().Be("select lastval();");
    }

    public static TheoryData<string, object, object> Conversions => new()
    {
        { "int4", "5", 5 },
        { "int4", true, 1 },
        { "int2", "3", (short)3 },
        { "int8", "9000000000", 9000000000L },
        { "numeric", "1.25", 1.25m },
        { "float8", "0.5", 0.5d },
        { "float8", "1E-30", 1E-30d },
        { "float8", "1.7976931348623157E+308", double.MaxValue },
        { "float8", "INF", double.PositiveInfinity },
        { "float8", "NaN", double.NaN },
        { "float4", "-INF", float.NegativeInfinity },
        { "bool", "1", true },
        { "bool", "0", false },
        { "bool", "True", true },
        { "bool", false, false },
        { "uuid", "3f2504e0-4f89-11d3-9a0c-0305e82c3301", Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301") },
        { "citext", "Text", "Text" },
        { "citext", 7, "7" },
        { "jsonb", "{\"a\":1}", "{\"a\":1}" }
    };

    [Theory]
    [MemberData(nameof(Conversions))]
    public void PostgreSqlConvertValue_ArchiveValue_GetsTheTypeOfTheColumn(string type, object value, object expected)
    {
        var table = new TableInfo("t") { ColumnTypes = new Dictionary<string, string> { ["c"] = type } };

        BackupSqlDialect.PostgreSql.ConvertValue(table, "c", value).Should().Be(expected);
    }

    [Theory]
    [InlineData("timestamptz", DateTimeKind.Utc)]
    [InlineData("timestamp", DateTimeKind.Unspecified)]
    public void PostgreSqlConvertValue_ArchiveDate_KeepsItsClockAndGetsTheKindOfTheColumn(string type, DateTimeKind kind)
    {
        var table = new TableInfo("t") { ColumnTypes = new Dictionary<string, string> { ["c"] = type } };

        var fromText = (DateTime)BackupSqlDialect.PostgreSql.ConvertValue(table, "c", "2026-03-04T05:06:07");
        var fromDate = (DateTime)BackupSqlDialect.PostgreSql.ConvertValue(table, "c", new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Unspecified));

        foreach (var date in new[] { fromText, fromDate })
        {
            date.Should().Be(new DateTime(2026, 3, 4, 5, 6, 7));
            date.Kind.Should().Be(kind);
        }
    }

    [Fact]
    public void PostgreSqlConvertValue_EmptyOrUnknown_IsLeftAsItIs()
    {
        var table = new TableInfo("t") { ColumnTypes = new Dictionary<string, string> { ["c"] = "int4" } };

        BackupSqlDialect.PostgreSql.ConvertValue(table, "c", DBNull.Value).Should().Be(DBNull.Value);
        BackupSqlDialect.PostgreSql.ConvertValue(table, "c", null!).Should().BeNull();
        BackupSqlDialect.PostgreSql.ConvertValue(table, "other", "x").Should().Be("x");
        BackupSqlDialect.PostgreSql.ConvertValue(new TableInfo("t"), "c", "x").Should().Be("x");
    }

    [Fact]
    public void PostgreSqlConvertValue_ValueThatIsNotOfTheColumnType_NamesTheColumn()
    {
        var table = new TableInfo("files_tag") { ColumnTypes = new Dictionary<string, string> { ["id"] = "int4" } };

        var act = () => BackupSqlDialect.PostgreSql.ConvertValue(table, "id", "abc");

        act.Should().Throw<InvalidOperationException>().WithMessage("*files_tag.id*int4*");
    }

    private static TableInfo PostgreSqlTable(InsertMethod method)
    {
        return new TableInfo("files_tag", "tenant_id", "id")
        {
            InsertMethod = method,
            ColumnTypes = new Dictionary<string, string> { ["id"] = "int4", ["tenant_id"] = "int4", ["title"] = "citext" },
            PrimaryKey = ["tenant_id", "id"]
        };
    }
}
