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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The keys the calling account needs in order to open one file of an end-to-end encrypted private room.
/// </summary>
public class FileEncryptionInfoDto
{
    /// <summary>
    /// The key pairs of the calling account, never those of the other people in the room. The private half of each
    /// pair is stored encrypted with that person's own password and has to be decrypted on the client. An empty list
    /// means the account has generated no key pair yet, and until it does no file key can be issued to it.
    /// </summary>
    /// <example>
    /// [
    ///   {
    ///     "id": "9924256B-447C-4F19-9dbd-8ad8c39e8ff5",
    ///     "userId": "9924256B-447C-4F19-9dbd-8ad8c39e8ff5",
    ///     "date": "2025-01-01T00:00:00",
    ///     "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBg...",
    ///     "privateKeyEnc": "U2FsdGVkX1+Lm3s...",
    ///     "cryptoEngineId": "defaultCryptoEngine"
    ///   }
    /// ]
    /// </example>
    public List<EncryptionKeyDto> UserKeys { get; set; }

    /// <summary>
    /// The keys of this file that were issued to the calling account, each naming the public key it was encrypted for
    /// so that the client can pick the matching private half. An empty list means the file has not been shared with
    /// this account rather than that the file is unencrypted.
    /// </summary>
    /// <example>
    /// [
    ///   {
    ///     "userId": "9924256B-447C-4F19-9dbd-8ad8c39e8ff5",
    ///     "publicKeyId": "9924256B-447C-4F19-9dbd-8ad8c39e8ff5",
    ///     "privateKeyEnc": "U2FsdGVkX1+Lm3s...",
    ///     "tenantId": 1,
    ///     "fileId": 9846,
    ///     "createOn": "2025-01-01T00:00:00"
    ///   }
    /// ]
    /// </example>
    public List<FileKeys> FileKeys { get; set; }
}