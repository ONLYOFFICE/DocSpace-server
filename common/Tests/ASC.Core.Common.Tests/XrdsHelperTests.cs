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
/// The XRDS discovery document is served from /login without a provider and carries the
/// caller-supplied return url. Both urls used to be concatenated into the document
/// unescaped, so a "&lt;" or "&amp;" in the return url broke the document apart or injected
/// sibling Service elements into it.
/// </summary>
[Trait("Bug", "82555")]
public class XrdsHelperTests
{
    private const string IconLink = "https://portal.example.com/openid.gif";

    [Fact]
    public void GetXrds_LocationClosingItsOwnElement_ShouldStayWellFormed()
    {
        const string location = "https://portal.example.com/login?auth=openid&returnurl=</URI></Service><Service><Type>injected</Type><URI>";

        var document = XDocument.Parse(XrdsHelper.GetXrds(location, IconLink));

        document.Descendants().Count(element => element.Name.LocalName == "Service").Should().Be(2);
        document.Descendants().Count(element => element.Name.LocalName == "URI").Should().Be(2);
        document.Descendants().First(element => element.Name.LocalName == "URI").Value.Should().Be(location);
    }

    [Fact]
    public void GetXrds_IconLinkClosingItsOwnElement_ShouldStayWellFormed()
    {
        const string iconLink = "https://portal.example.com/openid.gif\"><Service><Type>injected</Type><URI>";

        var document = XDocument.Parse(XrdsHelper.GetXrds("https://portal.example.com/login?auth=openid", iconLink));

        document.Descendants().Count(element => element.Name.LocalName == "Service").Should().Be(2);
        document.Descendants().Last(element => element.Name.LocalName == "URI").Value.Should().Be(iconLink);
    }

    [Fact]
    public void GetXrds_PlainUrls_ShouldKeepThemAsIs()
    {
        const string location = "https://portal.example.com/login?auth=openid&returnurl=%2Frooms";

        var document = XDocument.Parse(XrdsHelper.GetXrds(location, IconLink));

        document.Descendants().First(element => element.Name.LocalName == "URI").Value.Should().Be(location);
        document.Descendants().Last(element => element.Name.LocalName == "URI").Value.Should().Be(IconLink);
    }
}
