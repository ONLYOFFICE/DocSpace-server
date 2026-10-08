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
/// Covers the MySQL server version detection behind every pooled DbContext: a service that starts before
/// MySQL accepts connections must recover once the server is up, without a restart of the process.
/// </summary>
[Trait("Category", "Database")]
public class MySqlServerVersionCacheTests
{
    private const string ConnectionString = "Server=mysql;Port=3306;Database=onlyoffice";
    private static readonly ServerVersion _version = new MySqlServerVersion(new Version(8, 0, 39));

    [Fact]
    public void Get_DetectionFailedWhileServerStarting_DetectsAgainOnNextCall()
    {
        var serverUp = false;
        var calls = 0;
        var cache = new MySqlServerVersionCache(_ =>
        {
            calls++;
            return serverUp ? _version : throw new InvalidOperationException("Unable to connect to any of the specified MySQL hosts.");
        });

        var first = () => cache.Get(ConnectionString);
        first.Should().Throw<InvalidOperationException>();

        serverUp = true;

        cache.Get(ConnectionString).Should().BeSameAs(_version);
        calls.Should().Be(2);
    }

    [Fact]
    public void Get_DetectionSucceeded_DoesNotDetectAgain()
    {
        var calls = 0;
        var cache = new MySqlServerVersionCache(_ =>
        {
            calls++;
            return _version;
        });

        cache.Get(ConnectionString).Should().BeSameAs(_version);
        cache.Get(ConnectionString).Should().BeSameAs(_version);

        calls.Should().Be(1);
    }

    [Fact]
    public void Get_DifferentConnectionStrings_DetectsEachServerSeparately()
    {
        // Contexts registered for another region or connection name may point at another server;
        // the version of the first one detected must not be applied to them.
        var other = new MySqlServerVersion(new Version(9, 2, 0));
        var cache = new MySqlServerVersionCache(cs => cs == ConnectionString ? _version : other);

        cache.Get(ConnectionString).Should().BeSameAs(_version);
        cache.Get("Server=external;Port=3306;Database=onlyoffice").Should().BeSameAs(other);
    }
}
