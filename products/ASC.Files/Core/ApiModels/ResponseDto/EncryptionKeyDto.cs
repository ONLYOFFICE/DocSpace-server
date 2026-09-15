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
/// An encryption key pair as the portal reports it: the public half of some member's key, with the encrypted private
/// half filled in only when the pair belongs to the caller.
/// </summary>
public class EncryptionKeyDto
{
    private const string DefaultCryptoEngineId = "{DC522726-5E0E-43E5-AA02-8EA156BECBC5}";

    /// <summary>
    /// Names the pair inside its owner's key set. Pass it back to rotate the pair or to delete it; the all-zero value
    /// belongs to a client that stores its keys without sending an identifier.
    /// </summary>
    /// <example>9924256B-447C-4F19-9dbd-8ad8c39e8ff5</example>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The member the pair belongs to. In the key set of a room or of a file this is how the caller tells its own
    /// entries, the ones carrying a private half, from those of the other members.
    /// </summary>
    /// <example>9924256B-447C-4F19-9dbd-8ad8c39e8ff5</example>
    public Guid UserId { get; set; }
    //public EncryptionKeyType Type { get; set; }

    /// <summary>
    /// When this key material was written. Rotating the pair refreshes it, so it dates the material that is being
    /// reported rather than the first appearance of the identifier.
    /// </summary>
    /// <example>2025-01-01T00:00:00</example>
    public DateTime Date { get; set; } = DateTime.Now;
    //public string Version { get; set; }

    /// <summary>
    /// The public half of the pair, the half a client encrypts file keys with. A pair whose public half is missing
    /// is treated as no access and left out of a room's or a file's key set.
    /// </summary>
    /// <example>MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...</example>
    public string PublicKey { get; set; }

    /// <summary>
    /// The private half, encrypted with its owner's password. It is filled in only when the pair belongs to the
    /// calling user; on another member's entry it comes back empty, because the private half is not handed out.
    /// </summary>
    /// <example>U2FsdGVkX1+Lm3s...</example>
    public string PrivateKeyEnc { get; set; }

    /// <summary>
    /// The crypto engine this material was issued for, as a braced GUID. The engine is portal-wide, so the same value
    /// comes back for every key of every member.
    /// </summary>
    /// <example>{DC522726-5E0E-43E5-AA02-8EA156BECBC5}</example>
    public string CryptoEngineId { get; set; } = DefaultCryptoEngineId;
    //public string CryptoEngineId => DefaultCryptoEngineId;
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class EncryptionKeyMapper
{
    public static partial EncryptionKeyDto Map(this EncryptionKeyRequestDto source);
}
