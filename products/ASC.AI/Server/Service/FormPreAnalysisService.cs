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

namespace ASC.AI.Service;

/// <summary>A starter question about a form's submissions, and the request the chat gets when it is picked.</summary>
public sealed record FormQuestionSuggestion
{
    public required string Question { get; init; }
    public required string Prompt { get; init; }
}

/// <summary>
/// Asks a model for a few starter questions about a form's submissions, in the current user's language.
/// A convenience only: every failure degrades to an empty list.
/// </summary>
[Scope]
public class FormPreAnalysisService(
    AiGateway gateway,
    AiConfiguration aiConfiguration,
    AssignmentsStorage assignmentsStorage,
    AssignmentsResolver assignmentsResolver,
    ProfileStorage profileStorage,
    OpenAiChatCompletionClient chatClient,
    FormSchemaProvider formSchemaProvider,
    TenantManager tenantManager,
    IFusionCache fusionCache,
    ILogger<FormPreAnalysisService> logger)
{
    private const int MinQuestions = 3;
    private const int MaxQuestions = 4;

    // Rejection thresholds, deliberately well above what the prompt asks for: an answer past them
    // ignored the format, and generation time scales with what we ask to be written.
    private const int MaxQuestionLength = 120;
    private const int MaxPromptLength = 320;
    private const int MaxPromptColumns = 60;
    private const int MaxEnumValuesPerColumn = 10;

    private const string ThinkEndTag = "</think>";

    private const string SystemPrompt =
        """
        You generate starter analytics questions for a form-submission dataset.
        Reply with a JSON array only. No prose, no markdown, no code fences, no reasoning.
        """;

    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(12);

    // A model that was called and failed is expensive to retry; a portal with no model configured yet
    // is a couple of cheap queries away from being ready and must not stay dark after an admin fixes it.
    private static readonly TimeSpan _retryAfterFailure = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _retryWhenNotReady = TimeSpan.FromMinutes(1);

    // Bounds the call itself, which outlives the attach that started it.
    private static readonly TimeSpan _modelBudget = TimeSpan.FromSeconds(60);

    // Shared and cached under many keys. Never mutated.
    private static readonly List<FormQuestionSuggestion> _noSuggestions = [];

    // Every other provider the chat supports (openrouter, groq, ollama, openaicompatible, ...) is a thin
    // wrapper over the OpenAI wire format; only these three carry a transport of their own.
    private static readonly FrozenSet<string> _nonOpenAiProviders =
        new[] { "anthropic", "genai", "stabilityai" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Never throws. Waits at most <paramref name="wait"/>; past that the caller gets an empty list while
    /// the call finishes in the background and fills the cache, making the next attach instant.
    /// </summary>
    public async Task<IReadOnlyList<FormQuestionSuggestion>> GenerateAsync(File<int> file, TimeSpan wait)
    {
        var culture = CultureInfo.CurrentUICulture;
        var cacheKey = GetCacheKey(tenantManager.GetCurrentTenantId(), file, culture);

        try
        {
            // First, so that a hit costs one cache read and no profile, row-count or metadata lookup.
            var cached = await fusionCache.TryGetAsync<List<FormQuestionSuggestion>>(cacheKey);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            // All scope-bound work happens here: the factory can outlive the attach, and by then the
            // request scope and every service in it are gone.
            var request = await PrepareRequestAsync(file, culture);
            if (request is null)
            {
                // No model configured, or nothing to describe yet.
                await fusionCache.SetAsync(cacheKey, _noSuggestions, opt => opt.SetDuration(_retryWhenNotReady));
                return [];
            }

            return await fusionCache.GetOrSetAsync<List<FormQuestionSuggestion>>(
                cacheKey,
                async (_, token) => await CallModelAsync(request.Value, file.Id, token),
                _noSuggestions,
                opt => opt
                    .SetDuration(_cacheDuration)
                    .SetFailSafe(true, _cacheDuration, _retryAfterFailure)
                    .SetFactoryTimeouts(hardTimeout: wait));
        }
        catch (Exception e)
        {
            logger.WarnFormPreAnalysisFailed(e, file.Id);
            return [];
        }
    }

    private async Task<(ChatEndpoint Endpoint, ChatCompletionMessage[] Messages)?> PrepareRequestAsync(File<int> file, CultureInfo culture)
    {
        var endpoint = await ResolveEndpointAsync();
        if (endpoint is null)
        {
            return null;
        }

        var schema = await formSchemaProvider.TryReadAsync(file);
        if (schema is null || schema.RowCount == 0 || schema.Columns.Count == 0)
        {
            return null;
        }

        ChatCompletionMessage[] messages =
        [
            new("system", SystemPrompt),
            new("user", BuildUserPrompt(file, schema, culture))
        ];

        return (endpoint, messages);
    }

    private async Task<List<FormQuestionSuggestion>> CallModelAsync(
        (ChatEndpoint Endpoint, ChatCompletionMessage[] Messages) request,
        int fileId,
        CancellationToken cancellationToken)
    {
        // Outliving the caller's wait is the point, so the call carries its own bound, not the attach's.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_modelBudget);

        var endpoint = request.Endpoint;
        var startedAt = Stopwatch.GetTimestamp();

        var content = await chatClient.CompleteAsync(endpoint.BaseUrl, endpoint.ApiKey, endpoint.ModelId, request.Messages, cts.Token);
        var suggestions = Parse(content);

        logger.DebugFormPreAnalysisCompleted(fileId, suggestions.Count, (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        if (suggestions.Count == 0)
        {
            // Thrown, not returned: fail-safe hands back the empty default and holds off the next
            // attempt, instead of caching nothing for twelve hours.
            logger.WarnFormPreAnalysisUnusableAnswer(fileId);
            throw new InvalidOperationException("The model returned no usable questions.");
        }

        return suggestions;
    }

    /// <summary>
    /// The form version makes explicit invalidation unnecessary. The model is deliberately absent: it
    /// only affects phrasing, and keying on it would force a profile lookup on every attach.
    /// </summary>
    private static string GetCacheKey(int tenantId, File<int> file, CultureInfo culture)
    {
        return $"ai:form:preanalysis:{tenantId}:{file.Id}:{file.Version}:{culture.Name}";
    }

    private async Task<ChatEndpoint?> ResolveEndpointAsync()
    {
        if (gateway.Configured)
        {
            var model = aiConfiguration.RecommendedModelForForms;
            if (string.IsNullOrEmpty(model))
            {
                logger.WarnRecommendedModelNotConfigured();
                return null;
            }

            var key = await gateway.GetKeyAsync(allowEmpty: true);
            if (string.IsNullOrEmpty(key))
            {
                logger.DebugGatewayKeyUnavailable();
                return null;
            }

            return new ChatEndpoint(gateway.Url, key, model);
        }

        var profile = await ResolveProfileAsync();
        if (profile is null)
        {
            return null;
        }

        if (_nonOpenAiProviders.Contains(profile.ProviderType))
        {
            logger.DebugProviderNotSupported(profile.ProviderType);
            return null;
        }

        return new ChatEndpoint(profile.BaseUrl, profile.Key, profile.ModelId);
    }

    private async Task<Profile?> ResolveProfileAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        // Answered by the chat, so they run on the chat's own profile.
        var profileId = await ResolveProfileIdAsync(tenantId, ActionType.Chat)
                        ?? await ResolveProfileIdAsync(tenantId, ActionType.Default);

        if (profileId is null)
        {
            logger.DebugNoProfileAssigned();
            return null;
        }

        var profile = await profileStorage.ReadByIdAsync(tenantId, profileId.Value);
        if (profile is null)
        {
            logger.DebugProfileNotFound(profileId.Value);
        }

        return profile;
    }

    private async Task<Guid?> ResolveProfileIdAsync(int tenantId, ActionType actionType)
    {
        var stored = await assignmentsStorage.ReadByTypeAsync(tenantId, actionType);

        return await assignmentsResolver.ResolveByTypeAsync(actionType, stored, applyDefaults: false);
    }

    private static string BuildUserPrompt(File<int> file, FormSchema schema, CultureInfo culture)
    {
        // A form with hundreds of fields would blow both the context and the time budget.
        var columnList = FormSchemaFormatter.FormatColumnList(schema.Columns.Take(MaxPromptColumns), MaxEnumValuesPerColumn);

        return $$"""
                Form: "{{file.Title}}" — {{schema.RowCount}} submissions collected.
                Columns (name "label" (type) [allowed values]):
                {{columnList}}

                Write {{MaxQuestions}} starter questions an analyst would ask about these submissions.
                - Every question must be answerable from the columns above and must name at least one real column label.
                - Prefer distributions, counts, comparisons and time trends over single-record lookups.
                - Never invent a column or a value, and never repeat a question.
                - "question": at most 60 characters, phrased as a button label, no trailing period.
                - "prompt": one sentence, the full request sent to the analysis assistant, naming the exact columns it needs.
                - Write both fields in {{culture.EnglishName}} ({{culture.Name}}).
                Reply with exactly this shape: [{"question":"...","prompt":"..."}]
                """;
    }

    private static List<FormQuestionSuggestion> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var json = ExtractJsonArray(content);
        if (json is null)
        {
            return [];
        }

        var raw = JsonSerializer.Deserialize<List<RawSuggestion>>(json, _jsonOptions);
        if (raw is null)
        {
            return [];
        }

        var result = new List<FormQuestionSuggestion>(MaxQuestions);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in raw)
        {
            var question = item.Question?.Trim();
            var prompt = item.Prompt?.Trim();

            if (string.IsNullOrEmpty(question) || string.IsNullOrEmpty(prompt))
            {
                continue;
            }

            // Over-long means the format was ignored; truncating would leave half a sentence.
            if (question.Length > MaxQuestionLength || prompt.Length > MaxPromptLength)
            {
                continue;
            }

            // The prompt degenerated into an echo of the question.
            if (prompt.Length < question.Length)
            {
                continue;
            }

            if (!seen.Add(question))
            {
                continue;
            }

            result.Add(new FormQuestionSuggestion { Question = question, Prompt = prompt });

            if (result.Count == MaxQuestions)
            {
                break;
            }
        }

        // One or two chips read as a broken feature.
        return result.Count >= MinQuestions ? result : [];
    }

    private static string? ExtractJsonArray(string content)
    {
        var span = content.AsSpan();

        // Hybrid-reasoning models prefix the answer with a think block.
        var thinkEnd = content.IndexOf(ThinkEndTag, StringComparison.OrdinalIgnoreCase);
        if (thinkEnd >= 0)
        {
            span = span[(thinkEnd + ThinkEndTag.Length)..];
        }

        // Slicing between the outer brackets also drops any markdown fence.
        var start = span.IndexOf('[');
        var end = span.LastIndexOf(']');

        return start >= 0 && end > start ? span[start..(end + 1)].ToString() : null;
    }

    private sealed record ChatEndpoint(string BaseUrl, string? ApiKey, string ModelId);

    private sealed record RawSuggestion(string? Question, string? Prompt);
}

internal static partial class FormPreAnalysisServiceLogger
{
    [LoggerMessage(LogLevel.Warning, "Form pre-analysis failed for file {FileId}")]
    public static partial void WarnFormPreAnalysisFailed(this ILogger<FormPreAnalysisService> logger, Exception exception, int fileId);

    [LoggerMessage(LogLevel.Debug, "Form pre-analysis for file {FileId} produced {Count} questions in {ElapsedMs} ms")]
    public static partial void DebugFormPreAnalysisCompleted(this ILogger<FormPreAnalysisService> logger, int fileId, int count, long elapsedMs);

    [LoggerMessage(LogLevel.Warning, "Form pre-analysis for file {FileId} returned no usable questions")]
    public static partial void WarnFormPreAnalysisUnusableAnswer(this ILogger<FormPreAnalysisService> logger, int fileId);

    [LoggerMessage(LogLevel.Warning, "Form pre-analysis skipped: ai:recommendedModelForForms is not configured")]
    public static partial void WarnRecommendedModelNotConfigured(this ILogger<FormPreAnalysisService> logger);

    [LoggerMessage(LogLevel.Debug, "Form pre-analysis skipped: the ai gateway issued no key")]
    public static partial void DebugGatewayKeyUnavailable(this ILogger<FormPreAnalysisService> logger);

    [LoggerMessage(LogLevel.Debug, "Form pre-analysis skipped: no profile is assigned to the chat or default action")]
    public static partial void DebugNoProfileAssigned(this ILogger<FormPreAnalysisService> logger);

    [LoggerMessage(LogLevel.Debug, "Form pre-analysis skipped: profile {ProfileId} not found")]
    public static partial void DebugProfileNotFound(this ILogger<FormPreAnalysisService> logger, Guid profileId);

    [LoggerMessage(LogLevel.Debug, "Form pre-analysis skipped: provider {ProviderType} is not OpenAI-compatible")]
    public static partial void DebugProviderNotSupported(this ILogger<FormPreAnalysisService> logger, string providerType);
}
