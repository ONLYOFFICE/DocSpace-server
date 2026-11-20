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

namespace ASC.Site.Core.Classes;

[Scope]
public class ValidationKeyProvider(
    ICache cache,
    InstanceCrypto instanceCrypto,
    EmailValidationKeyProvider emailValidationKeyProvider)
{
    private static readonly TimeSpan _validInterval = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions _subscriptionIdJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string GetKey(string value)
    {
        return emailValidationKeyProvider.GetEmailKey(value, Tenant.DefaultTenant);
    }

    public KeyValidationResult ValidateKey(string cacheKey, string publickey, string secretkey)
    {
        var validationResult = emailValidationKeyProvider.ValidateEmailKey(publickey, secretkey, _validInterval, Tenant.DefaultTenant);

        if (validationResult != EmailValidationKeyProvider.ValidationResult.Ok)
        {
            return validationResult == EmailValidationKeyProvider.ValidationResult.Expired
                ? KeyValidationResult.Expired
                : KeyValidationResult.Invalid;
        }

        if (cache.Get<string>(cacheKey) != null)
        {
            return KeyValidationResult.AlreadyUsed; // single use only
        }

        cache.Insert(cacheKey, validationResult.ToString(), _validInterval);

        return KeyValidationResult.Ok;
    }

    public async Task<string> EncryptAndEncode(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var encryptedBytes = await instanceCrypto.EncryptAsync(bytes);
        var encoded = UrlTokenEncode(encryptedBytes);
        return encoded;
    }

    public async Task<string> DecodeAndDecrypt(string data)
    {
        var decoded = UrlTokenDecode(data);
        var decrypted = await instanceCrypto.DecryptAsync(decoded);
        return decrypted;
    }

    /// <summary>
    /// The longest subscription id read; a longer one is refused before it is decoded. Nothing limits the name the
    /// site puts into a request id, so the bound leaves room: a request with an address of 100 characters and a name of
    /// 64 characters, every one of them \u-escaped (6 bytes), takes under 900 characters (pinned by a test), and 4096
    /// characters hold about 3 000 bytes of JSON.
    /// </summary>
    public const int SubscriptionIdMaxLength = 4096;

    /// <summary>
    /// Reads the id of a subscription link: the address and, for a subscription request, the name and the list.
    /// </summary>
    /// <remarks>
    /// Two kinds of id are accepted, both the text encrypted with <see cref="InstanceCrypto"/> and UrlToken-encoded
    /// (<see cref="EncryptAndEncode"/>): the <c>emailKey</c> of <c>/key/generate</c> (the address) and the
    /// <c>unsubscribeId</c> of <c>/key/generateunsubscribeid</c> (the address, or the JSON <c>{email, firstname, type}</c>
    /// the site puts there for a subscription request).
    /// The encryption carries no MAC. A forged or spliced id now and then decrypts without an error, and the address
    /// check refuses most of those but not all.
    /// </remarks>
    /// <returns>The data of the id, or <see langword="null"/> when the id is longer than
    /// <see cref="SubscriptionIdMaxLength"/>, does not decrypt, or holds no valid address.</returns>
    public async Task<SubscriptionId> DecodeSubscriptionIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > SubscriptionIdMaxLength)
        {
            return null;
        }

        string text;

        try
        {
            var bytes = UrlTokenDecode(id);

            if (bytes is not { Length: > 0 })
            {
                return null;
            }

            text = await instanceCrypto.DecryptAsync(bytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            // Not a UrlToken string, or not encrypted with this installation's key.
            return null;
        }

        return ParseSubscriptionId(text);
    }

    /// <summary>
    /// Reads the decrypted text of a subscription id: an address, or the JSON <c>{email, firstname, type}</c>.
    /// </summary>
    /// <returns>
    /// The data with the address trimmed and lowercased, or <see langword="null"/> when the text holds no valid address
    /// of at most <see cref="DbTemplateUnsubscribe.EmailMaxLength"/> characters or the JSON does not parse.
    /// A JSON whose <c>type</c> is not a <see cref="SubscribeType"/> still gives the address, with
    /// <see cref="SubscriptionId.HasUnknownType"/> set: an unsubscription does not need the list.
    /// </returns>
    public static SubscriptionId ParseSubscriptionId(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();

        if (!text.StartsWith('{'))
        {
            var plainEmail = NormalizeEmail(text);

            return plainEmail == null ? null : new SubscriptionId(plainEmail, null, null);
        }

        SubscriptionIdPayload payload;

        try
        {
            payload = JsonSerializer.Deserialize<SubscriptionIdPayload>(text, _subscriptionIdJsonOptions);
        }
        catch (JsonException)
        {
            // Text that only starts like JSON: not an id this service issued.
            return null;
        }

        var email = NormalizeEmail(payload?.Email);

        if (email == null)
        {
            return null;
        }

        var typeIsKnown = TryReadSubscribeType(payload.Type, out var type);

        return new SubscriptionId(email, payload.FirstName?.Trim(), type, IsSubscriptionRequest: true, HasUnknownType: !typeIsKnown);
    }

    /// <summary>
    /// Trims and lowercases an address, and refuses one that the <c>email</c> column cannot hold: longer than
    /// <see cref="DbTemplateUnsubscribe.EmailMaxLength"/>, with a character outside the Basic Multilingual Plane
    /// (the Workspace tables are <c>utf8mb3_general_ci</c> by their DDL) or a control character, or not an address at all.
    /// </summary>
    private static string NormalizeEmail(string email)
    {
        email = email?.Trim().ToLowerInvariant();

        if (email is not { Length: <= DbTemplateUnsubscribe.EmailMaxLength }
            || email.Any(c => char.IsSurrogate(c) || char.IsControl(c)))
        {
            return null;
        }

        return email.TestEmailRegex() ? email : null;
    }

    private static bool TryReadSubscribeType(JsonElement? element, out SubscribeType? type)
    {
        type = null;

        switch (element?.ValueKind)
        {
            case null:
            case JsonValueKind.Null:
                return true;
            case JsonValueKind.Number when element.Value.TryGetInt32(out var number) && Enum.IsDefined((SubscribeType)number):
                type = (SubscribeType)number;
                return true;
            case JsonValueKind.String when Enum.TryParse<SubscribeType>(element.Value.GetString(), true, out var parsed) && Enum.IsDefined(parsed):
                type = parsed;
                return true;
            default:
                return false;
        }
    }

    // source code from
    // https://github.com/microsoft/referencesource/blob/master/System.Web/Util/HttpEncoder.cs

    private static string UrlTokenEncode(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length < 1)
        {
            return string.Empty;
        }

        ////////////////////////////////////////////////////////
        // Step 1: Do a Base64 encoding
        var base64Str = Convert.ToBase64String(input);

        ////////////////////////////////////////////////////////
        // Step 2: Find how many padding chars are present in the end
        int endPos;
        for (endPos = base64Str.Length; endPos > 0; endPos--)
        {
            if (base64Str[endPos - 1] != '=') // Found a non-padding char!
            {
                break; // Stop here
            }
        }

        ////////////////////////////////////////////////////////
        // Step 3: Create char array to store all non-padding chars,
        //      plus a char to indicate how many padding chars are needed
        var base64Chars = new char[endPos + 1];
        base64Chars[endPos] = (char)('0' + base64Str.Length - endPos); // Store a char at the end, to indicate how many padding chars are needed

        ////////////////////////////////////////////////////////
        // Step 3: Copy in the other chars. Transform the "+" to "-", and "/" to "_"
        for (var iter = 0; iter < endPos; iter++)
        {
            var c = base64Str[iter];

            base64Chars[iter] = c switch
            {
                '+' => '-',
                '/' => '_',
                _ => c
            };
        }

        return new string(base64Chars);
    }

    private static byte[] UrlTokenDecode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var len = input.Length;
        if (len < 1)
        {
            return [];
        }

        ///////////////////////////////////////////////////////////////////
        // Step 1: Calculate the number of padding chars to append to this string.
        //         The number of padding chars to append is stored in the last char of the string.
        var numPadChars = input[len - 1] - '0';
        if (numPadChars is < 0 or > 10)
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////
        // Step 2: Create array to store the chars (not including the last char)
        //          and the padding chars
        var base64Chars = new char[len - 1 + numPadChars];

        ////////////////////////////////////////////////////////
        // Step 3: Copy in the chars. Transform the "-" to "+", and "_" to "/"
        for (var iter = 0; iter < len - 1; iter++)
        {
            var c = input[iter];

            base64Chars[iter] = c switch
            {
                '-' => '+',
                '_' => '/',
                _ => c
            };
        }

        ////////////////////////////////////////////////////////
        // Step 4: Add padding chars
        for (var iter = len - 1; iter < base64Chars.Length; iter++)
        {
            base64Chars[iter] = '=';
        }

        // Do the actual conversion
        return Convert.FromBase64CharArray(base64Chars, 0, base64Chars.Length);
    }

    private sealed record SubscriptionIdPayload(string Email, string FirstName, JsonElement? Type);
}

/// <summary>The data of a subscription link id (<see cref="ValidationKeyProvider.DecodeSubscriptionIdAsync"/>).</summary>
/// <param name="Email">The address, trimmed and lowercased.</param>
/// <param name="FirstName">The name from a subscription request; <see langword="null"/> for a plain address.</param>
/// <param name="Type">The list from a subscription request; <see langword="null"/> when the id names none or an unknown one.</param>
/// <param name="IsSubscriptionRequest">Whether the id is the JSON of a subscription request, not a plain address.</param>
/// <param name="HasUnknownType">Whether the request names a list that is not a <see cref="SubscribeType"/>.</param>
public sealed record SubscriptionId(string Email, string FirstName, SubscribeType? Type, bool IsSubscriptionRequest = false, bool HasUnknownType = false);
