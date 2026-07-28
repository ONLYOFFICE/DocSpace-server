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
/// The way the metadata search is narrowed to a part of the folder tree.
/// </summary>
public enum MetadataSearchScopeType
{
    /// <summary>
    /// The whole tenant. The caller is expected to intersect the result with its own entry set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The direct children of the folder.
    /// </summary>
    Parent = 1,

    /// <summary>
    /// The whole subtree of the folder.
    /// </summary>
    Subtree = 2
}

/// <summary>
/// The scope of the metadata search.
/// </summary>
public readonly record struct MetadataSearchScope(MetadataSearchScopeType Type, int ParentId)
{
    public static readonly MetadataSearchScope None = new(MetadataSearchScopeType.None, 0);

    public static MetadataSearchScope Parent(int parentId)
    {
        return new MetadataSearchScope(MetadataSearchScopeType.Parent, parentId);
    }

    public static MetadataSearchScope Subtree(int parentId)
    {
        return new MetadataSearchScope(MetadataSearchScopeType.Subtree, parentId);
    }

    public static MetadataSearchScope For(int parentId, bool withSubfolders)
    {
        return withSubfolders ? Subtree(parentId) : Parent(parentId);
    }
}

/// <summary>
/// Builds the metadata search queries shared by the files and the folders listings:
/// the OpenSearch selectors over the metadata indexes and their SQL counterparts.
/// </summary>
public static class MetadataSearchQuery
{
    /// <summary>
    /// Builds the OpenSearch selector for the structured metadata filter. All conditions are combined with AND,
    /// the options within a single choice condition are combined with OR.
    /// </summary>
    public static Func<Selector<TDoc>, Selector<TDoc>> BuildSelector<TDoc>(MetadataFilter metadataFilter, MetadataSearchScope scope)
        where TDoc : MetadataSearchItemBase
    {
        return s =>
        {
            ApplyScope(s, scope);

            foreach (var condition in metadataFilter.Conditions)
            {
                switch (condition.FieldType)
                {
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
                                    r.GreaterThanOrEquals(condition.DateFrom.Value);
                                }

                                if (condition.DateTo.HasValue)
                                {
                                    r.LessThanOrEquals(condition.DateTo.Value);
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
                                    r.GreaterThanOrEquals(condition.NumberFrom.Value);
                                }

                                if (condition.NumberTo.HasValue)
                                {
                                    r.LessThanOrEquals(condition.NumberTo.Value);
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
    /// Builds the OpenSearch selector for the free text search over the globally visible system template fields.
    /// </summary>
    public static Func<Selector<TDoc>, Selector<TDoc>> BuildGlobalTextSelector<TDoc>(string searchText, MetadataSearchScope scope)
        where TDoc : MetadataSearchItemBase
    {
        return s =>
        {
            ApplyScope(s, scope);

            s.Match(r => r.GlobalText, searchText);

            s.Limit(0, BaseIndexer<TDoc>.QueryLimit);

            return s;
        };
    }

    /// <summary>
    /// The SQL counterpart of <see cref="BuildSelector{TDoc}"/>: one identifier sub-query per filter condition.
    /// Used as a fallback when the metadata index is unavailable.
    /// </summary>
    /// <remarks>
    /// The conditions are returned separately so the caller applies them as independent predicates and they are
    /// combined with AND by the outer query — the same shape the inlined version had. Intersecting them into a
    /// single sub-query would also work on the supported servers, but it buys nothing and needs INTERSECT,
    /// which older self-hosted MySQL builds (before 8.0.31) do not have.
    /// </remarks>
    public static IEnumerable<IQueryable<int>> FilteredEntryIdsPerCondition(FilesDbContext filesDbContext, int tenantId, FileEntryType entryType, MetadataFilter metadataFilter)
    {
        if (metadataFilter is not { Conditions.Count: > 0 })
        {
            throw new ArgumentException(@"The metadata filter must contain at least one condition", nameof(metadataFilter));
        }

        return metadataFilter.Conditions.Select(condition => filesDbContext.MetadataValues
            .Where(v => v.TenantId == tenantId && v.EntryType == entryType)
            .Where(BuildConditionPredicate(condition))
            .Select(v => v.EntryId)
            .Distinct());
    }

    /// <summary>
    /// The SQL counterpart of <see cref="BuildGlobalTextSelector{TDoc}"/>: the identifiers of the entries whose
    /// system template string values contain the text. The text is expected to be already lowered.
    /// </summary>
    public static IQueryable<int> SystemTemplateTextEntryIds(FilesDbContext filesDbContext, int tenantId, FileEntryType entryType, string lowerText)
    {
        return filesDbContext.MetadataValues
            .Where(v => v.TenantId == tenantId && v.EntryType == entryType && v.ValueString.ToLower().Contains(lowerText) &&
                filesDbContext.MetadataFields.Any(f => f.TenantId == tenantId && f.Id == v.FieldId &&
                    filesDbContext.MetadataTemplates.Any(t => t.TenantId == tenantId && t.Id == f.TemplateId && t.IsSystem)))
            .Select(v => v.EntryId)
            .Distinct();
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
                return v => v.FieldId == fieldId && v.ValueString.ToLower() == stringValue;

            case MetadataFieldType.Date:
                var dateFrom = condition.DateFrom;
                var dateTo = condition.DateTo;
                return v => v.FieldId == fieldId &&
                    (dateFrom == null || v.ValueDate >= dateFrom) &&
                    (dateTo == null || v.ValueDate <= dateTo);

            case MetadataFieldType.Number:
                var numberFrom = condition.NumberFrom;
                var numberTo = condition.NumberTo;
                return v => v.FieldId == fieldId &&
                    (numberFrom == null || v.ValueNumber >= numberFrom) &&
                    (numberTo == null || v.ValueNumber <= numberTo);

            case MetadataFieldType.SingleChoice:
            case MetadataFieldType.MultiChoice:
                var optionIds = condition.OptionIds.Select(id => id.ToString()).ToList();
                return v => v.FieldId == fieldId && optionIds.Contains(v.OptionId);

            default:
                throw new ArgumentOutOfRangeException(nameof(condition), condition.FieldType, @"Unknown metadata field type");
        }
    }

    private static void ApplyScope<TDoc>(Selector<TDoc> s, MetadataSearchScope scope) where TDoc : MetadataSearchItemBase
    {
        switch (scope.Type)
        {
            case MetadataSearchScopeType.Parent:
                s.Where(r => r.ParentId, scope.ParentId);
                break;
            case MetadataSearchScopeType.Subtree:
                s.In(r => r.Folders.Select(a => a.ParentId), new[] { scope.ParentId });
                break;
            case MetadataSearchScopeType.None:
            default:
                break;
        }
    }
}
