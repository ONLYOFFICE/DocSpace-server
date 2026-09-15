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

namespace ASC.FederatedLogin;

/// <summary>
/// The OAuth 2.0 token issued by a third-party provider.
/// </summary>
[DebuggerDisplay("{AccessToken} (expired: {IsExpired})")]
public class OAuth20Token
{
    /// <summary>The token sent to the provider with every request made on behalf of the account.</summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// The token used to obtain a new access token when the current one expires. A provider that issues no refresh
    /// token leaves it empty, and the account then has to be connected again to keep working.
    /// </summary>
    /// <example>def50200a1b2c3d4e5f6...</example>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    /// <summary>
    /// How long the access token stays usable, in seconds counted from `timestamp`. Zero means the provider did not
    /// say, and the token is then treated as expired.
    /// </summary>
    /// <example>3600</example>
    [JsonPropertyName("expires_in")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ExpiresIn { get; set; }

    /// <summary>The OAuth 2.0 client ID of the application the token was issued to.</summary>
    /// <example>my-client-id</example>
    [JsonPropertyName("client_id")]
    public string ClientID { get; set; }

    /// <summary>
    /// The client secret of the application the token was issued to, needed when the token is refreshed.
    /// </summary>
    /// <example>my-client-secret</example>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; }

    /// <summary>
    /// The redirect URL the authorization code behind this token was obtained with; providers require the same value
    /// again when the token is refreshed.
    /// </summary>
    /// <example>https://app.example.com/callback</example>
    [Url]
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; }

    /// <summary>When the token was issued, in UTC. This is the point `expires_in` is counted from.</summary>
    /// <example>2026-01-01T00:00:00Z</example>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Origin json
    /// </summary>
    [JsonIgnore]
    public string OriginJson { get; set; }

    /// <summary>
    /// Whether the access token can no longer be used and has to be refreshed. It is also true when the provider did
    /// not say how long the token lives.
    /// </summary>
    /// <example>false</example>
    public bool IsExpired
    {
        get
        {
            if (!ExpiresIn.Equals(0))
            {
                return DateTime.UtcNow > Timestamp + TimeSpan.FromSeconds(ExpiresIn);
            }

            return true;
        }
    }

    public OAuth20Token() { }

    public OAuth20Token(OAuth20Token oAuth20Token)
    {
        Copy(oAuth20Token);
    }

    public OAuth20Token(string json)
    {
        Copy(FromJson(json));
    }

    public static OAuth20Token FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<OAuth20Token>(json);

            if (result.Timestamp == default)
            {
                result.Timestamp = DateTime.UtcNow;
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public override string ToString()
    {
        return AccessToken;
    }

    private void Copy(OAuth20Token oAuth20Token)
    {
        if (oAuth20Token == null)
        {
            return;
        }

        AccessToken = oAuth20Token.AccessToken;
        RefreshToken = oAuth20Token.RefreshToken;
        ExpiresIn = oAuth20Token.ExpiresIn;
        ClientID = oAuth20Token.ClientID;
        ClientSecret = oAuth20Token.ClientSecret;
        RedirectUri = oAuth20Token.RedirectUri;
        Timestamp = oAuth20Token.Timestamp;
    }
}