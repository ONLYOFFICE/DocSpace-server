---
name: "data-profile"
description: "Describe a table: its columns, value ranges, gaps and quality problems. Use first whenever the user shares a spreadsheet to analyse."
---

# Data profile

## Steps
1. List the columns with their type, number of filled cells and number of distinct values.
2. For numeric columns give minimum, maximum, mean and median; for dates the range; for text the most frequent values.
3. Run the data quality checklist and list every problem with the affected rows.

## Output
A column summary table, a list of quality problems, and a one-paragraph description of what one row represents.

## Rules
- Do not correct the data; only report.
- State the number of rows analysed.
