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

namespace ASC.Files.Tests.Tests._08_Text;

public class CsvDocumentProcessingStrategyTests
{
    [Fact]
    public async Task CsvStrategy_ShouldEmitOneChunkPerRow_WhenRowsFit()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("alpha,beta\r\ngamma,delta\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".csv",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            "Column 1: alpha\nColumn 2: beta",
            "Column 1: gamma\nColumn 2: delta");
    }

    [Fact]
    public async Task CsvStrategy_ShouldSplitLargeRowWithOverlap_WhenRowExceedsLimit()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("one two three four five six seven eight nine\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".csv",
            CreateSettings(maxTokensPerChunk: 4, chunkOverlap: 0.25f),
            TestContext.Current.CancellationToken));

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().OnlyContain(chunk => CountTokens(chunk) <= 4);
        GetWordOverlap(chunks[0], chunks[1]).Should().NotBeEmpty();
    }

    [Fact]
    public async Task CsvStrategy_ShouldSkipRowsWithoutValues()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream(",,\r\nfirst,second\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".csv",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal("Column 1: first\nColumn 2: second");
    }

    [Fact]
    public async Task CsvStrategy_ShouldTreatFirstRowAsData()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("name,age\r\nalice,30\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".csv",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().HaveCount(2);
        chunks[0].Should().Be("Column 1: name\nColumn 2: age");
        chunks[1].Should().Be("Column 1: alice\nColumn 2: 30");
    }

    [Fact]
    public async Task CsvStrategy_ShouldNormalizeQuotedMultilineCells()
    {
        var strategy = new CsvDocumentProcessingStrategy();
        await using var stream = CreateStream("\"hello\r\nworld\",value\r\n");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".csv",
            CreateSettings(),
            TestContext.Current.CancellationToken));

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
        var csvChunks = await ToListAsync(processor.ProcessAsync(
            csvStream,
            csvStream.Length,
            ".csv",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        await using var textStream = CreateStream("plain text document");
        var textChunks = await ToListAsync(processor.ProcessAsync(
            textStream,
            textStream.Length,
            ".txt", CreateSettings(),
            TestContext.Current.CancellationToken));

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
