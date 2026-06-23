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

namespace ASC.Core.Tenants;

/// <summary>
/// The domain validator.
/// </summary>
[Singleton]
public class TenantDomainValidator
{
    private readonly Regex _validDomain;
    private readonly Regex _validName;
    private const string DomainContainsInvalidCharacters = "Domain contains invalid characters.";

    /// <summary>
    /// The regex string to validate a domain.
    /// </summary>
    /// <example>^[a-z0-9]([a-z0-9-]){1,61}[a-z0-9]$</example>
    public string Regex { get; }

    /// <summary>
    /// The minimum length of the valid domain.
    /// </summary>
    /// <example>6</example>
    public int MinLength { get; }

    /// <summary>
    /// The maximum length of the valid domain.
    /// </summary>
    /// <example>63</example>
    public int MaxLength { get; }

    public TenantDomainValidator()
    {

    }

    public TenantDomainValidator(IConfiguration configuration, CoreBaseSettings coreBaseSettings)
    {
        MaxLength = 63;

        if (int.TryParse(configuration["web:alias:max"], out var defaultMaxLength))
        {
            MaxLength = Math.Max(3, Math.Min(MaxLength, defaultMaxLength));
        }

        MinLength = 6;

        if (int.TryParse(configuration["web:alias:min"], out var defaultMinLength))
        {
            MinLength = Math.Max(1, Math.Min(MaxLength, defaultMinLength));
        }

        Regex = $"^[a-z0-9]([a-z0-9-]){{1,{MaxLength - 2}}}[a-z0-9]$";

        var regexpFromConfig = configuration["web:alias:regex"];
        if (!string.IsNullOrEmpty(regexpFromConfig))
        {
            Regex = regexpFromConfig;
        }

        _validDomain = new Regex(Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        var nameRegexpFromConfig = configuration["core:tenantname:regex"];
        if (!coreBaseSettings.Standalone && !string.IsNullOrEmpty(nameRegexpFromConfig))
        {
            _validName = new Regex(nameRegexpFromConfig, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }
    }

    public void ValidateDomainLength(string domain)
    {
        if (string.IsNullOrEmpty(domain)
            || domain.Length < MinLength || MaxLength < domain.Length)
        {
            throw new TenantTooShortException("The domain name must be between " + MinLength + " and " + MaxLength + " characters long.", MinLength, MaxLength);
        }
    }

    public void ValidateDomainCharacters(string domain)
    {
        if (!_validDomain.IsMatch(domain) || domain.TestPunnyCode())
        {
            throw new TenantIncorrectCharsException(DomainContainsInvalidCharacters);
        }
    }

    public void ValidateTenantName(string name)
    {
        if (!string.IsNullOrEmpty(name) && _validName != null && !_validName.IsMatch(name))
        {
            throw new TenantIncorrectCharsException("Name contains invalid characters.");
        }
    }
}