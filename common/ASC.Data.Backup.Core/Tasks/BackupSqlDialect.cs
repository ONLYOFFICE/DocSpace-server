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

namespace ASC.Data.Backup.Tasks;

/// <summary>
/// The SQL of a portal backup and restore that differs between the databases the services run on. The MySQL
/// dialect writes the statements the backup has always written. The PostgreSQL one writes their equivalents and
/// converts the values of an archive, which <c>DataRowInfoReader</c> reads as text (only flags and binary values come
/// typed), to the types of the target columns: PostgreSQL, unlike MySQL, does not turn a text parameter into a
/// number, a flag or a date by itself.
/// </summary>
public abstract class BackupSqlDialect
{
    public static readonly BackupSqlDialect MySql = new MySqlBackupSqlDialect();
    public static readonly BackupSqlDialect PostgreSql = new PostgreSqlBackupSqlDialect();

    /// <summary>
    /// The dialect of a connection string's provider name, read the way the EF contexts read it
    /// (<c>InstallerOptionsAction</c>): exactly "Npgsql" is PostgreSQL, any other name MySQL.
    /// </summary>
    public static BackupSqlDialect ForProvider(string providerName)
    {
        return string.Equals(providerName, "Npgsql", StringComparison.Ordinal) ? PostgreSql : MySql;
    }

    /// <summary>The name of the database the dialect is for, as a dump records its source.</summary>
    public abstract string Name { get; }

    public static BackupSqlDialect For(DbConnection connection)
    {
        return connection is NpgsqlConnection ? PostgreSql : MySql;
    }

    public abstract string LastInsertIdQuery { get; }

    /// <summary>The clause that picks one page of a table.</summary>
    public abstract string Page(DbConnection connection, TableInfo table, int offset, int limit);

    /// <summary>A delete of the rows of a table that its select condition picks.</summary>
    public abstract string Delete(TableInfo table, string condition);

    /// <summary>An insert of one row by the table's <see cref="TableInfo.InsertMethod"/>, one parameter per column.</summary>
    public abstract string Insert(TableInfo table, IReadOnlyCollection<string> columns);

    /// <summary>A plain insert of <paramref name="rows"/> rows, with the parameters named by <see cref="RowParameter"/>.</summary>
    public string InsertRows(TableInfo table, IReadOnlyList<string> columns, int rows)
    {
        var values = Enumerable.Range(0, rows)
            .Select(row => "(" + string.Join(",", columns.Select((column, index) => "@" + RowParameter(row, index) + Cast(table, column))) + ")");

        return $"insert into {Quote(table.Name)}({string.Join(",", columns.Select(Quote))}) values {string.Join(",", values)};";
    }

    public static string RowParameter(int row, int column)
    {
        return $"r{row}_{column}";
    }

    /// <summary>The cast a parameter of the column needs in a statement, empty when it needs none.</summary>
    protected virtual string Cast(TableInfo table, string column)
    {
        return "";
    }

    /// <summary>
    /// Opens a cursor over every row of a table in the transaction of the connection, so that a dump reads the
    /// table once, page after page, instead of skipping the rows of the earlier pages again for each page.
    /// </summary>
    public virtual string DeclareCursor(TableInfo table)
    {
        throw new NotSupportedException("a dump of rows is written on PostgreSQL only");
    }

    public virtual string FetchCursor(int limit)
    {
        throw new NotSupportedException("a dump of rows is written on PostgreSQL only");
    }

    public virtual string CloseCursor()
    {
        throw new NotSupportedException("a dump of rows is written on PostgreSQL only");
    }

    /// <summary>A whole-number expression written so that it compares with a text column.</summary>
    public abstract string AsText(string expression);

    /// <summary>Reads the columns of the target table into <see cref="TableInfo.Columns"/> and the fields beside it.</summary>
    public abstract void ReadColumns(DbConnection connection, TableInfo table);

    /// <summary>The base tables of the database the connection is in, one name per row.</summary>
    public abstract string ListTablesQuery { get; }

    /// <summary>The ids of the EF migrations the database has applied, one per row.</summary>
    public string MigrationHistoryQuery => $"select {Quote("MigrationId")} from {Quote(NeutralDump.MigrationHistoryTable)};";

    /// <summary>A select of every row of a table, of the columns <see cref="ReadColumns"/> read, without the page clause.</summary>
    public string SelectAll(TableInfo table)
    {
        return $"select {string.Join(",", table.Columns.Select(c => "t." + Quote(c)))} from {Quote(table.Name)} as t";
    }

    /// <summary>
    /// Lets the rows of a dump of rows go in whatever order the tables come, with the foreign keys off, in the
    /// transaction of the load, and returns what <see cref="EndDumpLoadAsync"/> needs to put them back. A dump of
    /// rows is written and restored on PostgreSQL only.
    /// </summary>
    public virtual Task<object> BeginDumpLoadAsync(DbConnection connection, DbTransaction transaction)
    {
        throw new NotSupportedException("a dump of rows is restored on PostgreSQL only");
    }

    /// <summary>Empties the tables a dump of rows is loaded into.</summary>
    public virtual Task TruncateAsync(DbConnection connection, DbTransaction transaction, IReadOnlyCollection<string> tables)
    {
        throw new NotSupportedException("a dump of rows is restored on PostgreSQL only");
    }

    /// <summary>
    /// Puts the foreign keys back. Returns the ones that the loaded rows break, which stay in place without being
    /// checked against those rows, as MySQL leaves its keys when its checks are switched back on.
    /// </summary>
    public virtual Task<IReadOnlyList<string>> EndDumpLoadAsync(DbConnection connection, DbTransaction transaction, object state)
    {
        throw new NotSupportedException("a dump of rows is restored on PostgreSQL only");
    }

    public abstract string Quote(string identifier);

    /// <summary>The value of a parameter for a column of the target table.</summary>
    public virtual object ConvertValue(TableInfo table, string column, object value)
    {
        return value;
    }

    /// <summary>Runs after rows of a table were written, in their transaction.</summary>
    public virtual Task AfterTableRestoredAsync(DbConnection connection, TableInfo table)
    {
        return Task.CompletedTask;
    }
}

internal sealed class MySqlBackupSqlDialect : BackupSqlDialect
{
    public override string Name => "MySql";

    public override string LastInsertIdQuery => "select Last_Insert_Id();";

    public override string Page(DbConnection connection, TableInfo table, int offset, int limit)
    {
        return $"limit {offset},{limit}";
    }

    public override string Delete(TableInfo table, string condition)
    {
        return $"delete t.* from {table.Name} as t {condition};";
    }

    public override string Insert(TableInfo table, IReadOnlyCollection<string> columns)
    {
        var insert = table.InsertMethod != InsertMethod.Ignore ? table.InsertMethod.ToString().ToLower() : "insert ignore";

        return $"{insert} into {table.Name}({string.Join(",", columns.Select(c => $"`{c}`"))}) values({string.Join(",", columns.Select(c => "@" + c))});";
    }

    public override string AsText(string expression)
    {
        // MySQL compares a number with a text column by itself
        return expression;
    }

    public override string ListTablesQuery =>
        "select table_name from information_schema.tables where table_schema = database() and table_type = 'BASE TABLE';";

    public override string Quote(string identifier)
    {
        return "`" + identifier.Replace("`", "``") + "`";
    }

    public override void ReadColumns(DbConnection connection, TableInfo table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "show columns from " + table.Name + ";";

        var list = new List<string>();
        using (var result = command.ExecuteReader())
        {
            while (result.Read())
            {
                var extra = result.GetString(5);
                if (!extra.Contains("GENERATED", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(result.GetString(0));
                }
            }
        }

        table.Columns = list.ToArray();
    }
}

internal sealed class PostgreSqlBackupSqlDialect : BackupSqlDialect
{
    private const string DumpCursor = "dump_rows";

    public override string Name => "PostgreSql";

    public override string LastInsertIdQuery => "select lastval();";

    public override string DeclareCursor(TableInfo table)
    {
        return $"declare {DumpCursor} no scroll cursor for {SelectAll(table)};";
    }

    public override string FetchCursor(int limit)
    {
        return $"fetch forward {limit} from {DumpCursor};";
    }

    public override string CloseCursor()
    {
        return $"close {DumpCursor};";
    }

    public override string Page(DbConnection connection, TableInfo table, int offset, int limit)
    {
        // without an order PostgreSQL may return the pages of one table in different orders and skip or repeat rows
        // between them; the primary key orders them as the clustered index does on MySQL, all columns a table
        // that has none
        var order = ReadPrimaryKey(connection, table);
        if (order.Length == 0)
        {
            order = (table.Columns ?? [.. ReadNames(connection, table, ColumnsQuery)]);
        }
        var orderBy = order.Length > 0 ? "order by " + string.Join(",", order.Select(c => "t." + Quote(c))) + " " : "";

        return $"{orderBy}limit {limit} offset {offset}";
    }

    public override string Delete(TableInfo table, string condition)
    {
        // the condition may join a parent (inner join ... where ...), which a PostgreSQL delete cannot
        return $"delete from {Quote(table.Name)} where ctid in (select t.ctid from {Quote(table.Name)} as t {condition});";
    }

    public override string Insert(TableInfo table, IReadOnlyCollection<string> columns)
    {
        var values = string.Join(",", columns.Select(c => "@" + c + Cast(table, c)));
        var insert = $"insert into {Quote(table.Name)}({string.Join(",", columns.Select(Quote))}) values({values})";

        if (table.InsertMethod == InsertMethod.Ignore)
        {
            return insert + " on conflict do nothing;";
        }

        if (table.InsertMethod != InsertMethod.Replace)
        {
            return insert + ";";
        }

        // replace collides on the primary key here; MySQL's also collides on a unique index
        var key = table.PrimaryKey ?? [];
        if (key.Length == 0)
        {
            return insert + ";";
        }

        var target = string.Join(",", key.Select(Quote));
        var update = columns.Where(c => !key.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

        return update.Count == 0
            ? $"{insert} on conflict ({target}) do nothing;"
            : $"{insert} on conflict ({target}) do update set {string.Join(",", update.Select(c => $"{Quote(c)} = excluded.{Quote(c)}"))};";
    }

    public override string AsText(string expression)
    {
        return $"{expression}::text";
    }

    public override string ListTablesQuery =>
        "select table_name from information_schema.tables where table_schema = current_schema() and table_type = 'BASE TABLE';";

    public override async Task<object> BeginDumpLoadAsync(DbConnection connection, DbTransaction transaction)
    {
        // switching the checks off for a session needs a superuser, so the foreign keys of the schema are dropped
        // for the load and added back at its end, in the transaction of the load: a load that fails or stops
        // rolls the drop back with the rest
        var keys = new List<(string Table, string Name, string Definition)>();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                "select c.conrelid::regclass::text, c.conname, pg_get_constraintdef(c.oid) from pg_constraint c " +
                "where c.contype = 'f' and c.connamespace = current_schema()::regnamespace order by c.conrelid::regclass::text, c.conname;";

            await using var result = await command.ExecuteReaderAsync();
            while (await result.ReadAsync())
            {
                keys.Add((result.GetString(0), result.GetString(1), result.GetString(2)));
            }
        }

        foreach (var key in keys)
        {
            await ExecuteAsync(connection, transaction, $"alter table {key.Table} drop constraint {Quote(key.Name)};");
        }

        return keys;
    }

    public override async Task TruncateAsync(DbConnection connection, DbTransaction transaction, IReadOnlyCollection<string> tables)
    {
        if (tables.Count > 0)
        {
            await ExecuteAsync(connection, transaction, $"truncate table {string.Join(",", tables.Select(Quote))};");
        }
    }

    public override async Task<IReadOnlyList<string>> EndDumpLoadAsync(DbConnection connection, DbTransaction transaction, object state)
    {
        var broken = new List<string>();

        foreach (var (table, name, definition) in (List<(string Table, string Name, string Definition)>)state)
        {
            // added without reading the rows, then checked against them; a key the rows break stays unchecked and
            // the load goes on, as on MySQL
            await ExecuteAsync(connection, transaction, $"alter table {table} add constraint {Quote(name)} {definition} not valid;");

            var npgsqlTransaction = (NpgsqlTransaction)transaction;
            await npgsqlTransaction.SaveAsync("validate_key");

            try
            {
                await ExecuteAsync(connection, transaction, $"alter table {table} validate constraint {Quote(name)};");
                await npgsqlTransaction.ReleaseAsync("validate_key");
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                await npgsqlTransaction.RollbackAsync("validate_key");
                broken.Add($"{table}.{name}");
            }
        }

        return broken;
    }

    public override string Quote(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.WithTimeout(600).ExecuteNonQueryAsync();
    }

    public override void ReadColumns(DbConnection connection, TableInfo table)
    {
        var columns = new List<string>();
        var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sequences = new List<string>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "select column_name, udt_name, is_generated, is_identity, column_default from information_schema.columns " +
                "where table_schema = current_schema() and table_name = @table order by ordinal_position;";
            AddParameter(command, "table", table.Name);

            using var result = command.ExecuteReader();
            while (result.Read())
            {
                // a stored generated column refuses a value, as on MySQL
                if (string.Equals(result.GetString(2), "ALWAYS", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = result.GetString(0);
                columns.Add(name);
                types[name] = result.GetString(1);

                var isIdentity = string.Equals(result.GetString(3), "YES", StringComparison.OrdinalIgnoreCase);
                var isSerial = !result.IsDBNull(4) && result.GetString(4).StartsWith("nextval(", StringComparison.OrdinalIgnoreCase);
                if (isIdentity || isSerial)
                {
                    sequences.Add(name);
                }
            }
        }

        table.Columns = columns.ToArray();
        table.ColumnTypes = types;
        table.SequenceColumns = sequences.ToArray();
        ReadPrimaryKey(connection, table);
    }

    public override object ConvertValue(TableInfo table, string column, object value)
    {
        if (value is null or DBNull || table.ColumnTypes == null || !table.ColumnTypes.TryGetValue(column, out var type))
        {
            return value;
        }

        try
        {
            return type switch
            {
                "int2" => Convert.ToInt16(Number(value), CultureInfo.InvariantCulture),
                "int4" => Convert.ToInt32(Number(value), CultureInfo.InvariantCulture),
                "int8" => Convert.ToInt64(Number(value), CultureInfo.InvariantCulture),
                "numeric" => Convert.ToDecimal(Number(value), CultureInfo.InvariantCulture),
                // through the XML form the archive writes (INF, NaN, 1E-30), not through decimal
                "float4" => value is string text4 ? XmlConvert.ToSingle(text4) : Convert.ToSingle(Number(value), CultureInfo.InvariantCulture),
                "float8" => value is string text8 ? XmlConvert.ToDouble(text8) : Convert.ToDouble(Number(value), CultureInfo.InvariantCulture),
                "bool" => Flag(value),
                "uuid" => value is Guid ? value : Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!),
                // the backup writes dates without a zone (DataSetDateTime.Unspecified), and the services store them in UTC
                "timestamp" => DateTime.SpecifyKind(Date(value), DateTimeKind.Unspecified),
                "timestamptz" => DateTime.SpecifyKind(Date(value), DateTimeKind.Utc),
                "date" => Date(value).Date,
                "bytea" => value is byte[] ? value : Convert.FromBase64String(Convert.ToString(value, CultureInfo.InvariantCulture)!),
                _ => value is string ? value : Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidOperationException($"The value of {table.Name}.{column} does not convert to {type}.", e);
        }
    }

    public override async Task AfterTableRestoredAsync(DbConnection connection, TableInfo table)
    {
        // a restore writes the ids it maps itself (IdType.Integer, Guid) and every other column with a sequence as
        // given; PostgreSQL does not move a sequence past a value given explicitly, so the next insert of the
        // services would collide with a restored row
        foreach (var column in table.SequenceColumns ?? [])
        {
            if (table.IdType == IdType.Autoincrement && string.Equals(column, table.IdColumn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText =
                "select setval(s.seq, greatest(m.top, coalesce(q.last_value, 1), 1)) " +
                "from (select pg_get_serial_sequence(quote_ident(@table), @column) as seq) s " +
                $"cross join (select max({Quote(column)}) as top from {Quote(table.Name)}) m " +
                "left join pg_sequences q on quote_ident(q.schemaname) || '.' || quote_ident(q.sequencename) = s.seq " +
                "where s.seq is not null and m.top is not null;";
            AddParameter(command, "table", table.Name);
            AddParameter(command, "column", column);
            await command.WithTimeout(120).ExecuteNonQueryAsync();
        }
    }

    private static string[] ReadPrimaryKey(DbConnection connection, TableInfo table)
    {
        if (table.PrimaryKey != null)
        {
            return table.PrimaryKey;
        }

        var key = ReadNames(connection, table,
            "select a.attname from pg_index i join pg_attribute a on a.attrelid = i.indrelid and a.attnum = any(i.indkey) " +
            "where i.indrelid = to_regclass(quote_ident(@table)) and i.indisprimary order by array_position(i.indkey::int2[], a.attnum);");

        // read once per TableInfo: the modules of a backup or restore job hold their own
        table.PrimaryKey = key.ToArray();

        return table.PrimaryKey;
    }

    private const string ColumnsQuery =
        "select column_name from information_schema.columns where table_schema = current_schema() and table_name = @table order by ordinal_position;";

    private static List<string> ReadNames(DbConnection connection, TableInfo table, string sql)
    {
        var names = new List<string>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "table", table.Name);

        using var result = command.ExecuteReader();
        while (result.Read())
        {
            names.Add(result.GetString(0));
        }

        return names;
    }

    protected override string Cast(TableInfo table, string column)
    {
        // PostgreSQL does not cast a text parameter into json by itself
        return table.ColumnTypes != null && table.ColumnTypes.TryGetValue(column, out var type) && type is "json" or "jsonb"
            ? "::" + type
            : "";
    }

    private static object Number(object value)
    {
        return value switch
        {
            bool b => b ? 1 : 0,
            string s => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => value
        };
    }

    private static bool Flag(object value)
    {
        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var b) => b,
            string s => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture) != 0,
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0
        };
    }

    private static DateTime Date(object value)
    {
        return value switch
        {
            DateTime d => d,
            DateTimeOffset o => o.UtcDateTime,
            _ => DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal)
        };
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@" + name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
