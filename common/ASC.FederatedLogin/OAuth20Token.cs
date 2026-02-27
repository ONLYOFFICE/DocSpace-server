// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.FederatedLogin;

[DebuggerDisplay("{AccessToken} (expired: {IsExpired})")]
public class OAuth20Token
{
    /// <summary>
    /// Access token
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    /// <example>def50200a1b2c3d4e5f6...</example>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    /// <summary>
    /// Expires in
    /// </summary>
    /// <example>3600</example>
    [JsonPropertyName("expires_in")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ExpiresIn { get; set; }

    /// <summary>
    /// Client id
    /// </summary>
    /// <example>my-client-id</example>
    [JsonPropertyName("client_id")]
    public string ClientID { get; set; }

    /// <summary>
    /// Client secret
    /// </summary>
    /// <example>my-client-secret</example>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; }

    /// <summary>
    /// Redirect uri
    /// </summary>
    /// <example>https://app.example.com/callback</example>
    [Url]
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    /// <example>2026-01-01T00:00:00Z</example>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Origin json
    /// </summary>
    [JsonIgnore]
    public string OriginJson { get; set; }

    /// <summary>
    /// Is expired
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