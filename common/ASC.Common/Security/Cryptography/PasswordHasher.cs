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

namespace ASC.Security.Cryptography;

/// <ssummary>
/// The password hash parameters.
/// </summary>
[Singleton]
public class PasswordHasher
{
    /// <summary>
    /// The password hash size.
    /// </summary>
    /// <example>32</example>
    public int Size { get; private set; }

    /// <summary>
    /// The number of iterations to generate the ppassword hash.
    /// </summary>
    /// <example>1000</example>
    public int Iterations { get; private set; }

    /// <summary>
    /// The salt to generate the ppassword hash.
    /// </summary>
    /// <example>random_salt_value</example>
    public string Salt { get; private set; }

    public PasswordHasher()
    {

    }

    public PasswordHasher(IConfiguration configuration, MachinePseudoKeys machinePseudoKeys)
    {
        if (!int.TryParse(configuration["core:password:size"], out var size))
        {
            size = 256;
        }

        Size = size;

        if (!int.TryParse(configuration["core.password.iterations"], out var iterations))
        {
            iterations = 100000;
        }

        Iterations = iterations;

        Salt = (configuration["core:password:salt"] ?? "").Trim();
        if (string.IsNullOrEmpty(Salt))
        {
            var salt = Hasher.Hash("{9450BEF7-7D9F-4E4F-A18A-971D8681722D}", HashAlg.SHA256);

            var PasswordHashSaltBytes = KeyDerivation.Pbkdf2(
                                               Encoding.UTF8.GetString(machinePseudoKeys.GetMachineConstant()),
                                               salt,
                                               KeyDerivationPrf.HMACSHA256,
                                               Iterations,
                                               Size / 8);
            Salt = BitConverter.ToString(PasswordHashSaltBytes).Replace("-", string.Empty).ToLower();
        }
    }

    public string GetClientPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            password = Guid.NewGuid().ToString();
        }

        var salt = new UTF8Encoding(false).GetBytes(Salt);

        var hashBytes = KeyDerivation.Pbkdf2(
                           password,
                           salt,
                           KeyDerivationPrf.HMACSHA256,
                           Iterations,
                           Size / 8);

        var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();

        return hash;
    }
}