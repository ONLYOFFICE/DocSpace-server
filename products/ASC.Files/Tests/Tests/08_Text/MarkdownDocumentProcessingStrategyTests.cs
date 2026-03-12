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

public class MarkdownDocumentProcessingStrategyTests
{
    [Fact]
    public async Task MarkdownStrategy_ShouldSplitByHeadings_WhenSectionsFit()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("# Intro\nalpha\n## Details\nbeta\n### Deep Dive\ngamma");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".md", CreateSettings()));

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

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".md", CreateSettings(maxTokensPerChunk: 5, chunkOverlap: 0.2f)));

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

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".md", CreateSettings()));

        chunks.Should().Equal(
            "Lead in",
            "# Title\nBody");
    }

    [Fact]
    public async Task MarkdownStrategy_ShouldIgnoreHeadingsInsideCodeFences()
    {
        var strategy = CreateStrategy();
        await using var stream = CreateStream("# Title\n```\n## not heading\n```\nBody\n## Next\nTail");

        var chunks = await ToListAsync(strategy.ProcessAsync(stream, stream.Length, ".md", CreateSettings()));

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
        var markdownChunks = await ToListAsync(processor.ProcessAsync(markdownStream, markdownStream.Length, ".md", CreateSettings()));

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
