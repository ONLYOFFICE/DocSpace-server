// (c) Copyright Ascensio System SIA 2009-2024
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


namespace ASC.Data.Encryption;

[Transient]
public class Metadata(IConfiguration configuration)
{
    private const string prefixString = "AscEncrypted";

    private const int prefixLength = 12; // prefixString length
    private const int versionLength = 1; // byte
    private const int sizeLength = 8; // long (int64)

    private const int saltLength = 32; // The salt size must be 8 bytes or larger

    private const int keySize = 256; // key size, in bits, of the secret key used for the symmetric algorithm. AES-256
    private const int blockSize = 128; // block size, in bits, of the cryptographic operation. default is 128 bits

    private const int keyLength = keySize / 8; // secret key used for the symmetric algorithm. 32 bytes
    private const int ivLength = blockSize / 8; // The initialization vector (IV) to use for the symmetric algorithm. 16 bytes

    private const int hmacKeyLength = 64; // HMACSHA256 64-byte private key is recommended.
    private const int hmacHashLength = 32; // HMACSHA256 The output hash is 256 bits (32 bytes) in length

    private const int metadataLength = prefixLength + versionLength + sizeLength + saltLength + hmacHashLength + ivLength;

    private static int? iterations; // Rfc2898DeriveBytes: The minimum recommended number of iterations is 1000.

    private IConfiguration Configuration { get; set; } = configuration;

    private int Iterations
    {
        get
        {
            if (iterations.HasValue)
            {
                return iterations.Value;
            }

            if (!int.TryParse(Configuration["storage:encryption:iterations"], out var iterationsCount))
            {
                iterationsCount = 4096;
            }

            iterations = iterationsCount;

            return iterations.Value;
        }
    }

    private string Password;

    private byte[] Prefix;
    private byte[] Version;
    private byte[] Size;

    private byte[] Salt;

    private byte[] Key;
    private byte[] IV;

    private byte[] HmacKey;
    private byte[] HmacHash;


    public void Initialize(string password)
    {
        Password = password;

        Prefix = Encoding.UTF8.GetBytes(prefixString);
        Version = new byte[versionLength];
        Size = new byte[sizeLength];

        Salt = new byte[saltLength];

        Key = new byte[keyLength];
        IV = new byte[ivLength];

        HmacKey = new byte[hmacKeyLength];
        HmacHash = new byte[hmacHashLength];
    }

    public void Initialize(byte version, string password, long fileSize)
    {
        Password = password;

        Prefix = Encoding.UTF8.GetBytes(prefixString);
        Version = [version];
        Size = LongToByteArray(fileSize);

        Salt = GenerateRandom(saltLength);

        Key = GenerateKey();
        IV = GenerateRandom(ivLength);

        HmacKey = GenerateHmacKey();
        HmacHash = new byte[hmacHashLength]; // Empty byte array. The real hmac will be computed after encryption
    }


    public async Task<bool> TryReadFromStreamAsync(Stream stream, byte cryptVersion)
    {
        try
        {
            var readed = await stream.ReadAsync(Prefix.AsMemory(0, prefixLength));
            if (readed < prefixLength)
            {
                return false;
            }

            if (Encoding.UTF8.GetString(Prefix) != prefixString)
            {
                return false;
            }

            readed = await stream.ReadAsync(Version.AsMemory(0, versionLength));
            if (readed < versionLength)
            {
                return false;
            }

            if (Version[0] != cryptVersion)
            {
                return false;
            }

            readed = await stream.ReadAsync(Size.AsMemory(0, sizeLength));
            if (readed < sizeLength)
            {
                return false;
            }

            if (ByteArrayToLong(Size) < 0)
            {
                return false;
            }

            readed = await stream.ReadAsync(Salt.AsMemory(0, saltLength));
            if (readed < saltLength)
            {
                return false;
            }

            readed = await stream.ReadAsync(HmacHash.AsMemory(0, hmacHashLength));
            if (readed < hmacHashLength)
            {
                return false;
            }

            readed = await stream.ReadAsync(IV.AsMemory(0, ivLength));
            return readed >= ivLength;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task WriteToStreamAsync(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        await stream.WriteAsync(Prefix.AsMemory(0, prefixLength));
        await stream.WriteAsync(Version.AsMemory(0, versionLength));
        await stream.WriteAsync(Size.AsMemory(0, sizeLength));
        await stream.WriteAsync(Salt.AsMemory(0, saltLength));
        await stream.WriteAsync(HmacHash.AsMemory(0, hmacHashLength));
        await stream.WriteAsync(IV.AsMemory(0, ivLength));
    }

    public SymmetricAlgorithm GetCryptographyAlgorithm()
    {
        var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.BlockSize = blockSize;
        aes.Key = Key;
        aes.IV = IV;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;
        return aes;
    }

    public async Task ComputeAndWriteHmacHashAsync(Stream stream)
    {
        HmacHash = ComputeHmacHash(stream);

        stream.Seek(metadataLength - ivLength - hmacHashLength, SeekOrigin.Begin); // Move position to hmac

        await stream.WriteAsync(HmacHash.AsMemory(0, hmacHashLength)); // Replace empty hmac with computed
    }

    public void ComputeAndValidateHmacHash(Stream stream)
    {
        Key = GenerateKey();

        HmacKey = GenerateHmacKey();

        var computedHash = ComputeHmacHash(stream);

        if (!HmacHash.SequenceEqual(computedHash))
        {
            stream.Close();

            throw new IntegrityProtectionException("Invalid signature");
        }

        stream.Seek(metadataLength, SeekOrigin.Begin); // Move position to encrypted data
    }

    public byte GetCryptoVersion()
    {
        return Version[0];
    }

    public long GetFileSize()
    {
        return ByteArrayToLong(Size);
    }

    public int GetMetadataLength()
    {
        return metadataLength;
    }


    private byte[] GenerateRandom(int length)
    {
        var random = RandomNumberGenerator.GetBytes(length);

        return random;
    }

    private byte[] GenerateKey()
    {
        using var deriveBytes = new Rfc2898DeriveBytes(Password, Salt, Iterations, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(keyLength);

        return key;
    }

    private byte[] GenerateHmacKey()
    {
        return SHA512.HashData(Key);
    }

    private byte[] ComputeHmacHash(Stream stream)
    {
        stream.Seek(metadataLength - ivLength, SeekOrigin.Begin); // Move position to (IV + encrypted data)

        using var hmac = new HMACSHA256(HmacKey);
        var hmacHash = hmac.ComputeHash(stream); // IV needs to be part of the MAC calculation

        return hmacHash;
    }

    private byte[] LongToByteArray(long value)
    {
        var result = BitConverter.GetBytes(value);

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(result);
        }

        return result;
    }

    private long ByteArrayToLong(byte[] value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(value);
        }

        try
        {
            return BitConverter.ToInt64(value, 0);
        }
        catch (Exception)
        {
            return -1;
        }
    }
}

