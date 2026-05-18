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

#nullable enable

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace ASC.Files.Core.Text;

[Singleton]
public class JsonDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public async IAsyncEnumerable<string> ProcessAsync(
        Stream content,
        long contentLength,
        string fileExtension,
        ChunkerSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        JsonDocument? document = null;
        var parseFailed = false;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            parseFailed = true;
        }

        if (parseFailed)
        {
            foreach (var chunk in TextChunkingHelper.Split(text, settings))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return chunk;
            }

            yield break;
        }

        if (document == null)
        {
            throw new InvalidOperationException("JSON document parsing did not produce a result.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                yield return root.GetRawText();
                yield break;
            }

            var normalizedRoot = NormalizeComposite(root);
            foreach (var chunk in SplitJson(normalizedRoot, settings))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Serialize(chunk);
            }
        }
    }

    private static IEnumerable<JsonObject> SplitJson(JsonObject jsonData, ChunkerSettings settings)
    {
        var chunks = new List<JsonObject> { new() };
        var currentChunkSize = 0;
        SplitObject(jsonData, [], chunks, ref currentChunkSize, settings);

        foreach (var chunk in chunks.Where(chunk => chunk.Count > 0))
        {
            yield return chunk;
        }
    }

    private static void SplitObject(
        JsonObject data,
        List<string> currentPath,
        IList<JsonObject> chunks,
        ref int currentChunkSize,
        ChunkerSettings settings)
    {
        foreach (var property in data)
        {
            currentPath.Add(property.Key);
            var currentChunk = chunks[^1];

            SetPathValue(currentChunk, currentPath, property.Value);
            var candidateSize = GetChunkSize(currentChunk, settings);

            if (candidateSize <= settings.MaxTokensPerChunk)
            {
                currentChunkSize = candidateSize;
                currentPath.RemoveAt(currentPath.Count - 1);
                continue;
            }

            RemovePathValue(currentChunk, currentPath);

            if (property.Value is JsonObject { Count: > 0 } nestedObject)
            {
                if (currentChunk.Count > 0 && currentChunkSize >= GetMinChunkSize(settings))
                {
                    chunks.Add(new JsonObject());
                    currentChunkSize = 0;
                }

                SplitObject(nestedObject, currentPath, chunks, ref currentChunkSize, settings);
                currentPath.RemoveAt(currentPath.Count - 1);
                continue;
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(new JsonObject());
                currentChunk = chunks[^1];
            }

            SetPathValue(currentChunk, currentPath, property.Value);
            currentChunkSize = GetChunkSize(currentChunk, settings);

            if (currentChunkSize > settings.MaxTokensPerChunk)
            {
                chunks.Add(new JsonObject());
                currentChunkSize = 0;
            }

            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }

    private static JsonObject NormalizeComposite(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => NormalizeObject(element),
            JsonValueKind.Array => NormalizeArray(element),
            _ => throw new InvalidOperationException("Only JSON objects and arrays can be split structurally.")
        };
    }

    private static JsonObject NormalizeObject(JsonElement element)
    {
        var result = new JsonObject();

        foreach (var property in element.EnumerateObject())
        {
            result.Add(property.Name, NormalizeValue(property.Value));
        }

        return result;
    }

    private static JsonObject NormalizeArray(JsonElement element)
    {
        var result = new JsonObject();
        var index = 0;

        foreach (var item in element.EnumerateArray())
        {
            result.Add(index.ToString(CultureInfo.InvariantCulture), NormalizeValue(item));
            index++;
        }

        return result;
    }

    private static JsonNode? NormalizeValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => NormalizeObject(element),
            JsonValueKind.Array => NormalizeArray(element),
            JsonValueKind.Null => null,
            JsonValueKind.String => JsonValue.Create(element.GetString()!),
            JsonValueKind.True or JsonValueKind.False => JsonValue.Create(element.GetBoolean()),
            _ => JsonNode.Parse(element.GetRawText())
        };
    }

    private static void SetPathValue(JsonObject root, List<string> path, JsonNode? value)
    {
        if (path.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(path));
        }

        var current = root;
        for (var index = 0; index < path.Count - 1; index++)
        {
            if (current[path[index]] is not JsonObject child)
            {
                child = new JsonObject();
                current[path[index]] = child;
            }

            current = child;
        }

        current[path[^1]] = value?.DeepClone();
    }

    private static void RemovePathValue(JsonObject root, List<string> path)
    {
        if (path.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(path));
        }

        var ancestors = new JsonObject[path.Count];
        ancestors[0] = root;

        for (var index = 0; index < path.Count - 1; index++)
        {
            if (ancestors[index][path[index]] is not JsonObject child)
            {
                return;
            }

            ancestors[index + 1] = child;
        }

        ancestors[path.Count - 1].Remove(path[^1]);

        for (var index = path.Count - 2; index >= 0; index--)
        {
            if (ancestors[index + 1].Count == 0)
            {
                ancestors[index].Remove(path[index]);
            }
            else
            {
                break;
            }
        }
    }

    private static int GetChunkSize(JsonObject chunk, ChunkerSettings settings)
    {
        return chunk.Count == 0 ? 0 : settings.TokenCounter(Serialize(chunk));
    }

    private static int GetMinChunkSize(ChunkerSettings settings)
    {
        return Math.Max(settings.MaxTokensPerChunk - Math.Max(settings.MaxTokensPerChunk / 10, 1), 1);
    }

    private static string Serialize(JsonObject value)
    {
        return value.ToJsonString();
    }
}
