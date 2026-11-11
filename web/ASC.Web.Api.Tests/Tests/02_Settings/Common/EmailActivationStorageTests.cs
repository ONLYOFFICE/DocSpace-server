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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// PUT /api/2.0/settings/emailactivation stores the preference per member
/// (<c>SettingsController.UpdateEmailActivationSettings</c>, <c>SaveForCurrentUserAsync</c>). No operation reads it
/// back apart from the update's own echo, so the stored rows are read from the database
/// (<c>EmailActivationSettings</c>, id <c>85987929-...</c>).
/// </summary>
[Trait("Category", "Settings")]
public class EmailActivationStorageTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static readonly Guid _emailActivationSettingsId = new("85987929-1339-48EB-B06D-B9D097BDACF6");

    [Fact]
    public async Task UpdateEmailActivationSettings_TwoMembers_EachKeepsTheirOwnValue()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.UpdateEmailActivationSettingsAsync(new EmailActivationSettingsRequestDto(false), TestContext.Current.CancellationToken);

        // Act
        await _webApiClient.Authenticate(user);
        await _commonSettingsApi.UpdateEmailActivationSettingsAsync(new EmailActivationSettingsRequestDto(true), TestContext.Current.CancellationToken);

        // Assert: the owner's false is stored under the owner's id and the user's call did not touch it
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        var rows = await SettingsAudit.ReadSettingsRowsAsync(connection, TenantId, _emailActivationSettingsId);
        rows.Should().ContainKey(Owner.Id);
        // false is the CLR default and is left out of the stored JSON, so the owner's row reads "{}"
        rows[Owner.Id].Replace(" ", "").Should().NotContainEquivalentOf("\"Show\":true");
        rows.Should().NotContainKey(Guid.Empty);
        if (rows.TryGetValue(user.Id, out var userRow))
        {
            userRow.Replace(" ", "").Should().ContainEquivalentOf("\"Show\":true");
        }
    }
}
