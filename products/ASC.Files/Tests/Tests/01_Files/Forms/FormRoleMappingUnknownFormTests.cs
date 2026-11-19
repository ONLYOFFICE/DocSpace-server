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

namespace ASC.Files.Tests.Tests._01_Files.Forms;

/// <summary>
/// <c>POST /files/file/{fileId}/formrolemapping</c> with a <c>formId</c> no file has. The service looked up the
/// form's room before it checked that the form exists, so <c>DocSpaceHelper.GetParentRoom</c> read the
/// <c>ParentId</c> of a null form and the call ended as 500 (<c>NullReferenceException</c>). The existence check now
/// comes first and refuses the call the way <c>manageformfilling</c> refuses the same form: 403 with the "file not
/// found" text.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "Forms")]
public class FormRoleMappingUnknownFormTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    [Fact]
    public async Task SaveFormRoleMapping_UnknownFormId_IsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const int unknownFormId = int.MaxValue;
        // The call is refused before any role is read, so the account in the role does not matter.
        var roles = new List<FormRoleRequest> { new(roleName: "Anyone", userId: Guid.NewGuid(), sequence: 1) };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFormRoleMappingAsync(
                unknownFormId.ToString(),
                new SaveFormRoleMappingDto(unknownFormId, roles),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The required file was not found");
    }
}
