---
name: "trend-and-outliers"
description: "Find trends, comparisons and outliers in tabular data and explain them. Use when the user asks what the data shows, for insights or for anomalies."
---

# Trends and outliers

## Steps
1. Choose the measure and the dimensions (time, category, region) relevant to the question.
2. Calculate totals and changes by period and by category.
3. Flag outliers: values beyond 1.5 times the interquartile range, or a period change above 30%.
4. Look for an explanation of each outlier in the other columns.

## Output
Three to five findings, each as a sentence with the number, the comparison and the calculation used; then a suggested chart for each.

## Rules
- Correlation is reported as correlation, not cause.
- Percentages come with the absolute numbers behind them.
