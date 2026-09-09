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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// Shared setup for the room-logo suites (<c>POST /files/logos</c>,
/// <c>POST/DELETE /files/rooms/{id}/logo</c>): image generators mirroring the TS suite's
/// <c>src/utils/test-image.ts</c>, upload/create helpers and a raw multipart client for the
/// requests the typed SDK cannot express.
/// </summary>
public abstract class RoomLogoTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private const string TestImageBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly uint[] _crcTable = BuildCrcTable();

    /// <summary>The minimal valid 1x1 PNG used by most of the TS suite's positive tests.</summary>
    protected static byte[] CreateTestImageBytes() => Convert.FromBase64String(TestImageBase64);

    /// <summary>Builds a valid solid-fill PNG of the given size and colour type.</summary>
    protected static byte[] CreatePng(int width, int height, byte colorType = 6, (byte R, byte G, byte B, byte A) fill = default)
    {
        if (fill == default)
        {
            fill = (255, 0, 0, 255);
        }

        var channels = colorType == 0 ? 1 : colorType == 2 ? 3 : 4;
        var rowLength = width * channels;
        var raw = new byte[(rowLength + 1) * height];

        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (rowLength + 1);
            raw[rowStart] = 0; // filter type: none

            for (var x = 0; x < width; x++)
            {
                var pixel = rowStart + 1 + x * channels;

                if (channels == 1)
                {
                    raw[pixel] = fill.R;
                }
                else if (channels == 3)
                {
                    raw[pixel] = fill.R;
                    raw[pixel + 1] = fill.G;
                    raw[pixel + 2] = fill.B;
                }
                else
                {
                    raw[pixel] = fill.R;
                    raw[pixel + 1] = fill.G;
                    raw[pixel + 2] = fill.B;
                    raw[pixel + 3] = fill.A;
                }
            }
        }

        var ihdr = new byte[13];
        WriteUInt32BigEndian(ihdr, 0, (uint)width);
        WriteUInt32BigEndian(ihdr, 4, (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = colorType;
        ihdr[10] = 0; // compression
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace

        using var output = new MemoryStream();
        output.Write(_pngSignature);
        output.Write(BuildChunk("IHDR", ihdr));
        output.Write(BuildChunk("IDAT", Deflate(raw)));
        output.Write(BuildChunk("IEND", []));

        return output.ToArray();
    }

    /// <summary>Opaque (no alpha) RGB PNG.</summary>
    protected static byte[] CreateOpaquePng() => CreatePng(2, 2, 2, (10, 200, 50, 255));

    /// <summary>Transparent RGBA PNG (alpha 0).</summary>
    protected static byte[] CreateTransparentPng() => CreatePng(2, 2, 6, (0, 0, 0, 0));

    /// <summary>Grayscale PNG (colour type 0).</summary>
    protected static byte[] CreateGrayscalePng() => CreatePng(4, 4, 0, (128, 0, 0, 0));

    /// <summary>Inserts a tEXt metadata chunk (keyword\0text) before IEND.</summary>
    protected static byte[] CreatePngWithText(string keyword, string text)
    {
        var basePng = CreatePng(1, 1);
        const int iendLength = 12; // length(4) + type(4) + crc(4), IEND has empty data
        var body = basePng[..^iendLength];
        var iend = basePng[^iendLength..];

        var textData = new byte[keyword.Length + 1 + text.Length];
        Encoding.Latin1.GetBytes(keyword, 0, keyword.Length, textData, 0);
        textData[keyword.Length] = 0;
        Encoding.Latin1.GetBytes(text, 0, text.Length, textData, keyword.Length + 1);

        return [.. body, .. BuildChunk("tEXt", textData), .. iend];
    }

    /// <summary>
    /// Decompression bomb: a tiny compressed IDAT that expands to a huge raster. A solid-fill
    /// grayscale image of large dimensions has a raw size of width*height bytes but compresses to
    /// only a few bytes.
    /// </summary>
    protected static byte[] CreateDecompressionBombPng(int side = 6000) => CreatePng(side, side, 0, (0, 0, 0, 0));

    /// <summary>Truncated (corrupt) PNG: valid signature + partial IHDR.</summary>
    protected static byte[] CreateCorruptPng() => CreateTestImageBytes()[..24];

    /// <summary>1x1 transparent GIF (GIF89a), used as "declared PNG but actually a GIF".</summary>
    protected static byte[] CreateGifBytes() =>
        Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    /// <summary>1x1 JPEG, used as "declared PNG but actually a JPEG".</summary>
    protected static byte[] CreateJpegBytes() =>
        Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRof" +
            "Hh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/wAALCAABAAEBAREA/8QAFAAB" +
            "AAAAAAAAAAAAAAAAAAAAAv/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAA" +
            "AAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AVf/Z");

    /// <summary>Minimal RIFF/WEBP container (lossy VP8 header), used as "declared PNG but actually WebP".</summary>
    protected static byte[] CreateWebpBytes() =>
        Convert.FromBase64String("UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==");

    /// <summary>SVG markup declared as an image.</summary>
    protected static byte[] CreateSvgBytes() =>
        Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1\" height=\"1\"></svg>");

    /// <summary>Deterministic pseudo-random binary blob (not a valid image of any kind).</summary>
    protected static byte[] CreateRandomBinaryBytes(int size = 256)
    {
        var buffer = new byte[size];
        var seed = 0x12345678u;

        for (var i = 0; i < size; i++)
        {
            seed = seed * 1103515245u + 12345u;
            buffer[i] = (byte)(seed & 0xff);
        }

        return buffer;
    }

    /// <summary>Valid PNG with extra bytes appended after IEND (polyglot PNG/HTML).</summary>
    protected static byte[] CreatePolyglotPng() =>
        [.. CreateTestImageBytes(), .. Encoding.UTF8.GetBytes("<html><script>alert('xss')</script></html>")];

    /// <summary>Uploads image bytes and returns the server-issued <c>tmpFile</c> path.</summary>
    protected async Task<string> UploadLogo(byte[] bytes, string filename = "logo.png", string contentType = "image/png")
    {
        await using var stream = new MemoryStream(bytes);
        var result = await _roomsApi.UploadRoomLogoAsync(
            new FileParameter(filename, contentType, stream),
            TestContext.Current.CancellationToken);

        return result.Response.Data?.ToString() ?? string.Empty;
    }

    /// <summary>Creates the logo of a room from an already-uploaded <paramref name="tmpFile"/>.</summary>
    protected async Task<FolderDtoInteger> CreateLogo(int roomId, string tmpFile, int x = 0, int y = 0, int width = 1, int height = 1)
    {
        return (await _roomsApi.CreateRoomLogoAsync(
            roomId,
            new LogoRequest(tmpFile, x, y, width, height),
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Creates a plain Custom room and sets a fresh 1x1 PNG as its logo.</summary>
    protected async Task<FolderDtoInteger> CreateRoomWithLogo(string title)
    {
        var room = await CreateCustomRoom(title);
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        return await CreateLogo(room.Id, tmpFile);
    }

    /// <summary>
    /// Raw multipart POST to <c>api/2.0/files/logos</c>, needed for the request shapes the typed
    /// SDK cannot express: missing file fields, extra fields, non-file field values and non-POST
    /// methods. Uses the shared <see cref="BaseTest._filesClient"/>, so its current authentication
    /// (or lack of it) applies.
    /// </summary>
    protected async Task<HttpResponseMessage> UploadRoomLogoRaw(
        HttpMethod method,
        (byte[] Bytes, string FileName, string ContentType)[]? files = null,
        Dictionary<string, string>? fields = null,
        bool omitBody = false)
    {
        using var request = new HttpRequestMessage(method, "api/2.0/files/logos");

        if (!omitBody)
        {
            var form = new MultipartFormDataContent();

            foreach (var file in files ?? [])
            {
                var content = new ByteArrayContent(file.Bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(content, "file", file.FileName);
            }

            foreach (var (key, value) in fields ?? [])
            {
                form.Add(new StringContent(value), key);
            }

            request.Content = form;
        }

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Sends an arbitrary raw body to <c>api/2.0/files/logos</c> with an explicit content type.</summary>
    protected async Task<HttpResponseMessage> UploadRoomLogoRaw(string body, string contentType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/logos")
        {
            Content = new StringContent(body, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Raw JSON POST to <c>api/2.0/files/rooms/{id}/logo</c>, needed for tmpFile values the typed
    /// <see cref="LogoRequest"/> cannot express: a missing key entirely or an explicit JSON null
    /// (its constructor rejects a null tmpFile before any request is sent).
    /// </summary>
    protected async Task<HttpResponseMessage> CreateRoomLogoRaw(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/rooms/{roomId}/logo")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static byte[] Deflate(byte[] raw)
    {
        using var output = new MemoryStream();

        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        return output.ToArray();
    }

    private static byte[] BuildChunk(string type, byte[] data)
    {
        var chunk = new byte[4 + 4 + data.Length + 4];
        WriteUInt32BigEndian(chunk, 0, (uint)data.Length);
        Encoding.ASCII.GetBytes(type, 0, 4, chunk, 4);
        data.CopyTo(chunk, 8);

        var crcInput = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type, 0, 4, crcInput, 0);
        data.CopyTo(crcInput, 4);
        WriteUInt32BigEndian(chunk, 8 + data.Length, Crc32(crcInput));

        return chunk;
    }

    private static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Crc32(byte[] buffer)
    {
        var crc = 0xffffffffu;

        foreach (var b in buffer)
        {
            crc = _crcTable[(crc ^ b) & 0xff] ^ (crc >> 8);
        }

        return crc ^ 0xffffffffu;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (uint n = 0; n < 256; n++)
        {
            var c = n;

            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
