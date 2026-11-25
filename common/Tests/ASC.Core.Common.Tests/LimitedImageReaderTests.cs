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

namespace ASC.Core.Common.Tests;

/// <summary>
/// <see cref="LimitedImageReader"/> reads the avatar of a third-party profile and a photo a member points the portal
/// at. Both come from an address someone else chose, so the body is cut off at the size limit whatever the response
/// says about its length: a response without a length was read whole before the size was checked.
/// </summary>
public class LimitedImageReaderTests
{
    private const long Limit = 1024;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    public async Task ReadAsync_BodyUpToTheLimit_ReturnsItWhole(int size)
    {
        var body = Enumerable.Range(0, size).Select(i => (byte)i).ToArray();

        var read = await LimitedImageReader.ReadAsync(new UnsizedContent(body), Limit, TestContext.Current.CancellationToken);

        read.Should().Equal(body);
    }

    [Theory]
    [InlineData(1025)]
    [InlineData(200_000)]
    public async Task ReadAsync_BodyOverTheLimitWithoutALength_ReturnsNull(int size)
    {
        var content = new UnsizedContent(new byte[size]);

        var read = await LimitedImageReader.ReadAsync(content, Limit, TestContext.Current.CancellationToken);

        read.Should().BeNull();
        // the reader stops within one read buffer (80 KiB) past the limit instead of taking in the whole body
        content.BytesServed.Should().BeLessThanOrEqualTo(Limit + 81920);
    }

    [Fact]
    public async Task ReadAsync_LengthOverTheLimit_ReturnsNullWithoutReading()
    {
        var content = new ByteArrayContent(new byte[Limit + 1]);

        var read = await LimitedImageReader.ReadAsync(content, Limit, TestContext.Current.CancellationToken);

        read.Should().BeNull();
    }

    /// <summary>
    /// A server that sends the body a byte at a time and never ends it: the client timeout is over once the headers
    /// arrive, so the download ends only by its own deadline (<see cref="LimitedImageReader.DownloadTimeout"/>, shortened
    /// here).
    /// </summary>
    [Fact]
    public async Task DownloadAsync_BodyThatNeverEnds_EndsAtTheDeadline()
    {
        using var client = new HttpClient(new TrickleHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://avatars.example.com/a.png");

        var download = async () => await LimitedImageReader.DownloadAsync(
            client, request, Limit, TestContext.Current.CancellationToken, TimeSpan.FromMilliseconds(300));

        await download.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DownloadAsync_Redirect_ReturnsTheLocationAndNoBytes()
    {
        using var client = new HttpClient(new RedirectHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.example.com/1/picture");

        var download = await LimitedImageReader.DownloadAsync(client, request, Limit, TestContext.Current.CancellationToken);

        download.StatusCode.Should().Be(HttpStatusCode.Found);
        download.Location.Should().Be(new Uri("https://cdn.example.com/1.jpg"));
        download.Bytes.Should().BeNull();
    }

    private sealed class TrickleHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new TrickleStream()) });
        }
    }

    private sealed class RedirectHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://cdn.example.com/1.jpg");

            return Task.FromResult(response);
        }
    }

    /// <summary>Returns one byte per read, after a pause, for ever.</summary>
    private sealed class TrickleStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            buffer.Span[0] = 1;

            return 1;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A body sent without a Content-Length, as a chunked response arrives, that counts what was read.</summary>
    private sealed class UnsizedContent(byte[] body) : HttpContent
    {
        private readonly CountingStream _stream = new(body);

        public long BytesServed => _stream.Served;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return _stream.CopyToAsync(stream);
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(_stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }
    }

    private sealed class CountingStream(byte[] body) : MemoryStream(body)
    {
        public long Served { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await base.ReadAsync(buffer, cancellationToken);
            Served += read;

            return read;
        }
    }
}
