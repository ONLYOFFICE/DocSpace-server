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

public class JsonDocumentProcessingStrategyTests
{
    [Fact]
    public async Task JsonStrategy_ShouldSplitFlatObjectsBySerializedSize()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        await using var stream = CreateStream("""{"a":"1111","b":"2222","c":"3333"}""");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 20),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            """{"a":"1111"}""",
            """{"b":"2222"}""",
            """{"c":"3333"}""");
    }

    [Fact]
    public async Task JsonStrategy_ShouldPreserveParentKeys_WhenSplittingNestedObjects()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        await using var stream = CreateStream("""{"outer":{"a":"1111","b":"2222"}}""");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 25),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            """{"outer":{"a":"1111"}}""",
            """{"outer":{"b":"2222"}}""");
    }

    [Fact]
    public async Task JsonStrategy_ShouldSplitArraysUsingIndexedObjects()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        await using var stream = CreateStream("""["1111","2222"]""");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 20),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            """{"0":"1111"}""",
            """{"1":"2222"}""");
    }

    [Fact]
    public async Task JsonStrategy_ShouldReturnSingleChunk_WhenDocumentFits()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        await using var stream = CreateStream("""{"a":"1111"}""");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 50),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal("""{"a":"1111"}""");
    }

    [Fact]
    public async Task JsonStrategy_ShouldEmitOversizedScalarAsSingleChunk()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        await using var stream = CreateStream("""{"a":"abcdefghijklmnopqrstuvwxyz"}""");

        var chunks = await ToListAsync(
            strategy.ProcessAsync(
                stream,
                stream.Length,
                ".json",
                CreateSettings(maxTokensPerChunk: 10),
                TestContext.Current.CancellationToken));

        chunks.Should().Equal("""{"a":"abcdefghijklmnopqrstuvwxyz"}""");
        CountTokens(chunks[0]).Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task JsonStrategy_ShouldFallbackToPlainText_WhenJsonIsInvalid()
    {
        var strategy = new JsonDocumentProcessingStrategy();
        const string invalidJson = """{"a":,}""";
        await using var stream = CreateStream(invalidJson);

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 50),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(invalidJson);
    }

    [Fact]
    public async Task TextProcessor_ShouldRouteJsonToJsonStrategy()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITextExtractor, FakeTextExtractor>()
            .AddSingleton<DocumentTextExtractor>()
            .AddSingleton<GenericDocumentProcessingStrategy>()
            .AddSingleton<CsvDocumentProcessingStrategy>()
            .AddSingleton<JsonDocumentProcessingStrategy>()
            .AddSingleton<MarkdownDocumentProcessingStrategy>()
            .AddSingleton<TextProcessor>()
            .BuildServiceProvider();

        var processor = services.GetRequiredService<TextProcessor>();

        await using var jsonStream = CreateStream("""{"a":"1111","b":"2222"}""");
        var jsonChunks = await ToListAsync(processor.ProcessAsync(
            jsonStream,
            jsonStream.Length,
            ".json",
            CreateSettings(maxTokensPerChunk: 20),
            TestContext.Current.CancellationToken));

        jsonChunks.Should().Equal(
            """{"a":"1111"}""",
            """{"b":"2222"}""");
    }

    private static ChunkerSettings CreateSettings(int maxTokensPerChunk = 64)
    {
        return new ChunkerSettings
        {
            MaxTokensPerChunk = maxTokensPerChunk,
            ChunkOverlap = 0.1f,
            TokenCounter = CountTokens
        };
    }

    private static int CountTokens(string text)
    {
        return text.Length;
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
