---
name: "formula-builder"
description: "Write a spreadsheet formula from a plain-language description and explain it. Use when the user asks how to calculate, look up, count or sum something in a spreadsheet."
---

# Formula builder

## Steps
1. Restate the task: the input cells or ranges, the condition, the expected result.
2. Choose the function from the function reference; prefer one function over nested ones.
3. Write the formula with the user's ranges and absolute references where it will be copied.
4. Walk through the formula on one example row.

## Output
The formula in a code block, a part-by-part explanation, the expected result for the example, and what to change when the range grows.

## Rules
- Use the argument separator the user's formulas use (comma or semicolon).
- Mention when a formula needs a helper column.
