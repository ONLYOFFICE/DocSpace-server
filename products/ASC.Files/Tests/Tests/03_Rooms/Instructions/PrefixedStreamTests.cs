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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

/// <summary>
/// The head of an uploaded skill file is read for the frontmatter check before the file is written. A seekable
/// source is simply rewound afterwards; a non-seekable one (a request body) cannot be, so the bytes already taken
/// are put back in front of the rest, and storage receives the original content byte for byte.
/// </summary>
public class PrefixedStreamTests
{
    [Fact]
    public async Task ReadHeadAsync_ShouldRewindSeekableSource()
    {
        var content = Content(1000);
        await using var source = new MemoryStream(content);

        var (head, stream) = await PrefixedStream.ReadHeadAsync(source, 100, TestContext.Current.CancellationToken);

        head.ToArray().Should().Equal(content[..100]);
        stream.Should().BeSameAs(source);
        source.Position.Should().Be(0);
        (await ReadAllAsync(stream)).Should().Equal(content);
    }

    [Fact]
    public async Task ReadHeadAsync_ShouldRewindSeekableSource_ToWhereItStarted()
    {
        var content = Content(1000);
        await using var source = new MemoryStream(content) { Position = 10 };

        var (head, _) = await PrefixedStream.ReadHeadAsync(source, 100, TestContext.Current.CancellationToken);

        head.ToArray().Should().Equal(content[10..110]);
        source.Position.Should().Be(10);
    }

    [Theory]
    [InlineData(1000, 100)]
    [InlineData(1000, 1000)]
    [InlineData(100, 1000)]
    [InlineData(0, 100)]
    [InlineData(1, 1)]
    public async Task ReadHeadAsync_ShouldPutTheHeadBackInFront_WhenSourceIsNotSeekable(int contentLength, int maxHead)
    {
        var content = Content(contentLength);
        await using var source = new NonSeekableStream(content);

        var (head, stream) = await PrefixedStream.ReadHeadAsync(source, maxHead, TestContext.Current.CancellationToken);

        head.ToArray().Should().Equal(content[..Math.Min(contentLength, maxHead)]);
        stream.Should().BeOfType<PrefixedStream>();
        stream.CanSeek.Should().BeFalse();
        (await ReadAllAsync(stream)).Should().Equal(content);
    }

    [Fact]
    public async Task ReadHeadAsync_ShouldFillTheHead_WhenSourceReturnsSmallReads()
    {
        var content = Content(1000);
        await using var source = new NonSeekableStream(content, maxRead: 7);

        var (head, stream) = await PrefixedStream.ReadHeadAsync(source, 100, TestContext.Current.CancellationToken);

        head.Length.Should().Be(100);
        (await ReadAllAsync(stream, bufferSize: 13)).Should().Equal(content);
    }

    [Fact]
    public async Task PrefixedStream_ShouldServeSyncReads_AcrossTheBoundary()
    {
        var content = Content(300);
        await using var source = new NonSeekableStream(content);
        var (_, stream) = await PrefixedStream.ReadHeadAsync(source, 100, TestContext.Current.CancellationToken);

        var buffer = new byte[64];
        var result = new List<byte>();
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            result.AddRange(buffer[..read]);
        }

        result.Should().Equal(content);
    }

    [Fact]
    public async Task PrefixedStream_ShouldNotSeekOrWrite()
    {
        await using var source = new NonSeekableStream(Content(10));
        var (_, stream) = await PrefixedStream.ReadHeadAsync(source, 5, TestContext.Current.CancellationToken);

        stream.CanWrite.Should().BeFalse();
        stream.Invoking(s => s.Seek(0, SeekOrigin.Begin)).Should().Throw<NotSupportedException>();
        stream.Invoking(s => s.Position = 0).Should().Throw<NotSupportedException>();
        stream.Invoking(s => s.Write([1], 0, 1)).Should().Throw<NotSupportedException>();
    }

    private static byte[] Content(int length)
    {
        return Enumerable.Range(0, length).Select(i => (byte)(i * 31 % 251)).ToArray();
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, int bufferSize = 4096)
    {
        using var result = new MemoryStream();
        await stream.CopyToAsync(result, bufferSize, TestContext.Current.CancellationToken);

        return result.ToArray();
    }

    private sealed class NonSeekableStream(byte[] content, int maxRead = int.MaxValue) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, Math.Min(count, maxRead));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.ReadAsync(buffer, offset, Math.Min(count, maxRead), cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(buffer[..Math.Min(buffer.Length, maxRead)], cancellationToken);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
