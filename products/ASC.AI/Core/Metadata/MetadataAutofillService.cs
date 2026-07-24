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

namespace ASC.AI.Core.MetadataAutofill;

/// <summary>
/// The metadata field proposed by AI for the globally visible system template.
/// </summary>
public record MetadataFieldSuggestion(string Title, MetadataFieldType Type, List<string> Options, string Value);

[Scope]
public class MetadataAutofillService(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    MetadataService metadataService,
    AiProviderService aiProviderService,
    ChatClientFactory chatClientFactory,
    AuthContext authContext,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    ILogger<MetadataAutofillService> logger)
{
    private const int MaxContentChars = 100_000;

    public async Task<List<MetadataValue>> AutofillAsync(int fileId, int? templateId, bool overwrite, bool dryRun)
    {
        var file = await GetFileForReadAsync(fileId);

        var fields = await ResolveFieldsAsync(fileId, templateId);
        if (fields.Count == 0)
        {
            return [];
        }

        var content = await ExtractContentAsync(file);
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        var responseText = await CompleteAsync(MetadataAutofillPrompt.BuildAutofillInstruction(fields), content);

        var values = MetadataAutofillPrompt.ParseAutofillResponse(responseText, fields, logger);
        if (values.Count == 0)
        {
            return [];
        }

        if (!overwrite)
        {
            var existing = await daoFactory.GetMetadataDao<int>()
                .GetValuesAsync(fileId, FileEntryType.File, values.Select(v => v.FieldId))
                .ToListAsync();

            var filledFieldIds = existing.Where(e => !e.IsEmpty).Select(e => e.FieldId).ToHashSet();

            values = values.Where(v => !filledFieldIds.Contains(v.FieldId)).ToList();
        }

        if (values.Count == 0 || dryRun)
        {
            return values;
        }

        return await metadataService.SetValuesAsync(fileId, FileEntryType.File, values);
    }

    public async Task<List<MetadataFieldSuggestion>> SuggestFieldsAsync(int fileId)
    {
        var file = await GetFileForReadAsync(fileId);

        var content = await ExtractContentAsync(file);
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        var systemTemplate = await metadataService.GetOrCreateSystemTemplateAsync();

        var existingNames = systemTemplate.Fields.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var responseText = await CompleteAsync(MetadataAutofillPrompt.BuildSuggestInstruction(existingNames), content);

        return MetadataAutofillPrompt.ParseSuggestResponse(responseText, existingNames, logger);
    }

    private async Task<File<int>> GetFileForReadAsync(int fileId)
    {
        var file = await daoFactory.GetFileDao<int>().GetFileAsync(fileId) ?? throw new ItemNotFoundException();

        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        return file;
    }

    private async Task<List<MetadataField>> ResolveFieldsAsync(int fileId, int? templateId)
    {
        var metadataDao = daoFactory.GetMetadataDao<int>();

        var templateIds = new List<int>();

        if (templateId.HasValue)
        {
            templateIds.Add(templateId.Value);
        }
        else
        {
            templateIds.AddRange(await metadataDao.GetLinksAsync(fileId, FileEntryType.File).Select(l => l.TemplateId).ToListAsync());

            var systemTemplate = await metadataDao.GetSystemTemplateAsync(withFields: false);
            if (systemTemplate != null)
            {
                templateIds.Add(systemTemplate.Id);
            }
        }

        var fields = new List<MetadataField>();

        foreach (var id in templateIds.Distinct())
        {
            fields.AddRange(await metadataDao.GetFieldsAsync(id).ToListAsync());
        }

        return fields;
    }

    private async Task<string> ExtractContentAsync(File<int> file)
    {
        if (!vectorizationGlobalSettings.IsSupportedContentExtraction(file.Title) ||
            file.ContentLength > vectorizationGlobalSettings.MaxContentLength)
        {
            throw new ArgumentException(@"The file content cannot be extracted", nameof(file));
        }

        await using var stream = await daoFactory.GetFileDao<int>().GetFileStreamAsync(file);

        var content = await textExtractor.ExtractAsync(stream, file.ContentLength);

        if (content is { Length: > MaxContentChars })
        {
            content = content[..MaxContentChars];
        }

        return content;
    }

    private async Task<string> CompleteAsync(string instruction, string content)
    {
        var defaultProvider = await aiProviderService.GetDefaultProviderAsync()
            ?? throw new InvalidOperationException(ErrorMessages.IncorrectProvider);

        var (provider, modelSettings) = await aiProviderService.GetProviderContextAsync(defaultProvider.ProviderId, defaultProvider.DefaultModel);

        if (!modelSettings.IsEnabled)
        {
            throw new ArgumentException(ErrorMessages.ModelDisabled);
        }

        var options = new ChatClientOptions
        {
            Provider = provider.Type,
            ProviderId = provider.Id,
            HasModelSettings = provider.HasModelSettings,
            Endpoint = provider.Url,
            Key = provider.Key,
            ModelId = modelSettings.Id
        };

        var client = chatClientFactory.Create(options, authContext.CurrentAccount.ID);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, instruction),
            new(ChatRole.User, content)
        };

        var response = await client.GetResponseAsync(messages);

        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(@"The model returned an empty response");
        }

        return TextContentUtils.CutThink(text);
    }
}

public static class MetadataAutofillPrompt
{
    public static string BuildAutofillInstruction(List<MetadataField> fields)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You extract metadata from a document.");
        builder.AppendLine("Respond with ONLY a single JSON object, no markdown, no explanations.");
        builder.AppendLine("Include only the fields whose values are clearly supported by the document text; omit anything uncertain.");
        builder.AppendLine("Dates must be in ISO 8601 format (YYYY-MM-DD). Numbers must be integers.");
        builder.AppendLine("For choice fields use only the allowed values exactly as listed.");
        builder.AppendLine();
        builder.AppendLine("The fields:");

        foreach (var field in fields)
        {
            var line = field.Type switch
            {
                MetadataFieldType.String => $"- \"field_{field.Id}\": \"{field.Name}\" - a string",
                MetadataFieldType.Date => $"- \"field_{field.Id}\": \"{field.Name}\" - a date string in ISO 8601 (YYYY-MM-DD)",
                MetadataFieldType.Number => $"- \"field_{field.Id}\": \"{field.Name}\" - an integer",
                MetadataFieldType.SingleChoice => $"- \"field_{field.Id}\": \"{field.Name}\" - one of: {FormatOptions(field)}",
                MetadataFieldType.MultiChoice => $"- \"field_{field.Id}\": \"{field.Name}\" - an array of one or more of: {FormatOptions(field)}",
                _ => null
            };

            if (line != null)
            {
                builder.AppendLine(line);
            }
        }

        builder.AppendLine();
        builder.AppendLine("""Example response: {"field_1":"ACME Corp","field_2":"2026-01-15","field_3":42,"field_4":"Signed","field_5":["NDA","SLA"]}""");

        return builder.ToString();
    }

    public static string BuildSuggestInstruction(IReadOnlyCollection<string> existingNames)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You propose new metadata fields for a document based on its content.");
        builder.AppendLine("Respond with ONLY a single JSON array, no markdown, no explanations.");
        builder.AppendLine("Each element: {\"title\": string, \"type\": one of \"string\"|\"date\"|\"number\"|\"singleChoice\"|\"multiChoice\", \"options\": array of strings (choice types only), \"value\": string with the value extracted from the document (optional)}.");
        builder.AppendLine("Propose at most 10 concise, generally useful fields. Titles must be short noun phrases.");

        if (existingNames.Count > 0)
        {
            builder.AppendLine($"Do not propose fields with these existing titles: {string.Join(", ", existingNames.Select(n => $"\"{n}\""))}.");
        }

        return builder.ToString();
    }

    public static List<MetadataValue> ParseAutofillResponse(string responseText, List<MetadataField> fields, ILogger logger)
    {
        var json = CutJson(responseText, '{', '}');
        if (json == null)
        {
            return [];
        }

        var values = new List<MetadataValue>();

        try
        {
            using var document = JsonDocument.Parse(json);

            var fieldsById = fields.ToDictionary(f => f.Id);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.StartsWith("field_", StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(property.Name["field_".Length..], out var fieldId) ||
                    !fieldsById.TryGetValue(fieldId, out var field))
                {
                    continue;
                }

                var value = ToValue(field, property.Value);
                if (value != null)
                {
                    values.Add(value);
                }
            }
        }
        catch (JsonException e)
        {
            logger.WarningMetadataAutofillParse(e);
        }

        return values;
    }

    public static List<MetadataFieldSuggestion> ParseSuggestResponse(string responseText, IReadOnlyCollection<string> existingNames, ILogger logger)
    {
        var json = CutJson(responseText, '[', ']');
        if (json == null)
        {
            return [];
        }

        var suggestions = new List<MetadataFieldSuggestion>();

        try
        {
            using var document = JsonDocument.Parse(json);

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty("title", out var titleElement) ||
                    titleElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var title = titleElement.GetString();
                if (string.IsNullOrWhiteSpace(title) || existingNames.Contains(title))
                {
                    continue;
                }

                var type = MetadataFieldType.String;
                if (element.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String &&
                    Enum.TryParse<MetadataFieldType>(typeElement.GetString(), true, out var parsedType))
                {
                    type = parsedType;
                }

                List<string> options = null;
                if (element.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    options = optionsElement.EnumerateArray()
                        .Where(o => o.ValueKind == JsonValueKind.String)
                        .Select(o => o.GetString())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();
                }

                string value = null;
                if (element.TryGetProperty("value", out var valueElement))
                {
                    value = valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() : valueElement.ToString();
                }

                suggestions.Add(new MetadataFieldSuggestion(title.Trim(), type, options, value));
            }
        }
        catch (JsonException e)
        {
            logger.WarningMetadataAutofillParse(e);
        }

        return suggestions;
    }

    private static MetadataValue ToValue(MetadataField field, JsonElement element)
    {
        switch (field.Type)
        {
            case MetadataFieldType.String:
                var stringValue = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();

                return string.IsNullOrWhiteSpace(stringValue) ? null : new MetadataValue { FieldId = field.Id, StringValue = stringValue };

            case MetadataFieldType.Date:
                if (element.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                {
                    return new MetadataValue { FieldId = field.Id, DateValue = date };
                }

                return null;

            case MetadataFieldType.Number:
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
                {
                    return new MetadataValue { FieldId = field.Id, NumberValue = number };
                }

                if (element.ValueKind == JsonValueKind.String &&
                    long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNumber))
                {
                    return new MetadataValue { FieldId = field.Id, NumberValue = parsedNumber };
                }

                return null;

            case MetadataFieldType.SingleChoice:
            case MetadataFieldType.MultiChoice:
                var labels = element.ValueKind switch
                {
                    JsonValueKind.String => [element.GetString()],
                    JsonValueKind.Array => element.EnumerateArray()
                        .Where(o => o.ValueKind == JsonValueKind.String)
                        .Select(o => o.GetString())
                        .ToList(),
                    _ => new List<string>()
                };

                var optionIds = labels
                    .Select(label => (field.Options ?? []).FirstOrDefault(o => o.Value.Equals(label, StringComparison.OrdinalIgnoreCase)))
                    .Where(o => o != null)
                    .Select(o => o.Id)
                    .Distinct()
                    .ToList();

                if (optionIds.Count == 0)
                {
                    return null;
                }

                if (field.Type == MetadataFieldType.SingleChoice && optionIds.Count > 1)
                {
                    optionIds = [optionIds[0]];
                }

                return new MetadataValue { FieldId = field.Id, OptionIds = optionIds };

            default:
                return null;
        }
    }

    private static string FormatOptions(MetadataField field)
    {
        return string.Join(", ", (field.Options ?? []).Select(o => $"\"{o.Value}\""));
    }

    private static string CutJson(string text, char open, char close)
    {
        var start = text.IndexOf(open);
        var end = text.LastIndexOf(close);

        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }
}

public static partial class MetadataAutofillLogger
{
    [LoggerMessage(LogLevel.Warning, "Failed to parse the metadata autofill model response")]
    public static partial void WarningMetadataAutofillParse(this ILogger logger, Exception exception);
}
