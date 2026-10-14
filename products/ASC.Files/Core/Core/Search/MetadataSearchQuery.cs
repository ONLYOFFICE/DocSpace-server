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

namespace ASC.Web.Files.Core.Search;

/// <summary>
/// Builds the metadata search queries shared by the files and the folders listings:
/// the OpenSearch selectors over the metadata indexes and their SQL counterparts.
/// </summary>
public static class MetadataSearchQuery
{
    /// <summary>
    /// Selects the identifiers of the entries matching the metadata filter from the index.
    /// The result counts as a failure — so the caller falls back to SQL — when the index is unavailable or when the
    /// result reached <see cref="BaseIndexer{T}.QueryLimit"/>: a capped id list intersected with the caller's query
    /// would silently drop matches instead of returning them.
    /// </summary>
    public static Task<(bool Success, List<int> Ids)> TrySelectMetadataIdsAsync<TDoc>(FactoryIndexer<TDoc> indexer, MetadataFilter metadataFilter)
        where TDoc : MetadataSearchItemBase
    {
        var selector = BuildSelector<TDoc>(metadataFilter);

        return TrySelectIdsAsync(indexer, s => selector(s));
    }

    /// <summary>
    /// Selects the identifiers of the entries whose string metadata values match the free text, see <see cref="TrySelectMetadataIdsAsync{TDoc}"/> for the failure rule.
    /// </summary>
    public static Task<(bool Success, List<int> Ids)> TrySelectGlobalTextIdsAsync<TDoc>(FactoryIndexer<TDoc> indexer, string searchText)
        where TDoc : MetadataSearchItemBase
    {
        var selector = BuildGlobalTextSelector<TDoc>(searchText);

        return TrySelectIdsAsync(indexer, s => selector(s));
    }

    private static async Task<(bool Success, List<int> Ids)> TrySelectIdsAsync<TDoc>(FactoryIndexer<TDoc> indexer, Expression<Func<Selector<TDoc>, Selector<TDoc>>> expression)
        where TDoc : MetadataSearchItemBase
    {
        var (success, ids) = await indexer.TrySelectIdsAsync(expression);

        return success && ids.Count < BaseIndexer<TDoc>.QueryLimit ? (true, ids) : (false, []);
    }

    /// <summary>
    /// Narrows an entry query by the structured metadata filter. The template assignment is a fact of the link table,
    /// so it always comes from the database. The field conditions are asked from the metadata index first, and every
    /// condition is then confirmed against the value table for the candidates the index returned: the documents are
    /// written best effort after the values (an index that is down at that moment leaves a document holding the old
    /// or the removed value behind) and the full indexing pass selects the entries by their modified values, so a
    /// removed value is never reconciled — the database is the truth, the index only keeps the work small. When the
    /// index is not there or overflows, the same conditions filter the query on their own. The selector names the
    /// entry identifier of a row: the file or the folder itself, or the entry carried by a tag projection.
    /// The index is asked tenant-wide, never by the place of the entry in the tree: the query is already narrowed
    /// to the folder, the section or the share records by the caller, and a document scoped by the ancestor chain
    /// went stale on every move and hid the entry until the chain was refreshed, which a dropped event never did.
    /// </summary>
    public static async Task<IQueryable<TRow>> ApplyFilterAsync<TRow, TDoc>(
        IQueryable<TRow> query,
        FilesDbContext filesDbContext,
        int tenantId,
        FileEntryType entryType,
        FactoryIndexer<TDoc> indexer,
        MetadataFilter metadataFilter,
        Expression<Func<TRow, int>> idSelector)
        where TDoc : MetadataSearchItemBase
    {
        if (metadataFilter is not { IsEmpty: false })
        {
            return query;
        }

        if (metadataFilter.TemplateId is { } templateId)
        {
            var templateEntryIds = TemplateEntryIds(filesDbContext, tenantId, entryType, templateId);

            query = WhereId(query, idSelector, id => templateEntryIds.Contains(id));
        }

        if (metadataFilter.Conditions.Count == 0)
        {
            return query;
        }

        var (success, candidateIds) = await TrySelectMetadataIdsAsync(indexer, metadataFilter);

        if (success)
        {
            query = WhereId(query, idSelector, id => candidateIds.Contains(id));
        }

        foreach (var condition in metadataFilter.Conditions)
        {
            query = WhereId(query, idSelector, EntryMatchesCondition(filesDbContext, tenantId, entryType, condition));
        }

        return query;
    }

    /// <summary>
    /// Adds the free text part of a listing once the title index answered with <paramref name="titleIds"/>: the string
    /// values of every template (the custom fields included) take part in the text search, so the entries whose
    /// metadata match are united with the entries whose titles match. Without a template the tenant has no values,
    /// and the title ids are applied as they are, which spares the metadata index request and the SQL fallback on
    /// every search. The metadata candidates the index returns are confirmed against the value table, the same way
    /// <see cref="ApplyFilterAsync{TRow, TDoc}"/> confirms its conditions: a document holding a value that has since
    /// been cleared (the index was down at that moment, and the full pass never reconciles a removed value) would
    /// otherwise keep the entry in the text search results for good.
    /// </summary>
    public static async Task<IQueryable<TRow>> ApplyTextSearchAsync<TRow, TDoc>(
        IQueryable<TRow> query,
        FilesDbContext filesDbContext,
        int tenantId,
        FileEntryType entryType,
        FactoryIndexer<TDoc> indexer,
        List<int> titleIds,
        string searchText,
        string lowerText,
        bool hasTemplates,
        Expression<Func<TRow, int>> idSelector)
        where TDoc : MetadataSearchItemBase
    {
        if (!hasTemplates)
        {
            return WhereId(query, idSelector, id => titleIds.Contains(id));
        }

        var (success, globalTextIds) = await TrySelectGlobalTextIdsAsync(indexer, searchText);

        // the metadata index is not there yet (it is created by the first full indexing pass) or it is overflowing:
        // the global metadata part of the search comes from the database alone; with the index it is narrowed to
        // the candidates the index returned first, so the confirmation probes a handful of rows instead of scanning
        var globalTextSqlIds = TemplateTextEntryIds(filesDbContext, tenantId, entryType, lowerText);

        if (success)
        {
            var candidateIds = globalTextIds.Except(titleIds).ToList();

            if (candidateIds.Count == 0)
            {
                return WhereId(query, idSelector, id => titleIds.Contains(id));
            }

            var confirmedIds = globalTextSqlIds.Where(id => candidateIds.Contains(id));

            return WhereId(query, idSelector, id => titleIds.Contains(id) || confirmedIds.Contains(id));
        }

        return WhereId(query, idSelector, id => titleIds.Contains(id) || globalTextSqlIds.Contains(id));
    }

    /// <summary>
    /// Builds the OpenSearch selector for the structured metadata filter. All conditions are combined with AND,
    /// the options within a single choice condition are combined with OR. The exact string match is a term on the
    /// keyword value, the substring match a wildcard per word on its text copy, the way the titles are searched.
    /// </summary>
    public static Func<Selector<TDoc>, Selector<TDoc>> BuildSelector<TDoc>(MetadataFilter metadataFilter)
        where TDoc : MetadataSearchItemBase
    {
        return s =>
        {
            foreach (var condition in metadataFilter.Conditions)
            {
                switch (condition.FieldType)
                {
                    case MetadataFieldType.String when condition.StringContains:
                        s.Nested(a => a.Values, b =>
                            b.Term(c => c.Values.Select(v => v.FieldId), condition.FieldId) &&
                            Selector<TDoc>.Contains(b, c => c.Values.Select(v => v.StringText), condition.StringValue));
                        break;
                    case MetadataFieldType.String:
                        s.Nested(a => a.Values, b =>
                            b.Term(c => c.Values.Select(v => v.FieldId), condition.FieldId) &&
                            b.Term(c => c.Values.Select(v => v.StringValue), condition.StringValue));
                        break;
                    case MetadataFieldType.Date:
                        s.Nested(a => a.Values, b =>
                            b.Term(c => c.Values.Select(v => v.FieldId), condition.FieldId) &&
                            b.DateRange(r =>
                            {
                                r.Field(c => c.Values.Select(v => v.DateValue));

                                if (condition.DateFrom.HasValue)
                                {
                                    if (condition.FromExclusive)
                                    {
                                        r.GreaterThan(condition.DateFrom.Value);
                                    }
                                    else
                                    {
                                        r.GreaterThanOrEquals(condition.DateFrom.Value);
                                    }
                                }

                                if (condition.DateTo.HasValue)
                                {
                                    if (condition.ToExclusive)
                                    {
                                        r.LessThan(condition.DateTo.Value);
                                    }
                                    else
                                    {
                                        r.LessThanOrEquals(condition.DateTo.Value);
                                    }
                                }

                                return r;
                            }));
                        break;
                    case MetadataFieldType.Number:
                        s.Nested(a => a.Values, b =>
                            b.Term(c => c.Values.Select(v => v.FieldId), condition.FieldId) &&
                            b.Range(r =>
                            {
                                r.Field(c => c.Values.Select(v => v.NumberValue));

                                if (condition.NumberFrom.HasValue)
                                {
                                    if (condition.FromExclusive)
                                    {
                                        r.GreaterThan(condition.NumberFrom.Value);
                                    }
                                    else
                                    {
                                        r.GreaterThanOrEquals(condition.NumberFrom.Value);
                                    }
                                }

                                if (condition.NumberTo.HasValue)
                                {
                                    if (condition.ToExclusive)
                                    {
                                        r.LessThan(condition.NumberTo.Value);
                                    }
                                    else
                                    {
                                        r.LessThanOrEquals(condition.NumberTo.Value);
                                    }
                                }

                                return r;
                            }));
                        break;
                    case MetadataFieldType.SingleChoice:
                    case MetadataFieldType.MultiChoice:
                        var optionIds = condition.OptionIds.Select(id => id.ToString()).ToArray();

                        s.Nested(a => a.Values, b =>
                            b.Term(c => c.Values.Select(v => v.FieldId), condition.FieldId) &&
                            b.Terms(t => t.Field(c => c.Values.Select(v => v.OptionIds)).Terms(optionIds)));
                        break;
                }
            }

            s.Limit(0, BaseIndexer<TDoc>.QueryLimit);

            return s;
        };
    }

    /// <summary>
    /// Builds the OpenSearch selector for the free text search over the string values of the entries.
    /// </summary>
    public static Func<Selector<TDoc>, Selector<TDoc>> BuildGlobalTextSelector<TDoc>(string searchText)
        where TDoc : MetadataSearchItemBase
    {
        return s =>
        {
            s.Match(r => r.GlobalText, searchText);

            s.Limit(0, BaseIndexer<TDoc>.QueryLimit);

            return s;
        };
    }

    /// <summary>
    /// The identifiers of the entries the template is assigned to, directly or by inheritance. Always evaluated in SQL:
    /// the metadata documents exist for the entries holding values only, so an index lookup would miss an assignment
    /// without values, and the link table is keyed by the tenant and the template anyway.
    /// </summary>
    public static IQueryable<int> TemplateEntryIds(FilesDbContext filesDbContext, int tenantId, FileEntryType entryType, int templateId)
    {
        return filesDbContext.MetadataLinks
            .Where(l => l.TenantId == tenantId && l.TemplateId == templateId && l.EntryType == entryType)
            .Select(l => l.EntryId);
    }

    /// <summary>
    /// The SQL counterpart of <see cref="BuildGlobalTextSelector{TDoc}"/>: the identifiers of the entries holding a
    /// string value, of whatever template, that contains the text. The text is expected to be already lowered.
    /// </summary>
    public static IQueryable<int> TemplateTextEntryIds(FilesDbContext filesDbContext, int tenantId, FileEntryType entryType, string lowerText)
    {
        return filesDbContext.MetadataValues
            .Where(v => v.TenantId == tenantId && v.EntryType == entryType && v.ValueString.ToLower().Contains(lowerText))
            .Select(v => v.EntryId)
            .Distinct();
    }

    /// <summary>
    /// The SQL counterpart of one condition of <see cref="BuildSelector{TDoc}"/>: whether the entry holds a value row
    /// matching the condition. A correlated existence check, so confirming a candidate costs one probe of the value
    /// table by its primary key.
    /// </summary>
    public static Expression<Func<int, bool>> EntryMatchesCondition(FilesDbContext filesDbContext, int tenantId, FileEntryType entryType, MetadataFilterCondition condition)
    {
        var valuePredicate = BuildConditionPredicate(condition);

        Expression<Func<int, bool>> entryPredicate = id => filesDbContext.MetadataValues.Any(v =>
            v.TenantId == tenantId && v.EntryType == entryType && v.EntryId == id && ValueMatches(v));

        return (Expression<Func<int, bool>>)new InlinePredicateVisitor(valuePredicate).Visit(entryPredicate);
    }

    /// <summary>
    /// The predicate matching a single metadata value row against one filter condition.
    /// The tenant and the entry type are expected to be applied by the caller.
    /// </summary>
    public static Expression<Func<DbFilesMetadataValue, bool>> BuildConditionPredicate(MetadataFilterCondition condition)
    {
        var fieldId = condition.FieldId;

        switch (condition.FieldType)
        {
            case MetadataFieldType.String:
                var stringValue = condition.StringValue;
                return condition.StringContains
                    ? v => v.FieldId == fieldId && v.ValueString.ToLower().Contains(stringValue)
                    : v => v.FieldId == fieldId && v.ValueString.ToLower() == stringValue;

            case MetadataFieldType.Date:
                var dateParts = new List<Expression<Func<DbFilesMetadataValue, bool>>> { v => v.FieldId == fieldId };

                if (condition.DateFrom is { } dateFrom)
                {
                    dateParts.Add(condition.FromExclusive ? v => v.ValueDate > dateFrom : v => v.ValueDate >= dateFrom);
                }

                if (condition.DateTo is { } dateTo)
                {
                    dateParts.Add(condition.ToExclusive ? v => v.ValueDate < dateTo : v => v.ValueDate <= dateTo);
                }

                return And(dateParts);

            case MetadataFieldType.Number:
                var numberParts = new List<Expression<Func<DbFilesMetadataValue, bool>>> { v => v.FieldId == fieldId };

                if (condition.NumberFrom is { } numberFrom)
                {
                    numberParts.Add(condition.FromExclusive ? v => v.ValueNumber > numberFrom : v => v.ValueNumber >= numberFrom);
                }

                if (condition.NumberTo is { } numberTo)
                {
                    numberParts.Add(condition.ToExclusive ? v => v.ValueNumber < numberTo : v => v.ValueNumber <= numberTo);
                }

                return And(numberParts);

            case MetadataFieldType.SingleChoice:
            case MetadataFieldType.MultiChoice:
                var optionIds = condition.OptionIds.Select(id => id.ToString()).ToList();
                return v => v.FieldId == fieldId && optionIds.Contains(v.OptionId);

            default:
                throw new ArgumentOutOfRangeException(nameof(condition), condition.FieldType, @"Unknown metadata field type");
        }
    }

    /// <summary>
    /// Joins the value predicates with AND into one lambda over a single parameter, so the bounds of a range are
    /// chosen in code (strict or inclusive, present or absent) instead of being switched inside the SQL.
    /// </summary>
    private static Expression<Func<DbFilesMetadataValue, bool>> And(List<Expression<Func<DbFilesMetadataValue, bool>>> parts)
    {
        var first = parts[0];
        var body = first.Body;

        foreach (var part in parts.Skip(1))
        {
            body = Expression.AndAlso(body, new ParameterReplaceVisitor(part.Parameters[0], first.Parameters[0]).Visit(part.Body));
        }

        return Expression.Lambda<Func<DbFilesMetadataValue, bool>>(body, first.Parameters);
    }

    /// <summary>
    /// Applies a predicate over the entry identifier to the rows of the query, whatever carries the identifier. The
    /// predicate is written once against the identifier and bound to the row through <paramref name="idSelector"/>,
    /// so the files, the folders and their tag projections share one filter instead of a copy each; the captured
    /// variables stay in place, and the provider translates the result exactly as the inlined lambda.
    /// </summary>
    private static IQueryable<TRow> WhereId<TRow>(IQueryable<TRow> query, Expression<Func<TRow, int>> idSelector, Expression<Func<int, bool>> predicate)
    {
        var body = new ParameterReplaceVisitor(predicate.Parameters[0], idSelector.Body).Visit(predicate.Body);

        return query.Where(Expression.Lambda<Func<TRow, bool>>(body, idSelector.Parameters));
    }

    /// <summary>
    /// The placeholder <see cref="InlinePredicateVisitor"/> replaces with the condition predicate; it is never invoked.
    /// </summary>
    private static bool ValueMatches(DbFilesMetadataValue value)
    {
        throw new InvalidOperationException("The placeholder must be replaced before the query runs");
    }

    /// <summary>
    /// Replaces the <see cref="ValueMatches"/> call with the body of the value predicate, bound to the argument of the call.
    /// </summary>
    private sealed class InlinePredicateVisitor(Expression<Func<DbFilesMetadataValue, bool>> predicate) : ExpressionVisitor
    {
        private static readonly MethodInfo _placeholder = typeof(MetadataSearchQuery).GetMethod(nameof(ValueMatches), BindingFlags.Static | BindingFlags.NonPublic);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == _placeholder)
            {
                return new ParameterReplaceVisitor(predicate.Parameters[0], node.Arguments[0]).Visit(predicate.Body);
            }

            return base.VisitMethodCall(node);
        }
    }

    private sealed class ParameterReplaceVisitor(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == parameter ? replacement : base.VisitParameter(node);
        }
    }
}
