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


namespace ASC.Files.Tests.Tests._01_Files.TemplateGallery;

/// <summary>
/// GET /files/templateGallery/available - whether the portal can create files from gallery templates (bug 78009).
/// </summary>
/// <remarks>
/// The operation is not in the SDK until the SDKs are regenerated, so it is called through RawApiClient. Whether the
/// test stack reaches the gallery depends on its network, so the tests pin the contract (a boolean for a signed-in
/// caller, 401 without a sign-in), not the value; the value itself is covered by the unit tests of the gallery client
/// (OFormRequestManagerTests).
/// </remarks>
[Trait("Bug", "78009")]
[Trait("Feature", "Files")]
public class TemplateGalleryAvailableTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string Route = "api/2.0/files/templateGallery/available";

    [Fact]
    public async Task GetTemplateGalleryAvailable_SignedInUser_ReturnsABoolean()
    {
        await _filesClient.Authenticate(await InviteContact(EmployeeType.User));

        await AssertReturnsABooleanAsync();
    }

    [Fact]
    public async Task GetTemplateGalleryAvailable_Guest_ReturnsABoolean()
    {
        // "Any signed-in user" includes a guest: the call has no check of the user type
        await _filesClient.Authenticate(await InviteGuest());

        await AssertReturnsABooleanAsync();
    }

    private async Task AssertReturnsABooleanAsync()
    {
        var files = new RawApiClient(_filesClient);
        using var response = await files.GetAsync(Route, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The value is read as it came: a `response` that is null, a string or a number is not a boolean
        var value = await files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
        value.ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Fact]
    public async Task GetTemplateGalleryAvailable_Anonymous_Returns401()
    {
        await _filesClient.Authenticate(null);

        using var response = await new RawApiClient(_filesClient).GetAsync(Route, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
