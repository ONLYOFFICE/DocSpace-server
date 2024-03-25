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

using System.Diagnostics.CodeAnalysis;

namespace ASC.FederatedLogin.Profile;

[DebuggerDisplay("{DisplayName} ({Id})")]
public class LoginProfile
{
    public const string QueryParamName = "up";
    public const string QuerySessionParamName = "sup";
    public const string QueryCacheParamName = "cup";

    public LoginProfile()
    {
        
    }

    public LoginProfile([NotNull]string serialized)
    {
        ArgumentNullException.ThrowIfNull(serialized);

        _fields = serialized.Split(PairSeparator).ToDictionary(x => x.Split(KeyValueSeparator)[0], y => y.Split(KeyValueSeparator)[1]);
    }
    
    public LoginProfile(Exception e)
    {
        AuthorizationError = e.Message;
    }
    
    public string Id
    {
        get => GetField(WellKnownFields.Id);
        internal init => SetField(WellKnownFields.Id, value);
    }

    public string Link
    {
        get => GetField(WellKnownFields.Link);
        internal set => SetField(WellKnownFields.Link, value);
    }

    public string Name
    {
        get => GetField(WellKnownFields.Name);
        internal init => SetField(WellKnownFields.Name, value);
    }

    public string DisplayName
    {
        get => GetField(WellKnownFields.DisplayName);
        internal set => SetField(WellKnownFields.DisplayName, value);
    }

    public string EMail
    {
        get => GetField(WellKnownFields.Email);
        internal set { SetField(WellKnownFields.Email, value); }
    }

    public string Avatar
    {
        get => GetField(WellKnownFields.Avatar);
        internal init => SetField(WellKnownFields.Avatar, value);
    }

    public string Gender
    {
        get => GetField(WellKnownFields.Gender);
        internal set => SetField(WellKnownFields.Gender, value);
    }

    public string FirstName
    {
        get => GetField(WellKnownFields.FirstName);
        internal set => SetField(WellKnownFields.FirstName, value);
    }

    public string LastName
    {
        get => GetField(WellKnownFields.LastName);
        internal set => SetField(WellKnownFields.LastName, value);
    }

    public string MiddleName
    {
        get => GetField(WellKnownFields.MiddleName);
        internal set => SetField(WellKnownFields.MiddleName, value);
    }

    public string Salutation
    {
        get => GetField(WellKnownFields.Salutation);
        internal set => SetField(WellKnownFields.Salutation, value);
    }

    public string BirthDay
    {
        get => GetField(WellKnownFields.BirthDay);
        internal set => SetField(WellKnownFields.BirthDay, value);
    }

    public string Locale
    {
        get => GetField(WellKnownFields.Locale);
        internal init => SetField(WellKnownFields.Locale, value);
    }

    public string TimeZone
    {
        get => GetField(WellKnownFields.Timezone);
        internal set => SetField(WellKnownFields.Timezone, value);
    }

    public string AuthorizationResult
    {
        get => GetField(WellKnownFields.Auth);
        internal set => SetField(WellKnownFields.Auth, value);
    }

    public string AuthorizationError
    {
        get => GetField(WellKnownFields.AuthError);
        private init => SetField(WellKnownFields.AuthError, value);
    }

    public string Provider
    {
        get => GetField(WellKnownFields.Provider);
        internal init => SetField(WellKnownFields.Provider, value);
    }

    public string RealmUrl
    {
        get => GetField(WellKnownFields.RealmUrl);
        internal set => SetField(WellKnownFields.RealmUrl, value);
    }

    private string UniqueId => $"{Provider}/{Id}";
    public string HashId => HashHelper.MD5(UniqueId);

    private const char KeyValueSeparator = '→';
    private const char PairSeparator = '·';

    private readonly Dictionary<string, string> _fields = new();
    
    private string GetField(string name)
    {
        return _fields.TryGetValue(name, out var field) ? field : string.Empty;
    }

    private void SetField(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!string.IsNullOrEmpty(value))
        {
            _fields[name] = value;
        }
        else
        {
            _fields.Remove(name);
        }
    }

    public override string ToString()
    {
        return string.Join(new string(PairSeparator, 1), _fields.Select(x => string.Join(new string(KeyValueSeparator, 1), x.Key, x.Value)).ToArray());
    }
}

[Scope]
public class LoginProfileTransport(InstanceCrypto instanceCrypto, TenantManager tenantManager)
{
    public async Task<string> ToString(LoginProfile profile)
    {
        return WebEncoders.Base64UrlEncode(instanceCrypto.Encrypt(Encoding.UTF8.GetBytes(profile.ToString() + await tenantManager.GetCurrentTenantIdAsync())));
    }

    public async Task<LoginProfile> FromTransport(string transportString)
    {
        var serialized = instanceCrypto.Decrypt(WebEncoders.Base64UrlDecode(transportString));
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        return new LoginProfile(serialized.Substring(0, serialized.LastIndexOf(tenantId.ToString(), StringComparison.Ordinal)));
    }
}