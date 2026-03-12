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
