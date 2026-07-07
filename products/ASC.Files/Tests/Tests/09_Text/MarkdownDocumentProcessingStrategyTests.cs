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

public class MarkdownDocumentProcessingStrategyTests
{
    [Fact]
    public async Task MarkdownStrategy_ShouldSplitByHeadings_WhenSectionsFit()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("# Intro\nalpha\n## Details\nbeta\n### Deep Dive\ngamma");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".md",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            "# Intro\nalpha",
            "## Details\nbeta",
            "### Deep Dive\ngamma");
    }

    [Fact]
    public async Task MarkdownStrategy_ShouldSplitOversizedSectionBySize_AndKeepHeadingContext()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("# Title\none two three four five six seven eight nine ten");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".md",
            CreateSettings(maxTokensPerChunk: 5, chunkOverlap: 0.2f),
            TestContext.Current.CancellationToken));

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().OnlyContain(chunk => chunk.StartsWith("# Title\n"));
        chunks.Should().OnlyContain(chunk => CountTokens(chunk) <= 5);
        GetWordOverlap(chunks[0], chunks[1]).Should().NotBeEmpty();
    }

    [Fact]
    public async Task MarkdownStrategy_ShouldPreserveContentBeforeFirstHeading()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("Lead in\n\n# Title\nBody");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".md",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().Equal(
            "Lead in",
            "# Title\nBody");
    }

    [Fact]
    public async Task MarkdownStrategy_ShouldIgnoreHeadingsInsideCodeFences()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("# Title\n```\n## not heading\n```\nBody\n## Next\nTail");

        var chunks = await ToListAsync(strategy.ProcessAsync(
            stream,
            stream.Length,
            ".md",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        chunks.Should().HaveCount(2);
        chunks[0].Should().Be("# Title\n```\n## not heading\n```\nBody");
        chunks[1].Should().Be("## Next\nTail");
    }

    [Fact]
    public async Task TextProcessor_ShouldRouteMarkdownToMarkdownStrategy()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITextExtractor, FakeTextExtractor>()
            .AddSingleton<DocumentTextExtractor>()
            .AddSingleton<GenericDocumentProcessingStrategy>()
            .AddSingleton<CsvDocumentProcessingStrategy>()
            .AddSingleton<MarkdownDocumentProcessingStrategy>()
            .AddSingleton<TextProcessor>()
            .BuildServiceProvider();

        var processor = services.GetRequiredService<TextProcessor>();

        await using var markdownStream = CreateStream("# Intro\nalpha\n## Details\nbeta");
        var markdownChunks = await ToListAsync(processor.ProcessAsync(
            markdownStream,
            markdownStream.Length,
            ".md",
            CreateSettings(),
            TestContext.Current.CancellationToken));

        markdownChunks.Should().Equal(
            "# Intro\nalpha",
            "## Details\nbeta");
    }

    private static MarkdownDocumentProcessingStrategy CreateStrategy()
    {
        return new MarkdownDocumentProcessingStrategy(new DocumentTextExtractor(new FakeTextExtractor()));
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
