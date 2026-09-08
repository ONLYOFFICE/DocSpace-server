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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// <c>GET /api/2.0/files/file/{fileId}/checkconversion</c> - the access-check bug open against
/// this endpoint. Functional coverage for this endpoint does not exist yet beyond this bug.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class CheckConversionStatusBugTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// BUG 81825: a room member invited with Editing access gets 403 from checkconversion, even
    /// though Editing lets them open and edit the file - the endpoint's access check requires more
    /// than editing rights.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81825")]
    public async Task CheckConversionStatus_MemberWithEditingAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CheckConversion Editor Room");
        var file = await CreateFile("Autotest CheckConversion Editor File.docx", room.Id);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        await _filesClient.Authenticate(member);

        // Act
        var status = await _filesOperationsApi.CheckConversionStatusAsync(
            file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        status.Should().NotBeNull("Editing access is enough to read the conversion status of a file");
    }
}
