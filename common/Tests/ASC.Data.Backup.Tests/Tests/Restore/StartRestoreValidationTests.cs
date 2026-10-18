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


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// The restore endpoint with bodies that cannot reach the restore queue, which is the only kind this suite
/// may send: restoring replaces the data of the portal, and even an all-zero GUID was seen to be accepted
/// and to queue a restore. Every request goes through <see cref="BackupTestBase"/>, whose guard refuses
/// anything that is not provably harmless: the <c>backupId</c> is never a GUID and <c>storageParams</c>
/// never carries a path. None of the bodies below can be written as a typed DTO (no body, a missing or
/// wrong-typed <c>backupId</c>, wrong-typed members), so each is a verbatim JSON body. The answer has to be
/// a 400 that names the field the framework could not read, no restore may be running, and a room created
/// before the request has to be intact afterwards.
/// </summary>
public class StartRestoreValidationTests(
    AspireAppFixture fixture)
    : BackupTestBase(fixture)
{
    private const string WitnessTitle = "Autotest Restore Witness";

    private static readonly Dictionary<string, (string? Json, string? Field)> _malformed = new()
    {
        ["a request without a body"] = (null, null),
        ["a null body"] = ("null", null),
        ["an empty body"] = ("{}", null),
        ["a body without backupId"] = ("""{"storageType":4}""", null),
        ["a numeric backupId"] = ("""{"backupId":123}""", "$.backupId"),
        ["an object backupId"] = ("""{"backupId":{}}""", "$.backupId"),
        ["an array backupId"] = ("""{"backupId":[]}""", "$.backupId"),
        ["a boolean backupId"] = ("""{"backupId":true}""", "$.backupId"),
        ["a string storageType"] = ("""{"backupId":"not-a-guid","storageType":"abc"}""", "$.storageType"),
        ["a string notify"] = ("""{"backupId":"not-a-guid","notify":"x"}""", "$.notify"),
        ["a numeric notify"] = ("""{"backupId":"not-a-guid","notify":5}""", "$.notify"),
        ["a null notify"] = ("""{"backupId":"not-a-guid","notify":null}""", "$.notify"),
        ["a string dump"] = ("""{"backupId":"not-a-guid","dump":"x"}""", "$.dump"),
        ["storageParams as an object"] = ("""{"backupId":"not-a-guid","storageParams":{}}""", "$.storageParams"),
        ["storageParams as a string"] = ("""{"backupId":"not-a-guid","storageParams":"x"}""", "$.storageParams")
    };

    public static TheoryData<string> MalformedCases => [.. _malformed.Keys];

    [Theory]
    [MemberData(nameof(MalformedCases))]
    public async Task StartRestore_MalformedBody_ReturnsBadRequestAndStartsNoRestore(string name)
    {
        // Arrange
        var (json, field) = _malformed[name];

        await _filesClient.Authenticate(Owner);

        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: WitnessTitle, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        // Act
        var rejection = await BackupRejection.FromResponseAsync(await StartRawRestoreThatCannotQueueAsync(json));

        // Assert
        await ExpectRestoreNotStartedAsync();
        rejection.Status.Should().Be(400);

        if (field is not null)
        {
            rejection.ErrorFields().Should().Contain(field);
        }

        var witness = await _roomsApi.GetRoomInfoAsync(room.Response.Id, TestContext.Current.CancellationToken);
        witness.Response.Title.Should().Be(WitnessTitle);
    }
}
