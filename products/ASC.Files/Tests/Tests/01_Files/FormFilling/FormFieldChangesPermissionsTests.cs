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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// Access to <c>PUT /files/file/:fileId/fieldchanges</c> on a started form: the renames are tied to the next save of
/// the form, so only a user who may edit it - its author, the user who started its filling, a room manager - may
/// report them. A content creator who may only start the filling is refused, or answers to a field it renamed would land
/// in another field's column.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class FormFieldChangesPermissionsTests(AspireAppFixture fixture) : FormFillingTestBase(fixture)
{
    private const string EditRefusal = "You do not have enough permissions to edit the file";

    [Fact]
    public async Task FieldChanges_AuthorOfStartedForm_IsNotRefusedForAccess()
    {
        var (_, formId) = await CreateStartedFormAsync();

        await _filesClient.Authenticate(Owner);

        await AssertNotRefusedForAccessAsync(formId);
    }

    [Fact]
    public async Task FieldChanges_RoomManagerOfStartedForm_IsNotRefusedForAccess()
    {
        var (roomId, formId) = await CreateStartedFormAsync();
        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(roomId, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        await AssertNotRefusedForAccessAsync(formId);
    }

    [Fact]
    public async Task FieldChanges_ContentCreatorWhoStartedTheForm_IsNotRefusedForAccess()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest FieldChanges Starter " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, contentCreator, FileShare.ContentCreator);

        await _filesClient.Authenticate(contentCreator);
        await StartFormFilling(form.Id);

        await AssertNotRefusedForAccessAsync(form.Id);
    }

    [Fact]
    public async Task FieldChanges_ContentCreatorNotAuthorOfStartedForm_IsRefused()
    {
        var (roomId, formId) = await CreateStartedFormAsync();
        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, contentCreator, FileShare.ContentCreator);

        await _filesClient.Authenticate(contentCreator);
        using var response = await ReportAsync(formId);

        ((int)response.StatusCode).Should().Be(403);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(EditRefusal);
    }

    // A null entry is a malformed body, refused before anything is read; a typed request cannot carry it.
    [Fact]
    public async Task FieldChanges_NullRenameEntry_IsBadRequest()
    {
        var (_, formId) = await CreateStartedFormAsync();

        await _filesClient.Authenticate(Owner);
        using var response = await new RawApiClient(_filesClient).PutRawAsync($"api/2.0/files/file/{formId}/fieldchanges",
            """{"version":1,"sequence":1,"sessionId":"tab","renames":[null]}""", TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(400);
    }

    private async Task<(int RoomId, int FormId)> CreateStartedFormAsync()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest FieldChanges " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return (room.Id, form.Id);
    }

    // The endpoint is not in the SDK yet, so it is called over raw HTTP.
    private Task<HttpResponseMessage> ReportAsync(int formId)
    {
        return new RawApiClient(_filesClient).PutAsync($"api/2.0/files/file/{formId}/fieldchanges",
            new { version = 1, sequence = 1, sessionId = "tab", renames = new[] { new { oldKey = "fromWhom", newKey = "applicant" } } },
            TestContext.Current.CancellationToken);
    }

    // With Document Server Community Edition, which the test stack may run, an allowed caller still gets 403 because
    // the editors cannot track renamed fields; the access refusal is told apart by its message.
    private async Task AssertNotRefusedForAccessAsync(int formId)
    {
        using var response = await ReportAsync(formId);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().BeOneOf(200, 403);
        body.Should().NotContain(EditRefusal);
    }
}
