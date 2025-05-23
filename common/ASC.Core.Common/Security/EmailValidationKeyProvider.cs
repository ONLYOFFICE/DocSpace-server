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

namespace ASC.Security.Cryptography;

/// <summary>
/// The confirmation result.
/// </summary>
[Scope]
public class EmailValidationKeyProvider
{
    public enum ValidationResult
    {
        [SwaggerEnum("Ok")]
        Ok,

        [SwaggerEnum("Invalid")]
        Invalid,

        [SwaggerEnum("Expired")]
        Expired,

        [SwaggerEnum("Tariff limit")]
        TariffLimit,

        [SwaggerEnum("User existed")]
        UserExisted,

        [SwaggerEnum("User excluded")]
        UserExcluded,

        [SwaggerEnum("Quota failed")]
        QuotaFailed
    }

    public TimeSpan ValidEmailKeyInterval { get; }
    public TimeSpan ValidAuthKeyInterval { get; }
    public TimeSpan ValidVisitLinkInterval { get; }

    private readonly ILogger<EmailValidationKeyProvider> _logger;
    private static readonly DateTime _from = new(2010, 01, 01, 0, 0, 0, DateTimeKind.Utc);
    private readonly MachinePseudoKeys _machinePseudoKeys;
    private readonly TenantManager _tenantManager;

    public EmailValidationKeyProvider(MachinePseudoKeys machinePseudoKeys, TenantManager tenantManager, IConfiguration configuration, ILogger<EmailValidationKeyProvider> logger)
    {
        _machinePseudoKeys = machinePseudoKeys;
        _tenantManager = tenantManager;
        if (!TimeSpan.TryParse(configuration["email:validinterval"], out var validInterval))
        {
            validInterval = TimeSpan.FromDays(7);
        }
        if (!TimeSpan.TryParse(configuration["auth:validinterval"], out var authValidInterval))
        {
            authValidInterval = TimeSpan.FromHours(1);
        }
        if (!TimeSpan.TryParse(configuration["visit:validinterval"], out var validVisitLinkInterval))
        {
            validVisitLinkInterval = TimeSpan.FromMinutes(15);
        }
       
        ValidEmailKeyInterval = validInterval;
        ValidAuthKeyInterval = authValidInterval;
        ValidVisitLinkInterval = validVisitLinkInterval;
        _logger = logger;
    }
    

    public string GetEmailKey(string email, int? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);

        email = FormatEmail(tenantId ?? _tenantManager.GetCurrentTenantId(), email);

        var ms = (long)(DateTime.UtcNow - _from).TotalMilliseconds;
        var hash = GetMachineHashedData(BitConverter.GetBytes(ms), Encoding.ASCII.GetBytes(email));

        return $"{ms}.{DoStringFromBytes(hash)}";
    }

    private string FormatEmail(int tenantId, string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            return $"{email.ToLowerInvariant()}|{tenantId}|{Encoding.UTF8.GetString(_machinePseudoKeys.GetMachineConstant())}";
        }
        catch (Exception e)
        {
            _logger.CriticalFormatEmail(e);

            return email.ToLowerInvariant();
        }
    }

    public ValidationResult ValidateEmailKey(string email, string key)
    {
        return ValidateEmailKey(email, key, TimeSpan.MaxValue);
    }

    public ValidationResult ValidateEmailKey(string email, string key, TimeSpan validInterval)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var result = ValidateEmailKey(email, key, validInterval, tenantId);
        _logger.DebugValidationResult(result, email, key, validInterval, tenantId);
        return result;
    }

    private ValidationResult ValidateEmailKey(string email, string key, TimeSpan validInterval, int tenantId)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentNullException.ThrowIfNull(key);

        email = FormatEmail(tenantId, email);
        var parts = key.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return ValidationResult.Invalid;
        }

        if (!long.TryParse(parts[0], out var ms))
        {
            return ValidationResult.Invalid;
        }

        var hash = GetMachineHashedData(BitConverter.GetBytes(ms), Encoding.ASCII.GetBytes(email));
        var key2 = DoStringFromBytes(hash);
        var key2_good = string.Equals(parts[1], key2, StringComparison.OrdinalIgnoreCase);
        if (!key2_good)
        {
            return ValidationResult.Invalid;
        }

        var ms_current = (long)(DateTime.UtcNow - _from).TotalMilliseconds;

        return validInterval >= TimeSpan.FromMilliseconds(ms_current - ms) ? ValidationResult.Ok : ValidationResult.Expired;
    }

    private static string DoStringFromBytes(byte[] data)
    {
        var str = Convert.ToBase64String(data);
        str = str.Replace("=", "").Replace("+", "").Replace("/", "").Replace("\\", "");

        return str.ToUpperInvariant();
    }

    private static byte[] GetMachineHashedData(byte[] salt, byte[] data)
    {
        var allData = new byte[salt.Length + data.Length];
        Array.Copy(data, allData, data.Length);
        Array.Copy(salt, 0, allData, data.Length, salt.Length);

        return Hasher.Hash(allData, HashAlg.SHA256);
    }
}

/// <summary>
/// The confirmation email parameters.
/// </summary>
public class EmailValidationKeyModel
{
    /// <summary>
    /// The email validation key. 
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The user type.
    /// </summary>
    public EmployeeType? EmplType { get; init; }

    /// <summary>
    /// The email address.
    /// </summary>
    [EmailAddress]
    public string Email { get; init; }

    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid? UiD { get; init; }

    /// <summary>
    /// The confirmation email type.
    /// </summary>
    public ConfirmType? Type { get; init; }

    /// <summary>
    /// Specifies whether it is the first time account access or not.
    /// </summary>
    public string First { get; init; }

    /// <summary>
    /// The room ID.
    /// </summary>
    public string RoomId { get; init; }

    public void Deconstruct(out string key, out EmployeeType? emplType, out string email, out Guid? uiD, out ConfirmType? type, out string first)
    {
        (key, emplType, email, uiD, type, first) = (Key, EmplType, Email, UiD, Type, First);
    }
}