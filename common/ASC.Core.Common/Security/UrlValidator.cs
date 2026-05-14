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

namespace ASC.Core.Common.Security;

/// <summary>
/// Validates URLs to prevent SSRF (Server-Side Request Forgery) attacks.
/// </summary>
public interface IUrlValidator
{
    /// <summary>
    /// Validates a URL against SSRF attack vectors.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="options">Validation options. If null, default options are used.</param>
    /// <returns>Validation result containing status, error message, and resolved IP addresses.</returns>
    Task<UrlValidationResult> ValidateAsync(string url, UrlValidationOptions options = null);
}

/// <summary>
/// Options for URL validation.
/// </summary>
public class UrlValidationOptions
{
    /// <summary>
    /// Require HTTPS scheme for the URL.
    /// Default: true.
    /// </summary>
    public bool RequireHttps { get; init; } = true;

    /// <summary>
    /// Additional custom blacklist rules to apply for this validation request only.
    /// These rules are merged with the global configuration blacklist.
    /// Supports IP addresses, CIDR ranges (e.g., "10.0.0.0/8"), and IP ranges (e.g., "192.168.1.1-192.168.1.254").
    /// Default: null (no additional rules).
    /// </summary>
    public string[] CustomBlacklist { get; init; }
}

/// <summary>
/// Result of URL validation.
/// </summary>
/// <param name="IsValid">True if URL is valid and safe to use.</param>
/// <param name="ErrorMessage">Error message if validation failed.</param>
/// <param name="Blacklisted">True if URL resolves to a blacklisted IP address.</param>
/// <param name="ParsedUri">Parsed URI object if validation succeeded.</param>
public record UrlValidationResult(bool IsValid, string ErrorMessage = null, bool Blacklisted = false, Uri ParsedUri = null);

[Singleton]
public class UrlValidator(
    IConfiguration configuration,
    ILogger<UrlValidator> logger) : IUrlValidator
{
    private static readonly string[] _defaultBlacklist =
    [
        "0.0.0.0/8",
        "10.0.0.0/8",
        "100.64.0.0/10",
        "127.0.0.0/8",
        "169.254.0.0/16",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "::/128",
        "::1/128",
        "fc00::/8",
        "fd00::/8",
        "fe80::/10"
    ];

    private readonly string[] _blacklist = configuration.GetSection("webhooks:blacklist").Get<string[]>() ?? _defaultBlacklist;

    public async Task<UrlValidationResult> ValidateAsync(string url, UrlValidationOptions options = null)
    {
        options ??= new UrlValidationOptions();

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.WarningInvalidUrlFormat(url ?? "(null)");
            return new UrlValidationResult(false, "URL is null or empty");
        }

        // 1. Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri))
        {
            logger.WarningInvalidUrlFormat(url);
            return new UrlValidationResult(false, "Invalid URL format");
        }

        // 2. Validate URL scheme - only http and https are allowed
        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            logger.WarningSsrfBlocked(url, parsedUri.Host, "N/A", $"Invalid scheme: {parsedUri.Scheme}");
            return new UrlValidationResult(false, $"Invalid URL scheme: {parsedUri.Scheme}. Only http and https are allowed");
        }

        // 3. Check HTTPS requirement
        if (options.RequireHttps && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            logger.WarningSsrfBlocked(url, parsedUri.Host, "N/A", "HTTPS required");
            return new UrlValidationResult(false, "HTTPS is required");
        }

        // 4. Check if the host is a direct IP address and validate against blacklist
        if (IPAddress.TryParse(parsedUri.Host, out var directIp))
        {
            if (IsBlacklisted([directIp], options.CustomBlacklist, out var blockedIp))
            {
                logger.WarningSsrfBlocked(url, parsedUri.Host, blockedIp.ToString(), "Direct IP in blacklist");
                return new UrlValidationResult(false, "URL host is in the blacklist", true);
            }

            return new UrlValidationResult(true, ParsedUri: parsedUri);
        }

        // 5. Resolve DNS and validate all resolved IP addresses
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(parsedUri.Host, cts.Token);
        }
        catch (Exception ex)
        {
            logger.ErrorDnsResolutionFailed(parsedUri.Host, ex);
            return new UrlValidationResult(false, $"DNS resolution failed for {parsedUri.Host}");
        }

        if (addresses.Length == 0)
        {
            logger.ErrorDnsResolutionFailed(parsedUri.Host, new InvalidOperationException("No addresses returned"));
            return new UrlValidationResult(false, $"DNS resolution returned no addresses for {parsedUri.Host}");
        }

        // 6. Check all resolved addresses against the blacklist
        if (IsBlacklisted(addresses, options.CustomBlacklist, out var blockedAddress))
        {
            logger.WarningSsrfBlocked(url, parsedUri.Host, blockedAddress.ToString(), "Resolved IP in blacklist");
            return new UrlValidationResult(false, "URL resolves to a blacklisted IP address", true);
        }

        return new UrlValidationResult(true, ParsedUri: parsedUri);
    }

    private bool IsBlacklisted(IPAddress[] addresses, string[] customBlacklist, out IPAddress blockedAddress)
    {
        blockedAddress = null;

        var allRestrictions = _blacklist;
        if (customBlacklist is { Length: > 0 })
        {
            allRestrictions = [.. _blacklist, .. customBlacklist];
        }

        foreach (var address in addresses)
        {
            foreach (var restriction in allRestrictions)
            {
                if (IPAddressRange.MatchIPs(address.ToString(), restriction))
                {
                    blockedAddress = address;
                    return true;
                }
            }
        }

        return false;
    }
}
