---
name: "formula-debug"
description: "Find why a formula returns an error or a wrong result and fix it. Use when the user shares a formula that shows an error such as #N/A, #VALUE!, #REF! or an unexpected number."
---

# Formula debug

## Steps
1. Identify the error type and look it up in the error guide.
2. Evaluate the formula from the innermost function outwards and find the first part that fails.
3. Check the usual causes: wrong range size, text instead of numbers, relative references that moved, missing lookup value.
4. Write the corrected formula.

## Output
The cause in one sentence, the corrected formula in a code block, and how to check the fix.

## Rules
- Do not hide an error with IFERROR unless the user asks for it; fix its cause.
- Ask for sample values when the cause depends on the data.
