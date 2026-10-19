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


namespace ASC.Data.Backup.Tests.Tests.Start;

/// <summary>
/// POST /api/2.0/backup/startbackup with a body the framework itself refuses before the controller: a
/// value of the wrong JSON type. The generated DTO cannot carry any of them, so each is a verbatim JSON
/// body, and the answer has to name the field that could not be read.
/// </summary>
public class StartBackupBindingValidationTests(
    AspireAppFixture fixture)
    : StartBackupValidationTestBase(fixture)
{
    private static readonly Dictionary<string, (string Json, string Field)> _bindingErrors = new()
    {
        ["a null body"] = ("null", ""),
        ["a string storageType"] = ("""{"storageType":"abc"}""", "$.storageType"),
        ["a fractional storageType"] = ("""{"storageType":1.5}""", "$.storageType"),
        ["a boolean storageType"] = ("""{"storageType":true}""", "$.storageType"),
        ["an array storageType"] = ("""{"storageType":[]}""", "$.storageType"),
        ["storageParams as an object"] = ("""{"storageType":0,"storageParams":{"folderId":"1"}}""", "$.storageParams"),
        ["storageParams as a string"] = ("""{"storageType":0,"storageParams":"x"}""", "$.storageParams"),
        ["storageParams as a number"] = ("""{"storageType":0,"storageParams":5}""", "$.storageParams"),
        ["a string dump"] = ("""{"storageType":4,"dump":"x"}""", "$.dump")
    };

    public static TheoryData<string> BindingErrorCases => [.. _bindingErrors.Keys];

    [Theory]
    [MemberData(nameof(BindingErrorCases))]
    public async Task StartBackup_UnreadableBody_ReturnsBadRequestNamingTheFieldAndStartsNothing(string name)
    {
        // Arrange
        var (json, field) = _bindingErrors[name];

        // Act
        var rejection = await SendRejectedRawAsync(json);

        // Assert
        rejection.Status.Should().Be(400);
        rejection.ErrorFields().Should().Contain(field);
    }
}
