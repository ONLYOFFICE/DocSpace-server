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

namespace ASC.Files.Core;

/// <summary>
/// The authentication data.
/// </summary>
[DebuggerDisplay("{Login} {Password} {RawToken} {Url}")]
public class AuthData(string url = null, string login = null, string password = null, string token = null, string provider = null)
{
    /// <summary>
    /// The authentication login.
    /// </summary>
    /// <example>user@example.com</example>
    public string Login { get; init; } = login ?? string.Empty;

    /// <summary>
    /// The authentication password.
    /// </summary>
    /// <example>p@ssw0rd!</example>
    public string Password { get; init; } = password ?? string.Empty;

    /// <summary>
    /// The authentication raw token.
    /// </summary>
    /// <example>{"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","expires_in":3600}</example>
    public string RawToken { get; init; } = token ?? string.Empty;

    /// <summary>
    /// The authentication URL.
    /// </summary>
    /// <example>https://auth.example.com</example>
    [Url]
    public string Url { get; set; } = url ?? string.Empty;

    /// <summary>
    /// The authentication provider.
    /// </summary>
    /// <example>OAuth2</example>
    public string Provider { get; init; } = provider ?? string.Empty;

    /// <summary>
    /// The authentication token.
    /// </summary>
    /// <example>
    /// {
    ///   "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "refresh_token": "def50200a1b2c3d4e5f6...",
    ///   "expires_in": 3600,
    ///   "client_id": "my-client-id",
    ///   "client_secret": "my-client-secret",
    ///   "redirect_uri": "https://app.example.com/callback",
    ///   "timestamp": "2026-01-01T00:00:00Z"
    /// }
    /// </example>
    public OAuth20Token Token
    {
        get
        {
            return field ??= OAuth20Token.FromJson(RawToken);
        }
        set;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty((Url ?? string.Empty) + (Login ?? string.Empty) + (Password ?? string.Empty) + (RawToken ?? string.Empty));
    }
}