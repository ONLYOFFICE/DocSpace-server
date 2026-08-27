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

namespace ASC.Files.Tests.Tests._07_Settings.SharingDefaults;

/// <summary>
/// <c>PUT /files/settings/dafaultaccessrights</c> - the per-user default access rights offered in the
/// sharing dialog.
/// </summary>
/// <remarks>
/// SDK defect: the controller returns a bare <c>List&lt;FileShare&gt;</c> (a JSON array of ints), but
/// the generated <c>ChangeDefaultAccessRightsAsync</c> deserialises the response as
/// <c>FileShareArrayWrapper.Response</c> -&gt; <c>List&lt;FileShareDto&gt;</c> (an array of sharing-info
/// objects with a required, non-nullable <c>subjectType</c>). Deserialising an array of ints into that
/// shape fails, so every call in this class goes over raw HTTP instead and reads the array back as ints.
/// This mirrors why <c>SettingsTests.ChangeDefaultAccessRights_ShouldUpdateAccessRights</c> is commented
/// out in this project.
/// </remarks>
[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class DefaultAccessRightsTests(AspireAppFixture fixture) : SharingDefaultsTestBase(fixture)
{
    private const string Path = "api/2.0/files/settings/dafaultaccessrights";

    public static TheoryData<EmployeeType?> AllowedRoles =>
    [
        null, // the portal owner
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    private async Task<HttpResponseMessage> PutRaw(List<int>? rights)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, Path);

        // "No body" still needs a Content-Type header: without any content ASP.NET answers 415
        // before the controller runs, which is not the case under test. An empty payload with the
        // header is what a real client sends and is refused as a malformed body (400).
        request.Content = rights != null
            ? new StringContent(JsonSerializer.Serialize(rights), Encoding.UTF8, "application/json")
            : new StringContent(string.Empty, Encoding.UTF8, "application/json");

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<List<int>> ChangeDefaultAccessRights(params FileShare[] rights)
    {
        using var response = await PutRaw([.. rights.Select(r => (int)r)]);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadRights(response);
    }

    private static async Task<List<int>> ReadRights(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);

        return [.. json.RootElement.GetProperty("response").EnumerateArray().Select(e => e.GetInt32())];
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_SetsMultipleAccessRights()
    {
        // Act
        var rights = await ChangeDefaultAccessRights(FileShare.ReadWrite, FileShare.Read);

        // Assert
        rights.Should().NotBeEmpty();
        rights.Should().OnlyContain(r => Enum.IsDefined(typeof(FileShare), r));
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_SetsSingleAccessRight()
    {
        // Act
        var rights = await ChangeDefaultAccessRights(FileShare.Read);

        // Assert
        rights.Should().Equal((int)FileShare.Read);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_SettingNewRights_ReplacesPreviousRights()
    {
        // Arrange
        await ChangeDefaultAccessRights(FileShare.ReadWrite, FileShare.Read, FileShare.Review);

        // Act
        var rights = await ChangeDefaultAccessRights(FileShare.Comment);

        // Assert
        rights.Should().Equal((int)FileShare.Comment);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_EmptyArray_FallsBackToDefault()
    {
        // Act
        var rights = await ChangeDefaultAccessRights();

        // Assert - an empty list resets the setting, which then reports the product default
        rights.Should().NotBeNull();
        rights.Should().OnlyContain(r => Enum.IsDefined(typeof(FileShare), r));
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_NoBody_ReturnsBadRequest()
    {
        // Act
        using var response = await PutRaw(null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_DuplicateValues_AreHandled()
    {
        // Act
        var rights = await ChangeDefaultAccessRights(FileShare.Read, FileShare.Read);

        // Assert
        rights.Should().NotBeEmpty();
        rights.Should().OnlyContain(r => Enum.IsDefined(typeof(FileShare), r));
    }

    [Fact]
    [Trait("Bug", "XXXXX")]
    public async Task ChangeDefaultAccessRights_InvalidFileShareValue_ReturnsBadRequest()
    {
        // Act
        using var response = await PutRaw([999]);

        // Assert - the product currently accepts an out-of-range FileShare value and returns 200
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_ChangeIsReflectedInGetFilesSettings()
    {
        // Arrange
        await ChangeDefaultAccessRights(FileShare.Read);

        // Act
        var settings = await GetFilesSettings();

        // Assert
        settings.DefaultSharingAccessRights.Should().Contain(r => (int)r == (int)FileShare.Read);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await ChangeDefaultAccessRights(FileShare.Read);

        // Act
        await _filesClient.Authenticate(user);
        await ChangeDefaultAccessRights(FileShare.ReadWrite);

        await _filesClient.Authenticate(Owner);
        var settings = await GetFilesSettings();

        // Assert - the owner's own setting was untouched by the member's change
        settings.DefaultSharingAccessRights.Should().Contain(r => (int)r == (int)FileShare.Read);
        settings.DefaultSharingAccessRights.Should().NotContain(r => (int)r == (int)FileShare.ReadWrite);
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await PutRaw([(int)FileShare.Read]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task ChangeDefaultAccessRights_EveryRole_CanChangeOwnSetting(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var rights = await ChangeDefaultAccessRights(FileShare.Read);

        // Assert
        rights.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChangeDefaultAccessRights_TerminatedDocSpaceAdmin_Unauthorized()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        await TerminateUser(admin);

        // Act
        using var response = await PutRaw([(int)FileShare.Read]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
