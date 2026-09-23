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

namespace ASC.Files.Core;

/// <summary>
/// The structured metadata filter applied to the entry listing.
/// </summary>
public class MetadataFilter
{
    /// <summary>
    /// The template the entries must be assigned to. On its own it narrows the listing to the entries carrying the
    /// template; together with <see cref="Conditions"/> it also pins the fields the conditions may name.
    /// </summary>
    public int? TemplateId { get; set; }

    public List<MetadataFilterCondition> Conditions { get; set; } = [];

    /// <summary>
    /// <c>true</c> when the filter narrows nothing, so the callers treat it the same as a missing one.
    /// </summary>
    public bool IsEmpty => TemplateId == null && Conditions.Count == 0;
}

/// <summary>
/// The single validated metadata filter condition. All conditions are combined with AND.
/// </summary>
public class MetadataFilterCondition
{
    public int FieldId { get; set; }
    public MetadataFieldType FieldType { get; set; }
    public string StringValue { get; set; }
    public long? NumberFrom { get; set; }
    public long? NumberTo { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<Guid> OptionIds { get; set; }
}

/// <summary>
/// One metadata filter condition as the clients send it: an element of the "metadataFilters" JSON of the listings and of
/// the body of the search endpoints. All conditions are combined with AND.
/// </summary>
public class MetadataFilterConditionRequest
{
    /// <summary>
    /// The ID of the template field the condition is on. A custom field is addressed by <see cref="Name"/> instead.
    /// </summary>
    /// <example>1</example>
    public int FieldId { get; set; }

    /// <summary>
    /// The name of a custom field, for the conditions on the custom fields, which have no identifier outside. Either the
    /// field ID or the name is given; the name is matched without regard to case.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The operator, one of <see cref="MetadataFilterOperators"/>. Optional: the field type alone determines how the
    /// condition is evaluated, so an omitted operator is accepted, while a present one has to match the field type.
    /// </summary>
    /// <example>eq</example>
    public string Op { get; set; }

    /// <summary>
    /// The exact value: string fields, and number fields given a single value.
    /// </summary>
    /// <example>ACME</example>
    public string Value { get; set; }

    /// <summary>
    /// The inclusive lower bound of a range. A date given without a time ("2026-06-01") is the start of that day (UTC).
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// The inclusive upper bound of a range. A date given without a time ("2026-06-30") covers the whole day (UTC);
    /// a value with a time is an instant and is taken as is.
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// The options any of which the choice field must hold.
    /// </summary>
    public List<Guid> OptionIds { get; set; }
}

/// <summary>
/// The operators a metadata filter condition may name (see the <c>metadataFilters</c> query parameter).
/// </summary>
public static class MetadataFilterOperators
{
    /// <summary>Exact match: string fields, and number fields given a single <c>value</c>.</summary>
    public const string Equal = "eq";

    /// <summary>Inclusive range with an optional side: number and date fields.</summary>
    public const string Range = "range";

    /// <summary>Any of the requested options: choice fields.</summary>
    public const string In = "in";
}

[Scope]
public class MetadataFilterHelper(IDaoFactory daoFactory)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds the filter from the query string parameters. A template without conditions is a filter of its own: the
    /// listing is narrowed to the entries the template is assigned to. It used to be read for validation only, so such a
    /// request came back unfiltered with a 200.
    /// </summary>
    public Task<MetadataFilter> ParseAsync(int? templateId, string json)
    {
        return ParseAsync(templateId, ParseConditions(json));
    }

    /// <summary>
    /// Builds the filter from typed conditions, the form the search endpoints take in their body. The query string
    /// variant is parsed into the same conditions first, so both roads validate and evaluate identically.
    /// </summary>
    public async Task<MetadataFilter> ParseAsync(int? templateId, IEnumerable<MetadataFilterConditionRequest> conditions)
    {
        var metadataDao = daoFactory.GetMetadataDao<int>();

        // the system template is not exposed, so for the filter it does not exist either: the custom fields are
        // filtered by their field ids like any other field
        if (templateId.HasValue && await metadataDao.GetTemplateAsync(templateId.Value, withFields: false) is null or { IsSystem: true })
        {
            throw new ArgumentException($@"Unknown metadata template {templateId}", nameof(templateId));
        }

        var requests = conditions?.ToList() ?? [];

        if (requests.Contains(null))
        {
            throw new ArgumentException(@"Invalid metadata filter format", nameof(conditions));
        }

        if (requests.Count == 0)
        {
            return templateId.HasValue ? new MetadataFilter { TemplateId = templateId } : null;
        }

        await ResolveCustomFieldNamesAsync(metadataDao, requests);

        var fields = await metadataDao.GetFieldsAsync(requests.Select(r => r.FieldId).Distinct())
            .ToDictionaryAsync(f => f.Id);

        var filter = new MetadataFilter { TemplateId = templateId };

        foreach (var request in requests)
        {
            if (!fields.TryGetValue(request.FieldId, out var field))
            {
                throw new ArgumentException($@"Unknown metadata field {request.FieldId}", nameof(conditions));
            }

            if (templateId.HasValue && field.TemplateId != templateId.Value)
            {
                throw new ArgumentException($@"The field {request.FieldId} does not belong to the template {templateId}", nameof(conditions));
            }

            filter.Conditions.Add(ToCondition(request, field));
        }

        return filter;
    }

    /// <summary>
    /// The custom fields are addressed by name, so a condition naming one gets the identifier of the matching field of
    /// the system template. A name no entry holds a value for is unknown: such a field has been dropped.
    /// </summary>
    private static async Task ResolveCustomFieldNamesAsync(IMetadataDao<int> metadataDao, List<MetadataFilterConditionRequest> requests)
    {
        var named = requests.Where(r => r.FieldId == 0).ToList();

        if (named.Count == 0)
        {
            return;
        }

        var systemTemplate = await metadataDao.GetSystemTemplateAsync();

        foreach (var request in named)
        {
            var name = request.Name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(@"A metadata filter condition requires a fieldId or a name", nameof(requests));
            }

            var field = systemTemplate?.Fields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($@"Unknown custom field '{name}'", nameof(requests));

            request.FieldId = field.Id;
        }
    }

    private static List<MetadataFilterConditionRequest> ParseConditions(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<MetadataFilterConditionRequest>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            throw new ArgumentException(@"Invalid metadata filter format", nameof(json));
        }
    }

    private static MetadataFilterCondition ToCondition(MetadataFilterConditionRequest request, MetadataField field)
    {
        ValidateOperator(request, field);

        var condition = new MetadataFilterCondition { FieldId = field.Id, FieldType = field.Type };

        switch (field.Type)
        {
            case MetadataFieldType.String:
                if (string.IsNullOrEmpty(request.Value))
                {
                    throw new ArgumentException($@"The condition for the field '{field.Name}' requires a value");
                }

                condition.StringValue = request.Value.ToLowerInvariant();
                break;

            case MetadataFieldType.Date:
                condition.DateFrom = ParseDate(request.From, field, endOfDay: false);
                condition.DateTo = ParseDate(request.To, field, endOfDay: true);

                if (condition.DateFrom == null && condition.DateTo == null)
                {
                    throw new ArgumentException($@"The condition for the field '{field.Name}' requires a date range");
                }

                break;

            case MetadataFieldType.Number:
                condition.NumberFrom = ParseNumber(request.From ?? request.Value, field);
                condition.NumberTo = ParseNumber(request.To ?? request.Value, field);

                if (condition.NumberFrom == null && condition.NumberTo == null)
                {
                    throw new ArgumentException($@"The condition for the field '{field.Name}' requires a number range");
                }

                break;

            case MetadataFieldType.SingleChoice:
            case MetadataFieldType.MultiChoice:
                if (request.OptionIds is not { Count: > 0 })
                {
                    throw new ArgumentException($@"The condition for the field '{field.Name}' requires option identifiers");
                }

                var knownOptionIds = (field.Options ?? []).Select(o => o.Id).ToHashSet();
                if (request.OptionIds.Any(id => !knownOptionIds.Contains(id)))
                {
                    throw new ArgumentException($@"The field '{field.Name}' does not contain the specified option");
                }

                condition.OptionIds = request.OptionIds;
                break;
        }

        return condition;
    }

    /// <summary>
    /// The operator used to be read by nobody, so a mistyped or unsupported one ("contains", "gt") was silently evaluated
    /// as the field type dictates and returned a confusing result. Now it has to be one the field type supports.
    /// </summary>
    private static void ValidateOperator(MetadataFilterConditionRequest request, MetadataField field)
    {
        if (string.IsNullOrEmpty(request.Op))
        {
            return;
        }

        var supported = field.Type switch
        {
            MetadataFieldType.String => request.Op is MetadataFilterOperators.Equal,
            MetadataFieldType.Number => request.Op is MetadataFilterOperators.Equal or MetadataFilterOperators.Range,
            MetadataFieldType.Date => request.Op is MetadataFilterOperators.Range,
            MetadataFieldType.SingleChoice or MetadataFieldType.MultiChoice => request.Op is MetadataFilterOperators.In,
            _ => false
        };

        if (!supported)
        {
            throw new ArgumentException($@"The operator '{request.Op}' is not supported for the field '{field.Name}'");
        }
    }

    /// <summary>
    /// A bound given as a date only names the whole day: as the upper bound it is moved to the last second of that day,
    /// otherwise every value stored later than midnight fell out of the "inclusive" range the UI sends as two dates.
    /// The last second, not the last tick: the values live in a datetime column without fractional seconds, so a
    /// finer bound would only depend on how the server rounds it. A bound carrying a time is an instant and is taken as is.
    /// </summary>
    private static DateTime? ParseDate(string value, MetadataField field, bool endOfDay)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var day))
        {
            return endOfDay ? day.AddDays(1).AddSeconds(-1) : day;
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
        {
            throw new ArgumentException($@"Invalid date value in the condition for the field '{field.Name}'");
        }

        return result;
    }

    private static long? ParseNumber(string value, MetadataField field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($@"Invalid number value in the condition for the field '{field.Name}'");
        }

        return result;
    }
}
