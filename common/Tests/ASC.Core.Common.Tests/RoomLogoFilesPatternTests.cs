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

public class RoomLogoFilesPatternTests
{
    private static readonly string[] _storedFiles =
    [
        "12_original.png",
        "12_large.png",
        "12_medium.png",
        "12_small.png",
        "12_watermark.png",
        "1_original.png",
        "112_original.png",
        "120_original.png",
        "123_small.png",
        "120_watermark.png",
        "sbox-12_original.png",
        "sbox-123_original.png"
    ];

    public static TheoryData<string, string[]> Rooms => new()
    {
        { "12", ["12_original.png", "12_large.png", "12_medium.png", "12_small.png", "12_watermark.png"] },
        { "sbox-12", ["sbox-12_original.png"] }
    };

    [Theory]
    [MemberData(nameof(Rooms))]
    public void Pattern_OnDiscStorage_MatchesOnlyTheFilesOfTheRoom(string roomId, string[] expected)
    {
        var directory = Directory.CreateTempSubdirectory("room_logos_");

        try
        {
            foreach (var name in _storedFiles)
            {
                File.WriteAllBytes(Path.Combine(directory.FullName, name), []);
            }

            var matched = Directory.EnumerateFiles(directory.FullName, RoomLogoManager.GetLogoFilesPattern(roomId), SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName);

            matched.Should().BeEquivalentTo(expected);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Theory]
    [MemberData(nameof(Rooms))]
    public void Pattern_OnCloudStorage_MatchesOnlyTheFilesOfTheRoom(string roomId, string[] expected)
    {
        var pattern = RoomLogoManager.GetLogoFilesPattern(roomId);

        _storedFiles.Where(name => Wildcard.IsMatch(pattern, name)).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Pattern_IntegerId_IsThePatternOfItsString()
    {
        RoomLogoManager.GetLogoFilesPattern(12).Should().Be(RoomLogoManager.GetLogoFilesPattern("12"));
    }
}
