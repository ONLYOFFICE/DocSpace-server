// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

#pragma warning disable SKEXP0050

using Microsoft.SemanticKernel.Text;
using System.Runtime.CompilerServices;

namespace ASC.Files.Core.Text;

[Singleton]
public class MarkdownDocumentProcessingStrategy(DocumentTextExtractor documentTextExtractor) : IDocumentProcessingStrategy
{
    public async IAsyncEnumerable<string> ProcessAsync(
        Stream content,
        long contentLength,
        string fileExtension,
        ChunkerSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = await documentTextExtractor.ExtractAsync(content, contentLength, fileExtension);

        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        foreach (var chunk in SplitMarkdown(text, settings))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }

    private static IEnumerable<string> SplitMarkdown(string text, ChunkerSettings settings)
    {
        var overlapInTokens = GetOverlapInTokens(settings);

        TextChunker.TokenCounter tokenCounter = input => settings.TokenCounter(input);

        foreach (var section in SplitMarkdownSections(text))
        {
            if (settings.TokenCounter(section) <= settings.MaxTokensPerChunk)
            {
                yield return section;
                continue;
            }

            if (TryExtractHeading(section, out var headingLine, out var body) &&
                !string.IsNullOrWhiteSpace(body) &&
                settings.TokenCounter(headingLine) < settings.MaxTokensPerChunk)
            {
                var bodyLines = TextChunker.SplitMarkDownLines(body, settings.MaxTokensPerChunk, tokenCounter);
                var bodyChunks = TextChunker.SplitPlainTextParagraphs(
                    bodyLines,
                    settings.MaxTokensPerChunk,
                    overlapInTokens,
                    chunkHeader: headingLine + "\n",
                    tokenCounter: tokenCounter);

                foreach (var chunk in bodyChunks)
                {
                    yield return chunk;
                }

                continue;
            }

            var lines = TextChunker.SplitMarkDownLines(section, settings.MaxTokensPerChunk, tokenCounter);
            var chunks = TextChunker.SplitPlainTextParagraphs(
                lines,
                settings.MaxTokensPerChunk,
                overlapInTokens,
                tokenCounter: tokenCounter);

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }
    }

    private static IEnumerable<string> SplitMarkdownSections(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var sectionBuilder = new StringBuilder();
        var insideCodeFence = false;
        var codeFenceMarker = '\0';
        var codeFenceLength = 0;

        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            if (TryToggleCodeFence(line, ref insideCodeFence, ref codeFenceMarker, ref codeFenceLength))
            {
                AppendLine(sectionBuilder, line);
                continue;
            }

            if (!insideCodeFence && IsAtxHeading(line) && ContainsVisibleContent(sectionBuilder))
            {
                yield return TrimSection(sectionBuilder);
                sectionBuilder.Clear();
            }

            AppendLine(sectionBuilder, line);
        }

        if (ContainsVisibleContent(sectionBuilder))
        {
            yield return TrimSection(sectionBuilder);
        }
    }

    private static int GetOverlapInTokens(ChunkerSettings settings)
    {
        return (int)Math.Floor(settings.ChunkOverlap * settings.MaxTokensPerChunk);
    }

    private static bool TryExtractHeading(string section, out string headingLine, out string body)
    {
        headingLine = string.Empty;
        body = string.Empty;

        using var reader = new StringReader(section);
        var firstLine = reader.ReadLine();
        if (firstLine == null || !IsAtxHeading(firstLine))
        {
            return false;
        }

        headingLine = firstLine.TrimEnd();
        body = reader.ReadToEnd()?.TrimStart('\r', '\n');

        return true;
    }

    private static bool TryToggleCodeFence(string line, ref bool insideCodeFence, ref char codeFenceMarker, ref int codeFenceLength)
    {
        if (!TryGetFence(line, out var fenceMarker, out var fenceLength, out var canOpen, out var canClose))
        {
            return false;
        }

        if (!insideCodeFence)
        {
            if (!canOpen)
            {
                return false;
            }

            insideCodeFence = true;
            codeFenceMarker = fenceMarker;
            codeFenceLength = fenceLength;
            return true;
        }

        if (!canClose || fenceMarker != codeFenceMarker || fenceLength < codeFenceLength)
        {
            return false;
        }

        insideCodeFence = false;
        codeFenceMarker = '\0';
        codeFenceLength = 0;
        return true;
    }

    private static bool TryGetFence(string line, out char fenceMarker, out int fenceLength, out bool canOpen, out bool canClose)
    {
        fenceMarker = '\0';
        fenceLength = 0;
        canOpen = false;
        canClose = false;

        var trimmed = line.TrimStart(' ');
        if (trimmed.Length == 0)
        {
            return false;
        }

        var indentation = line.Length - trimmed.Length;
        if (indentation > 3)
        {
            return false;
        }

        var marker = trimmed[0];
        if (marker is not ('`' or '~'))
        {
            return false;
        }

        var index = 0;
        while (index < trimmed.Length && trimmed[index] == marker)
        {
            index++;
        }

        if (index < 3)
        {
            return false;
        }

        fenceMarker = marker;
        fenceLength = index;

        var trailing = trimmed[index..];
        canOpen = true;
        canClose = string.IsNullOrWhiteSpace(trailing);

        return true;
    }

    private static bool IsAtxHeading(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.TrimStart(' ');
        var indentation = line.Length - trimmed.Length;
        if (indentation > 3 || trimmed.Length == 0 || trimmed[0] != '#')
        {
            return false;
        }

        var level = 0;
        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        if (level is 0 or > 6)
        {
            return false;
        }

        return trimmed.Length == level || trimmed[level] == ' ';
    }

    private static bool ContainsVisibleContent(StringBuilder builder)
    {
        for (var index = 0; index < builder.Length; index++)
        {
            if (!char.IsWhiteSpace(builder[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static string TrimSection(StringBuilder builder)
    {
        return builder.ToString().Trim('\r', '\n');
    }

    private static void AppendLine(StringBuilder builder, string line)
    {
        builder.Append(line);
        builder.Append('\n');
    }
}
