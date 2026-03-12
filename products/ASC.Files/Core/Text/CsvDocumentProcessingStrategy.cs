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

using System.Runtime.CompilerServices;

using CsvHelper;
using CsvHelper.Configuration;

namespace ASC.Files.Core.Text;

[Singleton]
public class CsvDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    private static readonly CsvConfiguration _configuration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        IgnoreBlankLines = false,
        TrimOptions = TrimOptions.Trim,
        DetectDelimiter = true
    };

    public async IAsyncEnumerable<string> ProcessAsync(
        Stream content,
        long contentLength,
        string fileExtension,
        ChunkerSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        using var csv = new CsvReader(reader, _configuration);

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowText = FormatRow(csv.Parser.Record);
            if (string.IsNullOrEmpty(rowText))
            {
                continue;
            }

            if (settings.TokenCounter(rowText) <= settings.MaxTokensPerChunk)
            {
                yield return rowText;
                continue;
            }

            foreach (var chunk in TextChunkingHelper.Split(rowText, settings))
            {
                yield return chunk;
            }
        }
    }

    private static string FormatRow(string[] record)
    {
        if (record == null || record.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        for (var index = 0; index < record.Length; index++)
        {
            var value = NormalizeWhitespace(record[index]);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("Column ");
            builder.Append(index + 1);
            builder.Append(": ");
            builder.Append(value);
        }

        return builder.ToString();
    }

    private static string NormalizeWhitespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        if (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
