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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The third-party request parameters.
/// </summary>
public class ThirdPartyRequestDto
{
    /// <summary>
    /// The connection URL for the sharepoint.
    /// </summary>
    /// <example>https://example.com</example>
    public string Url { get; set; }

    /// <summary>
    /// The third-party request login.
    /// </summary>
    /// <example>admin</example>
    public string Login { get; set; }

    /// <summary>
    /// The third-party request password.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; }

    /// <summary>
    /// The authentication token.
    /// </summary>
    /// <example>abc123</example>
    public string Token { get; set; }

    /// <summary>
    /// The customer title.
    /// </summary>
    /// <example>My Document</example>
    public required string CustomerTitle { get; set; }

    /// <summary>
    /// The provider key.
    /// </summary>
    /// <example>abc123</example>
    public required string ProviderKey { get; set; }

    /// <summary>
    /// The provider ID.
    /// </summary>
    /// <example>1</example>
    [JsonConverter(typeof(ProviderIdConverter))]
    public int? ProviderId { get; set; }
}

/// <summary>
/// The JSON converter for handling order values in different formats.
/// </summary>
public class ProviderIdConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var providerId))
        {
            return providerId;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var providerIdString = reader.GetString();
            if (string.IsNullOrEmpty(providerIdString))
            {
                return null;
            }

            if (int.TryParse(providerIdString, out var result))
            {
                return result;
            }
        }

        throw new ArgumentException("providerId");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
