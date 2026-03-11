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

using ASC.Files.Core.Text;

namespace ASC.Files.Tests.Tests._08_Text;

public class CsvDocumentProcessingStrategyTests
{
    [Fact]
    public async Task CsvStrategy_ShouldEmitOneChunkPerRow_WhenRowsFit()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("alpha,beta\r\ngamma,delta\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".csv", CreateSettings()));

        chunks.Should().Equal(
            "Column 1: alpha\nColumn 2: beta",
            "Column 1: gamma\nColumn 2: delta");
    }

    [Fact]
    public async Task CsvStrategy_ShouldSplitLargeRowWithOverlap_WhenRowExceedsLimit()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("one two three four five six seven eight nine\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".csv", CreateSettings(maxTokensPerChunk: 4, chunkOverlap: 0.25f)));

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().OnlyContain(chunk => CountTokens(chunk) <= 4);
        GetWordOverlap(chunks[0], chunks[1]).Should().NotBeEmpty();
    }

    [Fact]
    public async Task CsvStrategy_ShouldSkipRowsWithoutValues()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream(",,\r\nfirst,second\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".csv", CreateSettings()));

        chunks.Should().Equal("Column 1: first\nColumn 2: second");
    }

    [Fact]
    public async Task CsvStrategy_ShouldTreatFirstRowAsData()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("name,age\r\nalice,30\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".csv", CreateSettings()));

        chunks.Should().HaveCount(2);
        chunks[0].Should().Be("Column 1: name\nColumn 2: age");
        chunks[1].Should().Be("Column 1: alice\nColumn 2: 30");
    }

    [Fact]
    public async Task CsvStrategy_ShouldNormalizeQuotedMultilineCells()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("\"hello\r\nworld\",value\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".csv", CreateSettings()));

        chunks.Should().Equal("Column 1: hello world\nColumn 2: value");
    }

    [Fact]
    public async Task TextProcessor_ShouldRouteCsvToCsvStrategy_AndOthersToGenericStrategy()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITextExtractor, FakeTextExtractor>()
            .AddSingleton<DocumentTextExtractor>()
            .AddSingleton<GenericDocumentProcessingStrategy>()
            .AddSingleton<CsvDocumentProcessingStrategy>()
            .AddSingleton<TextProcessor>()
            .BuildServiceProvider();

        var processor = services.GetRequiredService<TextProcessor>();

        await using var csvStream = CreateStream("first,second\r\n");
        var csvChunks = await ToListAsync(processor.ProcessAsync(csvStream, csvStream.Length, ".csv", CreateSettings()));

        await using var textStream = CreateStream("plain text document");
        var textChunks = await ToListAsync(processor.ProcessAsync(textStream, textStream.Length, ".txt", CreateSettings()));

        csvChunks.Should().Equal("Column 1: first\nColumn 2: second");
        textChunks.Should().Equal("plain text document");
    }

    private static ChunkerSettings CreateSettings(int maxTokensPerChunk = 32, float chunkOverlap = 0.1f)
    {
        return new ChunkerSettings
        {
            MaxTokensPerChunk = maxTokensPerChunk,
            ChunkOverlap = chunkOverlap,
            TokenCounter = CountTokens
        };
    }

    private static int CountTokens(string text)
    {
        return text
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string[] GetWordOverlap(string left, string right)
    {
        var leftWords = left.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var rightWords = right.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);

        return leftWords.Intersect(rightWords).ToArray();
    }

    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    private static async Task<List<string>> ToListAsync(IAsyncEnumerable<string> source)
    {
        var chunks = new List<string>();

        await foreach (var item in source)
        {
            chunks.Add(item);
        }

        return chunks;
    }

    private sealed class FakeTextExtractor : ITextExtractor
    {
        public Task<string?> ExtractAsync(Stream content, long contentLength)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
