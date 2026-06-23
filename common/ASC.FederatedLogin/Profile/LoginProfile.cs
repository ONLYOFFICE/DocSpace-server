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

using System.Diagnostics.CodeAnalysis;

namespace ASC.FederatedLogin.Profile;

[DebuggerDisplay("{DisplayName} ({Id})")]
public class LoginProfile
{
    public string LinkId { get; init; }

    public LoginProfile() { }

    public LoginProfile([NotNull] string serialized, string linkId = null)
    {
        ArgumentNullException.ThrowIfNull(serialized);

        _fields = serialized.Split(PairSeparator).ToDictionary(x => x.Split(KeyValueSeparator)[0], y => y.Split(KeyValueSeparator)[1]);
        LinkId = linkId;
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
        internal set => SetField(WellKnownFields.Email, value);
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
    public async Task<string> ToString(LoginProfile profile, bool pureTransport = false)
    {
        var tenantId = pureTransport ? null : tenantManager.GetCurrentTenant(false)?.Id;
        var input = await instanceCrypto.EncryptAsync(Encoding.UTF8.GetBytes(profile.ToString() + tenantId));
        return WebEncoders.Base64UrlEncode(input);
    }

    public async Task<LoginProfile> FromTransport(string transportString)
    {
        var serialized = await instanceCrypto.DecryptAsync(WebEncoders.Base64UrlDecode(transportString));
        var tenantId = tenantManager.GetCurrentTenantId();
        return new LoginProfile(serialized.Substring(0, serialized.LastIndexOf(tenantId.ToString(), StringComparison.Ordinal)));
    }

    public async Task<LoginProfile> FromPureTransport(string transportString)
    {
        var serialized = await instanceCrypto.DecryptAsync(WebEncoders.Base64UrlDecode(transportString));
        return new LoginProfile(serialized);
    }
}