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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// Handing out the password of the built-in forms database and resetting it are both written to the audit trail, so the
/// portal owner sees who could read every submission.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbAuditTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AccessIssued = "Read access to the built-in forms database issued";
    private const string PasswordReset = "Password of the read access to the built-in forms database reset";

    [Fact]
    public async Task IssuedAndResetPassword_AreWrittenToTheAuditTrail()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);

        // A DocSpace admin, so the initiator is not the portal owner every event of a new portal is by.
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // The calls answer 202 until the portal's own database is ready, and only the answer with the password is audited.
        await BuiltinDbApi.GetReadyAsync(_filesClient);
        await BuiltinDbApi.GetReadyAsync(_filesClient, resetPassword: true);

        // Written by the audit service behind the message bus, after the requests return. Asserted by their text: the
        // MessageAction of the SDK predates these actions.
        var events = await PollLastAuditEventsAsync(e => e.Any(x => x.Action == AccessIssued) && e.Any(x => x.Action == PasswordReset));

        events.Where(e => e.Action == AccessIssued).Should().ContainSingle().Which.UserId.Should().Be(admin.Id);
        events.Where(e => e.Action == PasswordReset).Should().ContainSingle().Which.UserId.Should().Be(admin.Id);
    }

    private async Task<List<AuditEventDto>> PollLastAuditEventsAsync(Func<List<AuditEventDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetLastAuditEventsAsync(TestContext.Current.CancellationToken)).Response ?? [];

            if (until(events) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
