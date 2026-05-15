// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Tools;

[Scope(typeof(IAiToolFactory))]
public class FormDataToolsFactory(
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FormFillingReportCreator formFillingReportCreator,
    ILogger<FormDataToolsFactory> logger) : IAiToolFactory
{
    private const string QueryName = "query_form_data";
    private const string AggregateName = "aggregated_form_data";
    private const string SelfJoinName = "self_join_form_data";

    private static readonly HashSet<string> _toolNames = new(StringComparer.Ordinal)
    {
        QueryName,
        AggregateName,
        SelfJoinName
    };

    private const string FormDataRules =
        $"""
        <form_data_rules>
        ### Form data analysis rules
        **Priority when rules conflict: correctness > completeness > brevity.**

        ** STRICT REQUIREMENT — NO EXCEPTIONS: A form submission dataset is attached. ANY answer about the data MUST be preceded by a tool call. Answering from memory, prior context, or general knowledge WITHOUT calling a tool is ALWAYS WRONG — even if the answer seems obvious or was returned by a previous tool call. Every new question = new tool call. If you cannot call a tool for any reason, say so explicitly — NEVER produce a data answer without a tool call. The only exception is meta-questions about the schema itself (e.g., "what columns exist?").**

        #### Pre-response checklist
        Before every answer, verify:
        0. Are there 1–3 critical unknowns that would make my answer wrong if guessed? → Ask the user first. Do NOT answer until clarified.
        1. Did I call a tool? (no tool call = no answer, unless the answer is in the column schema itself)
        2. Did I use exact column names from the schema?
        3. Is this a count/stat/group? → `{AggregateName}`, not `{QueryName}`
        4. Is this "show me records"? → `{QueryName}`
        5. Is this "find pairs/overlaps"? → `{SelfJoinName}`
        6. Did the tool return an error? → fix and retry, never guess the answer
        7. Is my response just the answer with minimal context? (no "Let me check...", no "Based on the data...", no postamble)
        8. Does the question ask "who/what has the most/least/highest/lowest"? → identify max_value = result of row[0]; include ONLY rows where result == max_value. Do NOT include any row with a lower result value.
        9. Am I listing 4 or more names/labels? → use a two-column table (label | value), not prose. Copy each label character-for-character from the tool result — do NOT reconstruct from memory.

        #### Core behavior
        - Your visible response text must contain only the final answer — never reasoning steps, planning notes, or chain-of-thought.
        - Start with the answer directly: a short sentence, a list, or a table. No preamble, no postamble, no narration of your process. Include just enough context for the number to be understood (e.g., "45 approved submissions" not just "45").
        - For short factual answers (a number, a name, a yes/no), plain prose is sufficient — do not use bold, headers, or bullet points.
        - **Copy text values from tool results character-for-character.** Never paraphrase, correct spelling, or reconstruct names and labels from memory — use exactly what the tool returned. Transcription errors (wrong letters, extra characters) in names or labels make the answer wrong.
        - Before making tool calls, read the full column schema and plan which calls you need. For multi-part questions, identify all required calls upfront and make them before writing the final answer.
        - Minimise tool calls. Design each call to answer as much as possible in one shot.
        - **Heavy operations notice:** before calling `{SelfJoinName}` or any operation that compares all records pairwise, send a brief one-line status BEFORE the tool call (e.g., "Searching for overlapping records across the dataset, this may take a moment…"). Do NOT ask permission — just notify and call the tool immediately. Do NOT add this notice for simple aggregations or record lookups.

        #### Capability overview questions
        When the user asks about what the AI can answer or show from the attached form data (e.g. "what can you tell me?", "what questions can you answer?", "what insights are available?", "what can you show me about this data?") — this is a **capability overview request**, not a data query.
        **Do NOT call any tool.** The column schema is already present in this context — read it directly.
        Steps:
        1. Read the column schema from the context.
        2. For each column, infer what questions it enables based on its type:
           - **Integer** → totals, averages, sums, min/max per group
           - **Date / DateTime** → trends over time, busiest period (month/year/weekday), frequency
           - **String with enum values** → distribution by value, filtering by status/category
           - **String (names / IDs)** → per-entity breakdown, ranking by volume
        3. Produce a bullet list (4–8 items) of concrete example questions phrased in the user's language, using actual column labels from the schema.
        4. Do not mention tool names or technical implementation details.

        #### Tool selection
        **`{QueryName}` sees at most 500 rows — NEVER use it for numbers, percentages, statistics, or grouped summaries. Use `{AggregateName}` instead.**

        **`{AggregateName}`** — analyses ALL rows server-side, no row limit. Use when the question involves:
          - **Counting:** "how many", "count", "total", "how often", "frequency"
          - **Math:** "average", "sum", "max", "min", "median", "percentage", "ratio"
          - **Grouping:** "breakdown", "per category", "group by", "top N", "ranking", "most", "least", "trend", "distribution"
          - **Comparison:** "compare", "which has more/less", "difference between"
          - If ANY of these concepts appears — use `{AggregateName}`.
          - **SUM/AVG require Integer columns.** Before using SUM or AVG, check the column type in the schema. If the column is String or Date, use COUNT or COUNT_DISTINCT instead. Using SUM/AVG on a non-numeric column causes an error.
          - **SUM of days between two date columns** ("total days spent", "how many days in the interval", "sum of durations"): use `aggregateFunction='SUM'` with `dateDiffValueExpr='col_start col_end DAYS'` — do NOT set `valueColumn`. NEVER invent column names like `col_datediff`, `col_days`, or `DATEDIFF` — there is no pre-computed day-count column unless the schema explicitly lists one.
          - **`dateDiffValueExpr` syntax is strictly `"col_start col_end UNIT"`** — no arithmetic, no `+ 1`, no expressions. `"col_start col_end DAYS + 1"` is invalid and will error. The tool counts both boundary days inclusively (e.g. June 1–June 7 = 7 days).
          - **Date range filter for "intervals in year X"**: filter only by the START date year (`datePartFilters=["col_start YEAR = X"]`). Do NOT add a second filter on the end date year — intervals that start in year X but end in year X+1 are still "in year X" and should be counted.
          - `groupByDatePart` (YEAR/MONTH/WEEK/DAYOFYEAR/QUARTER/DAYOFWEEK) — **only for DATE/DATETIME columns**. NEVER set `groupByDatePart` when `groupByColumn` is a string, name, or ID column — doing so returns NULL for all groups. DAYOFWEEK: 1=Sunday, 2=Monday, 3=Tuesday, 4=Wednesday, 5=Thursday, 6=Friday, 7=Saturday.
          - **YEAR+MONTH grouping on a date column:** when grouping a DATE column by month, ALWAYS include YEAR as well. Use: `groupByColumn='col_date'`, `groupByDatePart='YEAR'`, `secondGroupByColumn='col_date'`, `secondGroupByDatePart='MONTH'`.
          - **Entity + year + month grouping:** when grouping by a non-date column (e.g. entity name) AND by calendar month, ALWAYS include YEAR to avoid merging the same month across different years. Use: `groupByColumn='col_entity'` (NO `groupByDatePart`), `secondGroupByColumn='col_date'`, `secondGroupByDatePart='YEAR'`, `thirdGroupByColumn='col_date'`, `thirdGroupByDatePart='MONTH'`.
          - `dateDiffFilter` — restrict by elapsed days between two date columns.
          - `secondGroupByColumn` (+ optional `secondGroupByDatePart`) — two-dimensional breakdowns.
          - Call multiple times when a question requires several independent breakdowns.
          - **High-cardinality group-by:** first call with `aggregateFunction='COUNT'` and no `groupByColumn` to get the total; then apply a filter or `topN` to restrict results. Ask the user to narrow scope only as a last resort.
          - **HAVING / post-aggregation filtering:** use the `having` parameter to filter groups by their aggregate result — e.g. `having="> 5"` keeps only groups where count > 5. Format: `"OPERATOR value"` (operators: =, !=, <, >, <=, >=). Always include `IS NOT NULL` filters on grouping columns to avoid counting rows with missing values. When the question asks for **a single count** of qualifying groups ("how many pairs/groups had more than N?") rather than a list of them — add `countGroupsOnly=true` to get an exact number from the database.
          - **"Entities NOT in subset" pattern** ("which X had no Y in period Z?", "which entities are absent from a subset?"): `having='= 0'` with `datePartFilters` is WRONG — `datePartFilters` act as WHERE before GROUP BY, so every surviving group already has count > 0 and the result is always empty. Use `excludeDatePartFilters` / `excludeFilters` instead — a single call returns only entities with no matching records:
            `groupByColumn='col_entity'`, `aggregateFunction='COUNT'`, `excludeDatePartFilters=["col_date YEAR = 2025"]`, `filters=["col_entity IS NOT NULL"]`
            The tool generates a NOT IN subquery server-side and returns only entities absent from the specified period — no manual list comparison needed.

        **`{QueryName}`** — retrieves individual records (max 500 rows). Use **only** when the answer requires showing raw row values, not a computed result. When the result is partial, state that it shows a sample.

        **`{SelfJoinName}`** — finds pairs of records: overlapping date ranges, scheduling conflicts, same value shared by multiple records.
          - Each `joinCondition` compares columns from record A (left) vs B (right). Plain column names only — `a_`/`b_` prefixes appear only in output.
          - Overlap operators — choose based on column type and question wording:
            - `<=`/`>=` (inclusive): A.end = B.start counts as overlap. Use for **Date columns** (touching = sharing a calendar day) or when question says "same day", "at least one common day".
            - `<`/`>` (strict): A.end must be strictly after B.start. Use for **DateTime/time columns** or when question says "strictly overlapping", "without touching".
          - Default pattern for date-only columns (inclusive): `joinConditions=["col_start <= col_end", "col_end >= col_start"]`.
          - Strict pattern for datetime/time columns: `joinConditions=["col_start < col_end", "col_end > col_start"]`.
          - **Same-entity overlaps** (e.g. same employee, same room): ALWAYS add `"col_entity = col_entity"` to `joinConditions`. Without it the join finds overlaps between ALL pairs of records regardless of entity — this is almost always wrong and causes timeouts on large tables.
          - **"Same calendar day" constraint** ("on the same day"): when the question requires both records to fall on the SAME calendar day, add TWO date-part conditions to `joinConditions`: `"col_date YEAR = col_date YEAR"` AND `"col_date DAYOFYEAR = col_date DAYOFYEAR"`. Without these, the tool finds overlaps spanning different days (e.g. Mar 28–Apr 1 overlaps Mar 31–Apr 3) and overcounts.
          - **CRITICAL — do NOT mix `joinConditions` YEAR cross-row with `datePartFilters` YEAR constant:** `"col_date YEAR = col_date YEAR"` in `joinConditions` means "both records have the same year" (cross-row comparison, covers ALL years). Adding `datePartFilters=["col_date YEAR = YYYY"]` on top restricts to that year only — wrong when the question covers all time. Use `datePartFilters` only if the question explicitly asks about a specific year.
          - **IS NOT NULL on ALL join columns:** ALWAYS add `IS NOT NULL` filters for EVERY column used in `joinConditions`. NULL rows cannot match but still participate in the join, wasting time and causing timeouts on large tables. Example: `filters=["col_entity IS NOT NULL", "col_start IS NOT NULL", "col_end IS NOT NULL"]`.
          - Example — same entity, same calendar day, strict time overlap, ALL time (no year filter): `joinConditions=["col_entity = col_entity", "col_start YEAR = col_start YEAR", "col_start DAYOFYEAR = col_start DAYOFYEAR", "col_start < col_end", "col_end > col_start"]`, `filters=["col_entity IS NOT NULL", "col_start IS NOT NULL", "col_end IS NOT NULL"]`, `countDistinctColumn="col_entity"`.
          - **Timeout recovery:** if the self-join returns a timeout error, do NOT give up. First retry with IS NOT NULL filters on all join columns. Only add `datePartFilters` year restriction if the same-entity AND same-day cross-row conditions are NOT already in `joinConditions` — those conditions already make the query highly selective and a year filter is not needed.

        **Planning step:** If the question requires 3+ tool calls, outline the plan before calling. Make all calls before the final answer.

        #### Column names and schema
        - **NEVER invent or guess column names.** Use ONLY the exact names listed under "Available columns" in the schema above. Column names are plain strings — no quotes. If none matches the question, tell the user — do NOT fabricate a plausible name. Common mistake: using `date_diff`, `start_date`, `employee_name` when the actual column is `col_datediff`, `col_start`, `col_employee`.
        - **Multiple columns of the same type:** match the column label against the user's wording before calling a tool. If genuinely ambiguous, ask the user.
        - **Enum values:** use only exact values from the schema (e.g., `col_status (String) [approved/pending/rejected]`).
        - **NULL awareness:** `COUNT(*)` includes NULLs; `COUNT(valueColumn)` excludes them; `COUNT_DISTINCT` ignores NULLs. Use `IS NULL`/`IS NOT NULL` filters when needed.

        #### When to ask the user for clarification
        **Before answering, check: is there critical missing information that would make the answer wrong?** If yes — ask 1–3 focused questions FIRST; do NOT answer until clarified. If a reasonable default exists — answer immediately.
        Ask when:
        - **Ambiguous column:** multiple columns equally match the user's wording and context does not resolve it.
        - **Unknown filter value:** the user references a value not in the schema's allowed values.
        - **Contradictory request:** conflicting conditions (e.g., "approved and rejected at the same time").
        - **Scope too broad:** the result would be impractically large and no narrowing filter is implied.
        - **Missing information:** the question requires data not present in any column.
        Keep clarification requests short — one sentence with specific options. Never ask for confirmation before calling a tool.

        #### Edge cases
        - **Empty result (0 rows):** report honestly — "No records match the criteria." Do not treat zero as an error.
        - **NULL-heavy columns:** if most values are NULL, note it (e.g., "42 submissions, but only 12 have a value in the 'department' column"). Use `COUNT(valueColumn)` to count non-null values, not `COUNT(*)`.
        - **Large datasets (>100k rows):** always use `{AggregateName}` for any statistics — never `{QueryName}`. For listing records, always apply filters to narrow the result set before querying. If the user asks to "show all records" without filters, warn that only a sample (up to 500 rows) can be displayed and suggest adding filters to narrow the scope.

        #### Error recovery
        - **If a tool returns an error, STOP.** Do NOT guess or produce an answer — fix the call and retry, or report the error.
        - A result of "0" or empty after an error is not valid — retry with a corrected call first.
        - Never repeat a failed call verbatim. Common errors:
          - `"column not found"` / `"Unknown column in filter: 'a_pk'"` → used an output column name (`a_`/`b_` prefix) instead of the plain schema name
          - `"Aggregate function not allowed"` → pass exactly ONE keyword (COUNT, COUNT_DISTINCT, SUM, AVG, MIN, MAX); SUM/AVG only on Integer columns
          - `"requires valueColumn"` → COUNT_DISTINCT/SUM/AVG/MIN/MAX need `valueColumn`; plain COUNT does not
          - `"groupByColumn must be a plain column name"` → use `groupByDatePart` instead of SQL expression
          - `"at least one column-to-column comparison required"` → move constant-value conditions from `joinConditions` to `filters`/`datePartFilters`
          - `"Command Timeout expired"` / `"timeout"` → the query is too heavy. Do NOT give up or ask the user to narrow scope. Retry: (1) add `IS NOT NULL` filters on all columns used in joinConditions, (2) add `datePartFilters` year restriction ONLY IF same-entity / same-day cross-row conditions are NOT already in `joinConditions` — those conditions already make the query highly selective and a year filter is not needed, (3) if still timing out, split into multiple calls by date range and combine results

        #### Syntax reference
        Consult this section when constructing tool parameters.

        **Array parameters:** `selectColumns`, `filters`, `datePartFilters`, `joinConditions`, `displayColumns` — always JSON arrays, never a JSON-encoded string. One condition per element.
          - Wrong: `selectColumns="[\"col_a\",\"col_b\"]"`. Correct: `selectColumns=["col_a","col_b"]`.
          - Wrong: `datePartFilters=["col_date MONTH IN 6,7 AND col_date YEAR = 2025"]`. Correct: `datePartFilters=["col_date MONTH IN 6,7", "col_date YEAR = 2025"]`.

        **Filters:** `"column OPERATOR value"`. Operators: `=`, `!=`, `<`, `>`, `<=`, `>=`, `LIKE`, `NOT LIKE`, `IS NULL`, `IS NOT NULL`, `IN`, `NOT IN`. `IN`/`NOT IN` values are comma-separated. Do NOT put date-diff or date-part expressions here.
          - **Column-to-column comparison** ("date A is on or after date B", "value in col_a exceeds col_b"): use a plain filter `"col_a >= col_b"`. Do NOT use `dateDiffFilter` for this — `dateDiffFilter` computes a numeric gap in days/hours and is not equivalent to a direct column comparison.
          - **Exact time-of-day matching:** when the question asks about records starting or ending at a specific clock time (e.g. "from 09:00 to 18:00"), use `LIKE` filters on the DateTime column — e.g. `filters=["col_start LIKE % 09:00%", "col_end LIKE % 18:00%"]`. Do NOT use `dateDiffFilter` for this — duration matching (e.g. "9 HOURS") is NOT equivalent to fixed-time matching because an interval can last 9 hours without starting at 09:00.
          - **Filter values must be literals** — a number, a string, or another column name. Do NOT use arithmetic expressions (e.g. `"col_start > col_end - 20"` is wrong). For "N days before/after col_b" use `dateDiffFilter` instead: `"col_start col_end > 20"`.

        **Date-part filters:** `"column DATE_PART OPERATOR value[,v2,...]"`. DATE_PART: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK. DAYOFWEEK values: 1=Sunday, 2=Monday, 3=Tuesday, 4=Wednesday, 5=Thursday, 6=Friday, 7=Saturday. Multiple values of same part — use `IN` (two `=` are ANDed → zero rows).
          - Wrong: `["col_date YEAR = 2025", "col_date YEAR = 2026"]`. Correct: `["col_date YEAR IN 2025,2026"]`.
          - Example (Mondays): `["col_start DAYOFWEEK = 2", "col_start YEAR = 2025"]`.

        **Date-diff filter:** `"col_a col_b OPERATOR days"`. Use ONLY when the threshold is a meaningful number of days/hours/minutes ("submitted less than 7 days before start"). Wrong: `"DATEDIFF(col_start, col_end) < 7"`. Correct: `"col_start col_end < 7"`. Combinable with other parameters.
          - Do NOT use `dateDiffFilter` with `> 0`, `< 0`, or `= 0` to check column ordering — use a plain filter `"col_a > col_b"` instead. Example: "submission date is later than end date" → `filters=["col_submitted > col_end"]`, NOT `dateDiffFilter="col_submitted col_end > 0"`.

        **Grouping:** `groupByColumn` — schema column name, not a keyword. Wrong: `"YEAR"`. Correct: `"col_date"` + `groupByDatePart="YEAR"`. Use `secondGroupByColumn`/`secondGroupByDatePart` for two-dimensional breakdowns, `thirdGroupByColumn`/`thirdGroupByDatePart` for three-dimensional (e.g. entity + year + month).

        **REMINDER: EVERY answer about form data MUST be preceded by a tool call. No exceptions. If you are about to answer without calling a tool — STOP and call a tool first.**
        </form_data_rules>
        """;

    public bool Owns(string toolName)
    {
        return _toolNames.Contains(toolName);
    }

    public async Task<ToolBundle> BuildAsync(ToolContext context)
    {
        if (context.FormId <= 0 || !externalDatabaseClient.IsEnabled())
        {
            return ToolBundle.Empty;
        }

        var init = await TryInitAsync(context.FormId);
        if (init is null)
        {
            return ToolBundle.Empty;
        }

        var (tableName, rowCount, columns, allowedColumns, pkColumn) = init;
        var schemaText = FormatSchema(tableName, rowCount, columns);

        var prompt =
            $"""
             {FormDataRules}

             {schemaText}
             """;

        var tools = new List<AiTool>
        {
            new(AggregateName, MakeAggregateFunction(tableName, allowedColumns, columns)),
            new(QueryName, MakeQueryFunction(tableName, allowedColumns, columns, rowCount))
        };

        if (columns.Count >= 2)
        {
            tools.Add(new AiTool(SelfJoinName, MakeSelfJoinFunction(tableName, allowedColumns, columns, pkColumn)));
        }

        return new ToolBundle(prompt, tools);
    }

    private async Task<InitData?> TryInitAsync(int fileId)
    {
        try
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var file = await fileDao.GetFileAsync(fileId);
            if (file == null || !await fileSecurity.CanEditAsync(file))
            {
                return null;
            }

            var properties = await fileDao.GetProperties(fileId);
            var formFilling = properties?.FormFilling;

            if (formFilling?.StartFilling != true || formFilling.OriginalFormId != fileId)
            {
                return null;
            }

            var tableName = FormFillingReportCreator.GetTableName(fileId, file.Version);
            if (!await externalDatabaseClient.TableExistsAsync(tableName))
            {
                return null;
            }

            var rowCount = await externalDatabaseClient.CountAsync(tableName);
            var columns = (await formFillingReportCreator.GetColumnDefinitionsAsync(fileId, file.Version)).ToList();
            var allowedColumns = columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var pkColumn = columns.FirstOrDefault(c => c.IsPrimaryKey)
                ?? columns.FirstOrDefault(c => c.Type == DbColumnType.Integer)
                ?? columns.FirstOrDefault();

            return new InitData(tableName, rowCount, columns, allowedColumns, pkColumn?.Name ?? string.Empty);
        }
        catch (Exception e)
        {
            logger.WarnFormDataToolsFailed(e, fileId);
            return null;
        }
    }

    private AIFunction MakeAggregateFunction(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IReadOnlyList<DbColumnDefinition> columns)
    {
        var description =
            $"""
             Compute statistics and distributions over form submissions directly in the database.
             Analyses ALL rows server-side regardless of table size — there is no row limit on the input.
             Use this instead of '{QueryName}' for ANY counting, grouping, percentage, ratio, average, sum, min, max, top N, ranking, or trend question.
             Supports single-column, two-column, or three-column GROUP BY; HAVING; NOT IN exclusion subqueries; date-part extraction (YEAR/MONTH/WEEK/DAYOFYEAR/QUARTER/DAYOFWEEK); and aggregation over date differences.
             Combine multiple values of the same date part with IN — two separate '=' filters for the same column are ANDed and always return zero rows.
             SUM/AVG require Integer columns; use COUNT_DISTINCT for string columns. NULL awareness: COUNT(*) includes NULLs, COUNT(valueColumn) excludes them.
             {FormatTableAndColumns(tableName, columns)}
             """;

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = AggregateName,
            Description = description,
            SerializerOptions = _flexibleJsonOptions
        });

        Task<string> Function(
            [Description("Exactly one aggregate function keyword: COUNT, COUNT_DISTINCT, SUM, AVG, MIN or MAX. COUNT counts rows (or non-null values when valueColumn is set). COUNT_DISTINCT counts unique non-null values (requires valueColumn). SUM/AVG/MIN/MAX require either valueColumn (Integer columns) or dateDiffValueExpr. Pass exactly one keyword — never a comma-separated list.")] string aggregateFunction,
            [Description("Column to apply the aggregate function to. Omit only for plain COUNT(*) or when using dateDiffValueExpr. Required for COUNT_DISTINCT, SUM, AVG, MIN, MAX on Integer columns.")] string? valueColumn = null,
            [Description("Plain column name from the schema to group results by. Do NOT pass SQL expressions like DATE_FORMAT() — use groupByDatePart for date grouping (YEAR/MONTH/WEEK/DAYOFYEAR/QUARTER/DAYOFWEEK).")] string? groupByColumn = null,
            [Description("Row-level filters applied with AND. Format: \"column OPERATOR value\" (e.g. \"col_status = approved\", \"col_age > 25\", \"col_status IN approved,pending\"). Column-to-column comparison: \"col_start < col_end\". Operators: =, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL, IN, NOT IN. IN/NOT IN values are comma-separated.")] IEnumerable<string>? filters = null,
            [Description("Date part extracted from groupByColumn: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK. DAYOFWEEK: 1=Sunday … 7=Saturday. Example: groupByColumn='col_start_date', groupByDatePart='MONTH'.")] string? groupByDatePart = null,
            [Description("Second column for two-dimensional grouping (e.g. category + date year). Combine with secondGroupByDatePart when this column is a date.")] string? secondGroupByColumn = null,
            [Description("Date part for secondGroupByColumn. Same values as groupByDatePart.")] string? secondGroupByDatePart = null,
            [Description("Third column for three-dimensional grouping (e.g. entity + year + month). Combine with thirdGroupByDatePart when this column is a date.")] string? thirdGroupByColumn = null,
            [Description("Date part for thirdGroupByColumn. Same values as groupByDatePart.")] string? thirdGroupByDatePart = null,
            [Description("Date-part filters applied with AND. Format: \"column DATE_PART OPERATOR value[,v2,...]\". Date parts: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK. Operators: =, !=, <, >, <=, >=, IN. Examples: \"col_date MONTH IN 6,7,8\", \"col_date YEAR >= 2022\".")] IEnumerable<string>? datePartFilters = null,
            [Description("Filter on the difference between two date/datetime columns. Format: \"col_a col_b OPERATOR value [UNIT]\". UNIT defaults to DAYS. Allowed units: DAYS, HOURS, MINUTES. Example: \"col_start col_submitted < 48 HOURS\".")] string? dateDiffFilter = null,
            [Description("Aggregate over the difference between two DateTime columns instead of a single valueColumn. Format: \"col_a col_b [UNIT]\". UNIT defaults to DAYS; allowed: DAYS, HOURS, MINUTES. Example: \"col_created col_submitted HOURS\" with aggregateFunction='AVG' yields the average hours elapsed.")] string? dateDiffValueExpr = null,
            [Description("Post-aggregation filter (HAVING clause). Format: \"OPERATOR value\" — e.g. \"> 5\". Operators: =, !=, <, >, <=, >=. Only applies when groupByColumn is set.")] string? having = null,
            [Description("Exclude groupByColumn values that appear in rows matching these filter conditions (generates NOT IN subquery). Use for \"which entities had NO matching records?\" questions.")] IEnumerable<string>? excludeFilters = null,
            [Description("Exclude groupByColumn values that appear in rows matching these date-part conditions (generates NOT IN subquery). Example: excludeDatePartFilters=[\"col_date YEAR = 2025\"] returns only entities with no record in 2025.")] IEnumerable<string>? excludeDatePartFilters = null,
            [Description("When true, returns a single number — the count of distinct groups satisfying the query. Requires groupByColumn. Use when the question asks for a single count of qualifying groups rather than the list.")] bool countGroupsOnly = false)
        {
            if (groupByColumn != null && _validDateParts.Contains(groupByColumn))
            {
                throw new ArgumentException(
                    $"'{groupByColumn}' is a date-part keyword, not a column name. " +
                    $"Set groupByDatePart='{groupByColumn.ToUpperInvariant()}' and groupByColumn to the actual date column name from the schema.");
            }

            if (groupByColumn != null && groupByColumn.Contains(' ') && !groupByColumn.Contains('('))
            {
                var gParts = groupByColumn.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (gParts.Length == 2 && _validDateParts.Contains(gParts[1].Trim()))
                {
                    groupByDatePart ??= gParts[1].Trim().ToUpperInvariant();
                    groupByColumn = gParts[0].Trim();
                }
                else
                {
                    throw new ArgumentException(
                        $"groupByColumn must be a plain column name, not a SQL expression (got: \"{groupByColumn}\"). " +
                        "For date grouping use groupByDatePart='YEAR'/'MONTH'/'WEEK'/'DAYOFYEAR'/'QUARTER'/'DAYOFWEEK' together with the column name.");
                }
            }
            else if (groupByColumn != null && groupByColumn.Contains('('))
            {
                throw new ArgumentException(
                    $"groupByColumn must be a plain column name, not a SQL expression (got: \"{groupByColumn}\"). " +
                    "For date grouping use groupByDatePart='YEAR'/'MONTH'/'WEEK'/'DAYOFYEAR'/'QUARTER'/'DAYOFWEEK' together with the column name.");
            }

            var (normalFilters, autoDatePartFilters, autoDiff) = ExtractDatePartFilters(filters);
            dateDiffFilter ??= autoDiff;
            var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters);

            var parsedFilters = normalFilters.Select(QueryFilter.Parse);
            var parsedDatePartFilters = allDatePartFilters.Select(DatePartFilter.Parse);
            var parsedDateDiffFilter = dateDiffFilter != null ? DateDiffFilter.Parse(dateDiffFilter) : null;
            var parsedDateDiffAggregate = dateDiffValueExpr != null ? DateDiffAggregate.Parse(dateDiffValueExpr) : null;
            var (normalExcludeFilters, autoExcludeDatePartFilters, _) = ExtractDatePartFilters(excludeFilters);
            var allExcludeDatePartFilters = (excludeDatePartFilters ?? []).Concat(autoExcludeDatePartFilters);
            var parsedExcludeFilters = normalExcludeFilters.Select(QueryFilter.Parse);
            var parsedExcludeDatePartFilters = allExcludeDatePartFilters.Select(DatePartFilter.Parse);

            return externalDatabaseClient.AggregateAsync(
                tableName, allowedColumns, aggregateFunction, valueColumn, groupByColumn, parsedFilters,
                groupByDatePart, secondGroupByColumn, secondGroupByDatePart,
                parsedDatePartFilters, parsedDateDiffFilter, parsedDateDiffAggregate,
                havingFilter: having, thirdGroupByColumn: thirdGroupByColumn, thirdGroupByDatePart: thirdGroupByDatePart,
                excludeFilters: parsedExcludeFilters, excludeDatePartFilters: parsedExcludeDatePartFilters,
                countGroupsOnly: countGroupsOnly);
        }
    }

    private AIFunction MakeQueryFunction(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IReadOnlyList<DbColumnDefinition> columns,
        long rowCount)
    {
        var description =
            $"""
             Retrieve specific rows from form submission data.
             Use ONLY to display individual records — 'show me', 'list', 'find the rows', 'which records'.
             PROHIBITED for any counting, summing, averaging, grouping, or statistics — use '{AggregateName}' for those.
             Returns at most {ExternalDatabaseClient.MaxRowsPerRequest} rows per call (out of {rowCount:N0} total).
             Always specify selectColumns (only the columns you need) and filters to narrow the result set.
             NULL awareness: use IS NULL / IS NOT NULL filters to control inclusion of nulls.
             {FormatTableAndColumns(tableName, columns)}
             """;

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = QueryName,
            Description = description,
            SerializerOptions = _flexibleJsonOptions
        });

        Task<string> Function(
            [Description("Column names to include in the result. Always pick only what is needed. JSON array of strings — e.g. [\"col_a\",\"col_b\"].")] IEnumerable<string>? selectColumns = null,
            [Description("Filter conditions applied with AND. Format: \"column OPERATOR value\" — e.g. \"col_status = approved\", \"col_status IN approved,pending\", \"col_name IS NULL\". Column-to-column comparison: \"col_start < col_end\". Operators: =, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL, IN, NOT IN. IN/NOT IN values are comma-separated.")] IEnumerable<string>? filters = null,
            [Description("Primary column to sort results by.")] string? orderByColumn = null,
            [Description("True to sort orderByColumn descending. Default: false.")] bool orderByDescending = false,
            [Description("Secondary column to sort by when rows tie on orderByColumn.")] string? thenByColumn = null,
            [Description("True to sort thenByColumn descending. Default: false.")] bool thenByDescending = false,
            [Description("Maximum rows to return (1–500). Default: 50.")] int limit = 50,
            [Description("Rows to skip for pagination. Default: 0.")] int offset = 0,
            [Description("Date-part filters applied with AND. Format: \"column DATE_PART OPERATOR value[,v2,...]\". Date parts: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER. Operators: =, !=, <, >, <=, >=, IN.")] IEnumerable<string>? datePartFilters = null,
            [Description("Filter on the difference between two date/datetime columns. Format: \"col_a col_b OPERATOR value [UNIT]\". UNIT defaults to DAYS; allowed: DAYS, HOURS, MINUTES.")] string? dateDiffFilter = null)
        {
            var (normalFilters, autoDatePartFilters, autoDiff) = ExtractDatePartFilters(filters);
            dateDiffFilter ??= autoDiff;
            var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters);

            var parsedFilters = normalFilters.Select(QueryFilter.Parse);
            var parsedDatePartFilters = allDatePartFilters.Select(DatePartFilter.Parse);
            var parsedDateDiffFilter = dateDiffFilter != null ? DateDiffFilter.Parse(dateDiffFilter) : null;

            return externalDatabaseClient.QueryAsync(
                tableName, allowedColumns, selectColumns, parsedFilters,
                orderByColumn, orderByDescending, thenByColumn, thenByDescending, limit, offset,
                parsedDatePartFilters, parsedDateDiffFilter);
        }
    }

    private AIFunction MakeSelfJoinFunction(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IReadOnlyList<DbColumnDefinition> columns,
        string pkColumn)
    {
        var description =
            $"""
             Compare every record against every other record (self-join) to find related pairs: overlapping time periods, scheduling conflicts, concurrent events, or any "find pairs where ..." question.
             Do NOT use '{QueryName}' to fetch rows for manual comparison — always use this tool instead.
             Each joinCondition compares record A (left) vs record B (right) with plain column names — never add a_/b_ prefixes (those appear only in output).
             Operators: =, !=, <, >, <=, >=. Overlap pattern (date-only, inclusive): ["col_start <= col_end", "col_end >= col_start"]. Strict pattern (datetime): ["col_start < col_end", "col_end > col_start"].
             For same-entity overlaps add an equality condition on the entity column. For same-calendar-day add cross-row YEAR and DAYOFYEAR conditions.
             Returns at most 500 pairs per call. Apply filters or datePartFilters to narrow the pair space on large tables.
             NULL awareness: NULL values never match — pre-filter with IS NOT NULL.
             {FormatTableAndColumns(tableName, columns)}
             """;

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = SelfJoinName,
            Description = description,
            SerializerOptions = _flexibleJsonOptions
        });

        Task<string> Function(
            [Description("Cross-row conditions comparing a column from record A vs a column from record B. Format: \"left_col OPERATOR right_col\" with plain column names from the schema — no a_/b_ prefixes (those appear only in output). Operators: =, !=, <, >, <=, >=. Overlap default: [\"col_start <= col_end\", \"col_end >= col_start\"]. Same-value pairs: [\"col_name = col_name\"]. Date-part cross-row comparisons supported: \"col_date YEAR = col_date YEAR\".")] IEnumerable<string> joinConditions,
            [Description("Plain column names from both records to include in output. JSON array of strings — one name per element. Each column appears twice in output, prefixed with 'a_' and 'b_'.")] IEnumerable<string>? displayColumns = null,
            [Description("Maximum pairs to return (1–500). Default: 100.")] int limit = 100,
            [Description("Row-level filters applied to BOTH records with AND. Format: \"column OPERATOR value\". Operators: =, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL, IN, NOT IN. Use plain schema column names — never output-prefixed names (a_pk, b_pk).")] IEnumerable<string>? filters = null,
            [Description("Date-part filters applied to BOTH records. Format: \"column DATE_PART OPERATOR value[,v2,...]\". Date parts: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK.")] IEnumerable<string>? datePartFilters = null,
            [Description("Plain column name. When set, returns a single count of distinct values of this column from record A instead of individual pairs — useful for \"how many distinct entities have overlapping records?\" questions. Returns [{\"count\": N}].")] string? countDistinctColumn = null)
        {
            if (string.IsNullOrEmpty(pkColumn))
            {
                throw new InvalidOperationException("Self-join unavailable: no primary key column resolved.");
            }

            var (normalFilters, autoDatePartFilters, _) = ExtractDatePartFilters(filters);

            var joinList = joinConditions.ToList();
            var (parsedJoins, extraDatePart) = SeparateDatePartJoins(joinList);

            var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters).Concat(extraDatePart);
            var (extraJoins, cleanDatePartFilters) = SeparateDatePartJoins(allDatePartFilters);

            var parsedJoinList = parsedJoins.Concat(extraJoins).ToList();
            if (parsedJoinList.Count == 0)
            {
                throw new ArgumentException(
                    "joinConditions must contain at least one column-to-column comparison (e.g. \"col_start_date <= col_end_date\"). " +
                    "Date-part comparisons like \"col YEAR = 2025\" belong in datePartFilters, not joinConditions.");
            }

            var parsedFilters = normalFilters.Select(QueryFilter.Parse);
            var parsedDatePartFilters = cleanDatePartFilters.Select(DatePartFilter.Parse);
            return externalDatabaseClient.SelfJoinAsync(
                tableName, allowedColumns, pkColumn, parsedJoinList, displayColumns, limit,
                parsedFilters, parsedDatePartFilters, countDistinctColumn);
        }
    }

    private static (IEnumerable<SelfJoinCondition> parsedJoins, IEnumerable<string> datePart) SeparateDatePartJoins(IEnumerable<string> joinConditions)
    {
        var parsed = new List<SelfJoinCondition>();
        var datePart = new List<string>();

        foreach (var rawCond in joinConditions)
        {
            var cond = rawCond.Trim();
            var parts = cond.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 && _validDateParts.Contains(parts[1]))
            {
                if (parts.Length >= 5 && _validDateParts.Contains(parts[4]))
                {
                    parsed.Add(new SelfJoinCondition(parts[0], parts[2].ToUpperInvariant(), parts[3], parts[1].ToUpperInvariant()));
                }
                else if (parts.Length >= 4)
                {
                    datePart.Add($"{parts[0]} {parts[1]} {parts[2]} {parts[3].TrimEnd(',')}");
                }
            }
            else if (parts.Length == 3)
            {
                var dp = _validDateParts.FirstOrDefault(d =>
                    parts[0].EndsWith("_" + d, StringComparison.OrdinalIgnoreCase) &&
                    parts[2].EndsWith("_" + d, StringComparison.OrdinalIgnoreCase));
                if (dp != null)
                {
                    var leftCol = parts[0][..^(dp.Length + 1)];
                    var rightCol = parts[2][..^(dp.Length + 1)];
                    parsed.Add(new SelfJoinCondition(leftCol, parts[1].ToUpperInvariant(), rightCol, dp.ToUpperInvariant()));
                }
                else
                {
                    try
                    {
                        parsed.Add(SelfJoinCondition.Parse(cond));
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            else
            {
                try
                {
                    parsed.Add(SelfJoinCondition.Parse(cond));
                }
                catch (ArgumentException)
                {
                }
            }
        }

        return (parsed, datePart);
    }

    private static string FormatSchema(string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var columnLines = columns.Select(col =>
        {
            var line = $"- {col.Name} ({col.Type})";
            return col.EnumValues?.Count > 0
                ? $"{line}: {string.Join(", ", col.EnumValues)}"
                : line;
        });

        return $"""
                <form_data_schema>
                ## Form Submissions ({rowCount} total, stored in external database)
                Table: {tableName}
                Schema:
                {string.Join("\n", columnLines)}
                </form_data_schema>
                """;
    }

    private static string FormatTableAndColumns(string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        return $"Table: '{tableName}'. Available columns: {string.Join(", ", columns.Select(FormatColumn))}";
    }

    private static string FormatColumn(DbColumnDefinition c)
    {
        var label = c.Label is not null && c.Label != c.Name ? $" \"{c.Label}\"" : string.Empty;
        var desc = $"{c.Name}{label} ({c.Type})";
        if (c.EnumValues?.Count > 0)
        {
            desc += $" [{string.Join("/", c.EnumValues)}]";
        }
        return desc;
    }

    private static readonly HashSet<string> _validDateParts =
        new(["YEAR", "MONTH", "WEEK", "DAYOFYEAR", "QUARTER", "DAYOFWEEK"], StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _flexibleJsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        Converters = { FlexibleStringArrayJsonConverter.Instance }
    };

    private static (IEnumerable<string> normal, IEnumerable<string> datePart, string? autoDiff)
        ExtractDatePartFilters(IEnumerable<string>? filters)
    {
        if (filters == null)
        {
            return ([], [], null);
        }

        var normal = new List<string>();
        var datePart = new List<string>();
        string? autoDiff = null;

        foreach (var f in filters)
        {
            if (string.IsNullOrWhiteSpace(f))
            {
                continue;
            }

            var parts = f.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts[0].EndsWith(':'))
            {
                continue;
            }

            if (f.TrimStart().StartsWith("DATEDIFF(", StringComparison.OrdinalIgnoreCase))
            {
                var openIdx = f.IndexOf('(');
                var closeIdx = f.IndexOf(')');
                if (openIdx >= 0 && closeIdx > openIdx)
                {
                    var colNames = f[(openIdx + 1)..closeIdx].Split(',', 2);
                    var afterParen = f[(closeIdx + 1)..].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (colNames.Length == 2 && afterParen.Length == 2)
                    {
                        autoDiff ??= $"{colNames[0].Trim()} {colNames[1].Trim()} {afterParen[0]} {afterParen[1]}";
                        continue;
                    }
                }
            }

            if (parts.Length >= 4 && _validDateParts.Contains(parts[1]))
            {
                datePart.Add(f);
                continue;
            }

            if (parts is [_, "-", _, _, ..])
            {
                autoDiff ??= $"{parts[0]} {string.Join(" ", parts[2..])}";
                continue;
            }

            if (parts.Length == 4 && !_validDateParts.Contains(parts[1]) && int.TryParse(parts[3], out _))
            {
                autoDiff ??= $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
                continue;
            }

            normal.Add(f);
        }

        return (normal, datePart, autoDiff);
    }

    private sealed record InitData(
        string TableName,
        long RowCount,
        IReadOnlyList<DbColumnDefinition> Columns,
        IReadOnlyCollection<string> AllowedColumns,
        string PkColumn);

    private sealed class FlexibleStringArrayJsonConverter : JsonConverter<IEnumerable<string>>
    {
        public static readonly FlexibleStringArrayJsonConverter Instance = new();

        public override IEnumerable<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var raw = reader.GetString()!;

                var trimmed = raw.AsSpan().Trim();
                if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<string>>(trimmed, options);
                        if (parsed is not null)
                        {
                            return parsed;
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }

                return [raw];
            }

            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                return list;
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString()!);
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var s in value)
            {
                writer.WriteStringValue(s);
            }
            writer.WriteEndArray();
        }
    }
}

internal static partial class FormDataToolsFactoryLogger
{
    [LoggerMessage(LogLevel.Warning, "Failed to initialize form data tools for file {FileId}")]
    public static partial void WarnFormDataToolsFailed(this ILogger<FormDataToolsFactory> logger, Exception exception, int fileId);
}
