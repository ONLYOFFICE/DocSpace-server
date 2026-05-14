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

namespace ASC.Core.Common.Tests;

public class UrlValidatorTests
{
    private readonly IUrlValidator _urlValidator;

    public UrlValidatorTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = NullLogger<UrlValidator>.Instance;

        _urlValidator = new UrlValidator(configuration, logger);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.100")]
    [InlineData("127.1.1.1")]
    public async Task ValidateAsync_LocalhostIpv4_ShouldBlock(string ipAddress)
    {
        var url = $"http://{ipAddress}/test";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_LocalhostIpv6_ShouldBlock()
    {
        var url = "http://[::1]/test";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("10.1.2.3")]
    public async Task ValidateAsync_PrivateIpClass10_ShouldBlock(string ipAddress)
    {
        var url = $"https://{ipAddress}/api";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Theory]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("172.20.10.5")]
    public async Task ValidateAsync_PrivateIpClass172_ShouldBlock(string ipAddress)
    {
        var url = $"https://{ipAddress}/api";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("192.168.0.1")]
    [InlineData("192.168.255.255")]
    public async Task ValidateAsync_PrivateIpClass192_ShouldBlock(string ipAddress)
    {
        var url = $"https://{ipAddress}/api";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CloudMetadataAws_ShouldBlock()
    {
        var url = "http://169.254.169.254/latest/meta-data/";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Theory]
    [InlineData("169.254.1.1")]
    [InlineData("169.254.100.200")]
    public async Task ValidateAsync_LinkLocal_ShouldBlock(string ipAddress)
    {
        var url = $"http://{ipAddress}/";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://google.com/test")]
    [InlineData("https://api.github.com")]
    public async Task ValidateAsync_ValidHttpsUrl_ShouldAllow(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ParsedUri.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_HttpWithRequireHttps_ShouldBlock()
    {
        var url = "http://example.com/test";
        var options = new UrlValidationOptions
        {
            RequireHttps = true
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HTTPS is required");
    }

    [Fact]
    public async Task ValidateAsync_HttpWithoutRequireHttps_ShouldAllow()
    {
        var url = "http://example.com/test";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeTrue();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("file://C:/Windows/System32/config/sam")]
    public async Task ValidateAsync_FileScheme_ShouldBlock(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid URL scheme");
    }

    [Theory]
    [InlineData("ftp://ftp.example.com")]
    [InlineData("ftps://secure.example.com")]
    public async Task ValidateAsync_FtpScheme_ShouldBlock(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid URL scheme");
    }

    [Theory]
    [InlineData("gopher://gopher.example.com")]
    [InlineData("telnet://telnet.example.com")]
    [InlineData("ldap://ldap.example.com")]
    [InlineData("htp://typo.example.com")]
    public async Task ValidateAsync_UnsupportedSchemes_ShouldBlock(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid URL scheme");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_InvalidUrl_ShouldBlock(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("null or empty");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("://missing-scheme.com")]
    public async Task ValidateAsync_MalformedUrl_ShouldBlock(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid URL format");
    }

    [Fact]
    public async Task ValidateAsync_DnsResolutionFailed_ShouldBlock()
    {
        var url = "https://this-domain-definitely-does-not-exist-12345.com";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DNS resolution failed");
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklist_ShouldUseConfiguredRanges()
    {
        var customBlacklist = new[] { "203.0.113.0/24" }; // TEST-NET-3

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = NullLogger<UrlValidator>.Instance;

        var validator = new UrlValidator(configuration, logger);

        var url = "https://203.0.113.50/api";
        var options = new UrlValidationOptions
        {
            CustomBlacklist = customBlacklist
        };

        var result = await validator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_DirectIpNotInBlacklist_ShouldAllow()
    {
        // Public IP that should not be blacklisted (Cloudflare DNS)
        var url = "https://1.1.1.1/";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.ParsedUri.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_DefaultOptions_ShouldRequireHttps()
    {
        var url = "http://example.com/api";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HTTPS is required");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://api.github.com/repos")]
    public async Task ValidateAsync_ValidUrl_ShouldReturnParsedUri(string url)
    {
        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.ParsedUri.Should().NotBeNull();
        result.ParsedUri.IsAbsoluteUri.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Ipv6LinkLocal_ShouldBlock()
    {
        var url = "http://[fe80::1]/api";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Ipv6UniqueLocal_ShouldBlock()
    {
        var url = "http://[fc00::1]/api";
        var options = new UrlValidationOptions
        {
            RequireHttps = false
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklistInOptions_ShouldBlockSpecifiedRange()
    {
        // Public IP that would normally be allowed
        var url = "https://203.0.113.50/api"; // TEST-NET-3

        // Add custom blacklist rule in options
        var options = new UrlValidationOptions
        {
            CustomBlacklist = ["203.0.113.0/24"]
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklistWithSingleIp_ShouldBlockExactIp()
    {
        var url = "https://8.8.8.8/"; // Google DNS

        var options = new UrlValidationOptions
        {
            CustomBlacklist = ["8.8.8.8"]
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklistWithIpRange_ShouldBlockRange()
    {
        var url = "https://8.8.4.4/";

        var options = new UrlValidationOptions
        {
            CustomBlacklist = ["8.8.0.0-8.8.255.255"] // IP range
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeFalse();
        result.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklistMergedWithGlobalBlacklist_ShouldBlockBoth()
    {
        // Test that custom blacklist is merged with global blacklist (not replaced)

        // 1. Private IP should be blocked by global blacklist
        var privateUrl = "https://192.168.1.1/";
        var options = new UrlValidationOptions
        {
            CustomBlacklist = ["8.8.8.8"]
        };

        var result1 = await _urlValidator.ValidateAsync(privateUrl, options);
        result1.IsValid.Should().BeFalse("private IPs should still be blocked by global blacklist");
        result1.Blacklisted.Should().BeTrue();

        // 2. Custom blacklisted IP should be blocked
        var customUrl = "https://8.8.8.8/";
        var result2 = await _urlValidator.ValidateAsync(customUrl, options);
        result2.IsValid.Should().BeFalse("8.8.8.8 should be blocked by custom blacklist");
        result2.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NoCustomBlacklist_ShouldOnlyUseGlobalBlacklist()
    {
        // Public IP that's not in default blacklist should be allowed
        var url = "https://1.1.1.1/"; // Cloudflare DNS

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_EmptyCustomBlacklist_ShouldOnlyUseGlobalBlacklist()
    {
        var url = "https://1.1.1.1/";

        var options = new UrlValidationOptions
        {
            CustomBlacklist = []
        };

        var result = await _urlValidator.ValidateAsync(url, options);

        result.IsValid.Should().BeTrue();
        result.Blacklisted.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_CustomBlacklistWithMultipleRules_ShouldBlockAll()
    {
        var options = new UrlValidationOptions
        {
            CustomBlacklist =
            [
                "8.8.8.8",              // Single IP
                "203.0.113.0/24",       // CIDR range
                "198.51.100.1-198.51.100.50"  // IP range
            ]
        };

        // Test each rule
        var result1 = await _urlValidator.ValidateAsync("https://8.8.8.8/", options);
        result1.IsValid.Should().BeFalse();
        result1.Blacklisted.Should().BeTrue();

        var result2 = await _urlValidator.ValidateAsync("https://203.0.113.25/", options);
        result2.IsValid.Should().BeFalse();
        result2.Blacklisted.Should().BeTrue();

        var result3 = await _urlValidator.ValidateAsync("https://198.51.100.25/", options);
        result3.IsValid.Should().BeFalse();
        result3.Blacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_DirectPublicIp_ShouldPopulateResolvedAddresses()
    {
        var url = "https://1.1.1.1/";

        var result = await _urlValidator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.ResolvedAddresses.Should().NotBeNull();
        result.ResolvedAddresses.Should().HaveCountGreaterThanOrEqualTo(1);
        result.ResolvedAddresses[0].Should().Be(IPAddress.Parse("1.1.1.1"));
    }
}
