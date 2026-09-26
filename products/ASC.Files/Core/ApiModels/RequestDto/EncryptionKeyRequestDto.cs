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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The two halves of an encryption key pair to store for the calling user, plus the identifier the pair is kept
/// under.
/// </summary>
public class EncryptionKeyRequestDto
{
    //public EncryptionKeyType Type { get; set; }
    //public string Version { get; set; }

    /// <summary>
    /// Names the pair inside the caller's own key set. The client generates it, and leaving it out means the all-zero
    /// GUID, which is the pair a client that never sends an identifier keeps working with.
    /// </summary>
    /// <example>9924256B-447C-4F19-9dbd-8ad8c39e8ff5</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The public half of the pair, as the client's crypto engine produced it and stored verbatim. This is the half
    /// handed to the other members of a private room so that they can encrypt file keys for this user.
    /// </summary>
    /// <example>MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...</example>
    public string PublicKey { get; set; }

    /// <summary>
    /// The private half of the pair, encrypted on the client with the user's password before it is sent. The portal
    /// stores it as opaque text and cannot decrypt it, so material lost on the client cannot be recovered from here.
    /// </summary>
    /// <example>U2FsdGVkX1+Lm3s...</example>
    public string PrivateKeyEnc { get; set; }
}
