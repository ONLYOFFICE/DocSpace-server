// (c) Copyright Ascensio System SIA 2009-2025
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
    public string Regex { get; }

    /// <summary>
    /// The minimum length of the valid domain.
    /// </summary>
    public int MinLength { get; }

    /// <summary>
    /// The maximum length of the valid domain.
    /// </summary>
    [SwaggerSchemaCustom(Example = 63)]
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
