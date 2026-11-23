# Formula error guide

- #N/A: the lookup value was not found. Check for extra spaces, text versus number, or add the if_not_found argument.
- #VALUE!: an argument has the wrong type, often text in arithmetic. Convert with VALUE or clean the cell.
- #REF!: the formula points to a deleted cell or a column beyond the table. Restore the reference.
- #DIV/0!: division by zero or an empty cell. Test the divisor with IF.
- #NAME?: a misspelt function or an unquoted text. Check the spelling and the quotation marks.
- #NUM!: an impossible number, such as the root of a negative value.
- #SPILL!: the cells an array result needs are not empty. Clear them.
- A wrong result without an error: check relative references after copying, hidden rows, and ranges of different sizes.
