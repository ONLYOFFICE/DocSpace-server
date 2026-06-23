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

namespace ASC.Data.Encryption;

internal sealed class StreamWrapper : Stream
{
    private readonly Stream _stream;
    private readonly CryptoStreamWrapper _cryptoStream;
    private readonly SymmetricAlgorithm _symmetricAlgorithm;
    private readonly ICryptoTransform _cryptoTransform;
    private readonly long _fileSize;
    private readonly long _metadataLength;

    public StreamWrapper(Stream fileStream, Metadata metadata)
    {
        _stream = fileStream;
        _symmetricAlgorithm = metadata.GetCryptographyAlgorithm();
        _cryptoTransform = _symmetricAlgorithm.CreateDecryptor();
        _cryptoStream = new CryptoStreamWrapper(_stream, _cryptoTransform, CryptoStreamMode.Read);
        _fileSize = metadata.GetFileSize();
        _metadataLength = metadata.GetMetadataLength();
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _fileSize;

    public override long Position
    {
        get => _stream.Position - _metadataLength;
        set
        {
            if (value < 0 || value > _fileSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }

            _stream.Position = value + _metadataLength;
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _cryptoStream.Read(buffer, offset, count);
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

    public override void Close()
    {
        _cryptoStream.Dispose();
        _stream.Dispose();
        _symmetricAlgorithm.Dispose();
        _cryptoTransform.Dispose();
    }
}