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
    public int? TemplateId { get; set; }
    public List<MetadataFilterCondition> Conditions { get; set; } = [];
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
/// The raw metadata filter condition as it comes from the query string JSON.
/// </summary>
public class MetadataFilterConditionRequest
{
    public int FieldId { get; set; }
    public string Op { get; set; }
    public string Value { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public List<Guid> OptionIds { get; set; }
}

[Scope]
public class MetadataFilterHelper(IDaoFactory daoFactory)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<MetadataFilter> ParseAsync(int? templateId, string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        List<MetadataFilterConditionRequest> requests;

        try
        {
            requests = JsonSerializer.Deserialize<List<MetadataFilterConditionRequest>>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            throw new ArgumentException(@"Invalid metadata filter format", nameof(json));
        }

        if (requests is not { Count: > 0 })
        {
            return null;
        }

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var fields = await metadataDao.GetFieldsAsync(requests.Select(r => r.FieldId).Distinct())
            .ToDictionaryAsync(f => f.Id);

        var filter = new MetadataFilter { TemplateId = templateId };

        foreach (var request in requests)
        {
            if (!fields.TryGetValue(request.FieldId, out var field))
            {
                throw new ArgumentException($@"Unknown metadata field {request.FieldId}", nameof(json));
            }

            if (templateId.HasValue && field.TemplateId != templateId.Value)
            {
                throw new ArgumentException($@"The field {request.FieldId} does not belong to the template {templateId}", nameof(json));
            }

            filter.Conditions.Add(ToCondition(request, field));
        }

        return filter;
    }

    private static MetadataFilterCondition ToCondition(MetadataFilterConditionRequest request, MetadataField field)
    {
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
                condition.DateFrom = ParseDate(request.From, field);
                condition.DateTo = ParseDate(request.To, field);

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

    private static DateTime? ParseDate(string value, MetadataField field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
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
