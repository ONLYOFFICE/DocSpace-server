// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Security.Cryptography;

public static class Hasher
{
    private const HashAlg DefaultAlg = HashAlg.SHA256;

    public static byte[] Hash(string data, HashAlg hashAlg)
    {
        return ComputeHash(data, hashAlg);
    }

    public static byte[] Hash(string data)
    {
        return Hash(data, DefaultAlg);
    }

    public static byte[] Hash(byte[] data, HashAlg hashAlg)
    {
        return ComputeHash(data, hashAlg);
    }

    public static byte[] Hash(byte[] data)
    {
        return Hash(data, DefaultAlg);
    }

    public static string Base64Hash(string data, HashAlg hashAlg)
    {
        return ComputeHash64(data, hashAlg);
    }

    public static string Base64Hash(string data)
    {
        return Base64Hash(data, DefaultAlg);
    }

    public static string Base64Hash(byte[] data, HashAlg hashAlg)
    {
        return ComputeHash64(data, hashAlg);
    }

    public static string Base64Hash(byte[] data)
    {
        return Base64Hash(data, DefaultAlg);
    }

    public static bool EqualHash(byte[] dataToCompare, byte[] hash)
    {
        return EqualHash(dataToCompare, hash, DefaultAlg);
    }

    public static bool EqualHash(string dataToCompare, string hash, HashAlg hashAlg)
    {
        return EqualHash(S2B(dataToCompare), S642B(hash), hashAlg);
    }

    public static bool EqualHash(string dataToCompare, string hash)
    {
        return EqualHash(dataToCompare, hash, DefaultAlg);
    }

    public static bool EqualHash(byte[] dataToCompare, byte[] hash, HashAlg hashAlg)
    {
        return string.Equals(
            ComputeHash64(dataToCompare, hashAlg),
            B2S64(hash)
            );
    }

    private static HashAlgorithm GetAlg(HashAlg hashAlg)
    {
        return hashAlg switch
        {
            HashAlg.MD5 => MD5.Create(),
            HashAlg.SHA1 => SHA1.Create(),
            HashAlg.SHA256 => SHA256.Create(),
            HashAlg.SHA512 => SHA512.Create(),
            _ => SHA256.Create()
        };
    }

    private static byte[] S2B(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return Encoding.UTF8.GetBytes(str);
    }

    private static string B2S(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Encoding.UTF8.GetString(data);
    }

    private static byte[] S642B(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return Convert.FromBase64String(str);
    }

    private static string B2S64(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Convert.ToBase64String(data);
    }

    private static byte[] ComputeHash(byte[] data, HashAlg hashAlg)
    {
        using var alg = GetAlg(hashAlg);

        return alg.ComputeHash(data);
    }

    private static byte[] ComputeHash(string data, HashAlg hashAlg)
    {
        return ComputeHash(S2B(data), hashAlg);
    }

    private static string ComputeHash64(byte[] data, HashAlg hashAlg)
    {
        return B2S64(ComputeHash(data, hashAlg));
    }

    private static string ComputeHash64(string data, HashAlg hashAlg)
    {
        return ComputeHash64(S2B(data), hashAlg);
    }
}