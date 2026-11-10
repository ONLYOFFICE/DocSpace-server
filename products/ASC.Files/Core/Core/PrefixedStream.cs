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

namespace ASC.Web.Files.Core;

public sealed class PrefixedStream(ReadOnlyMemory<byte> head, Stream rest) : Stream
{
    private int _headPosition;

    public static async Task<(ReadOnlyMemory<byte> Head, Stream Stream)> ReadHeadAsync(Stream source, int maxLength, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new byte[maxLength];
        var length = 0;

        while (length < maxLength)
        {
            var read = await source.ReadAsync(buffer.AsMemory(length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            length += read;
        }

        var head = buffer.AsMemory(0, length);

        if (source.CanSeek)
        {
            source.Seek(-length, SeekOrigin.Current);

            return (head, source);
        }

        return (head, new PrefixedStream(head, source));
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => head.Length + rest.Length;

    public override long Position
    {
        get => _headPosition + rest.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var fromHead = ReadFromHead(buffer);

        return fromHead > 0 ? fromHead : rest.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var fromHead = ReadFromHead(buffer.Span);

        return fromHead > 0 ? ValueTask.FromResult(fromHead) : rest.ReadAsync(buffer, cancellationToken);
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

    private int ReadFromHead(Span<byte> buffer)
    {
        var remaining = head.Length - _headPosition;
        if (remaining <= 0 || buffer.Length == 0)
        {
            return 0;
        }

        var count = Math.Min(remaining, buffer.Length);
        head.Span.Slice(_headPosition, count).CopyTo(buffer);
        _headPosition += count;

        return count;
    }
}
