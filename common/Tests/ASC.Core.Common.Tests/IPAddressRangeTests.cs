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

namespace ASC.Core.Common.Tests;

/// <summary>
/// <see cref="IPAddressRange.MatchIPs"/> is the matcher behind every IP allow list a portal owner can write:
/// the TFA trusted IPs, the portal IP restrictions and the outgoing-URL restrictions. It is called on the login
/// path, where nothing catches an exception, so a restriction it cannot parse has to answer "no match" rather
/// than throw.
/// </summary>
public class IPAddressRangeTests
{
    private const string RequestIp = "192.0.2.10";

    /// <summary>
    /// A trusted IP entry that is neither an address, a range nor a CIDR block used to reach
    /// <see cref="IPAddress.Parse(string)"/> unguarded — the login path then failed with an unhandled
    /// <see cref="FormatException"/> ("An invalid IP address was specified."), locking every user out of the
    /// portal, web UI included. Such an entry is now simply not a match.
    /// </summary>
    [Theory]
    [Trait("Bug", "82994")]
    [InlineData("not-an-ip")]
    [InlineData("192.0.2.10-")]
    [InlineData("-192.0.2.10")]
    [InlineData("192.0.2.1-nonsense")]
    [InlineData("999.999.999.999-999.999.999.999")]
    [InlineData("nonsense/24")]
    [InlineData("192.0.2.0/nonsense")]
    [InlineData("192.0.2.0/64")]
    public void MatchIPs_MalformedRestriction_DoesNotMatchInsteadOfThrowing(string restrictionIp)
    {
        IPAddressRange.MatchIPs(RequestIp, restrictionIp).Should().BeFalse();
    }

    /// <summary>
    /// The request IP comes off the connection rather than from settings, but it reaches the same parser and
    /// must not be able to break the match either.
    /// </summary>
    [Theory]
    [Trait("Bug", "82994")]
    [InlineData("unknown", "192.0.2.1-192.0.2.20")]
    [InlineData("unknown", "192.0.2.0/24")]
    [InlineData("", "192.0.2.1-192.0.2.20")]
    public void MatchIPs_MalformedRequestIp_DoesNotMatchInsteadOfThrowing(string requestIp, string restrictionIp)
    {
        IPAddressRange.MatchIPs(requestIp, restrictionIp).Should().BeFalse();
    }

    [Theory]
    [InlineData("192.0.2.10")]
    [InlineData("192.0.2.1-192.0.2.20")]
    [InlineData("192.0.2.10-192.0.2.10")]
    [InlineData("192.0.2.0/24")]
    public void MatchIPs_MatchingRestriction_Matches(string restrictionIp)
    {
        IPAddressRange.MatchIPs(RequestIp, restrictionIp).Should().BeTrue();
    }

    [Theory]
    [InlineData("192.0.2.11")]
    [InlineData("192.0.2.11-192.0.2.20")]
    [InlineData("198.51.100.0/24")]
    [InlineData("2001:db8::/32")]
    public void MatchIPs_NonMatchingRestriction_DoesNotMatch(string restrictionIp)
    {
        IPAddressRange.MatchIPs(RequestIp, restrictionIp).Should().BeFalse();
    }

    /// <summary>
    /// A request IP arrives with the peer port attached, in either address family.
    /// </summary>
    [Theory]
    [InlineData("192.0.2.10:52341", "192.0.2.10")]
    [InlineData("192.0.2.10:52341", "192.0.2.1-192.0.2.20")]
    [InlineData("[2001:db8::1]:52341", "2001:db8::1")]
    public void MatchIPs_RequestIpWithPort_MatchesOnTheAddressAlone(string requestIp, string restrictionIp)
    {
        IPAddressRange.MatchIPs(requestIp, restrictionIp).Should().BeTrue();
    }

    /// <summary>
    /// PUT api/2.0/settings/tfaapp used to store any string as a trusted IP, so an entry that could never match
    /// was accepted with 200 and the owner got no sign the bypass was not in place. The value is now rejected
    /// where it is written.
    /// </summary>
    [Theory]
    [Trait("Bug", "82994")]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("192.0.2.10-")]
    [InlineData("-192.0.2.10")]
    [InlineData("192.0.2.1-nonsense")]
    [InlineData("nonsense/24")]
    [InlineData("192.0.2.0/nonsense")]
    [InlineData("192.0.2.0/64")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidRestriction_MalformedEntry_IsRejected(string? restrictionIp)
    {
        IPAddressRange.IsValidRestriction(restrictionIp).Should().BeFalse();
    }

    [Theory]
    [InlineData("192.0.2.10")]
    [InlineData("192.0.2.1-192.0.2.20")]
    [InlineData(" 192.0.2.1 - 192.0.2.20 ")]
    [InlineData("192.0.2.0/24")]
    [InlineData("2001:db8::1")]
    [InlineData("2001:db8::/32")]
    public void IsValidRestriction_WellFormedEntry_IsAccepted(string restrictionIp)
    {
        IPAddressRange.IsValidRestriction(restrictionIp).Should().BeTrue();
    }

    [Theory]
    [Trait("Bug", "82994")]
    [InlineData("not-an-ip")]
    [InlineData("192.0.2.10-")]
    public void IpAddressOrRange_ListWithAMalformedEntry_ReportsTheMember(string restrictionIp)
    {
        var results = Validate(["192.0.2.1", restrictionIp]);

        results.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(TrustedIpsHolder.TrustedIps));
    }

    [Fact]
    public void IpAddressOrRange_ListOfWellFormedEntries_IsAccepted()
    {
        Validate(["192.0.2.1", "198.51.100.1-198.51.100.20", "203.0.113.0/24"]).Should().BeEmpty();
    }

    /// <summary>
    /// An absent or empty list means "no bypass at all", which is the default — it is not a validation error.
    /// </summary>
    [Fact]
    public void IpAddressOrRange_NoList_IsAccepted()
    {
        Validate(null).Should().BeEmpty();
    }

    [Fact]
    public void IpAddressOrRange_EmptyList_IsAccepted()
    {
        Validate([]).Should().BeEmpty();
    }

    private static List<ValidationResult> Validate(List<string>? trustedIps)
    {
        var model = new TrustedIpsHolder { TrustedIps = trustedIps };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        return results;
    }

    /// <summary>
    /// Stands in for <c>TfaRequestsDto</c>, which lives in ASC.Web.Api and is not referenced here — the property
    /// carries the same attribute, so the rule under test is the same one the endpoint runs.
    /// </summary>
    private sealed class TrustedIpsHolder
    {
        [IpAddressOrRange]
        public List<string>? TrustedIps { get; set; }
    }
}
