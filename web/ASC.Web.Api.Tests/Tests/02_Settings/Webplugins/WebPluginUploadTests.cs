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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webplugins;

/// <summary>
/// POST /api/2.0/settings/webplugins — who may upload and what the request itself must look like.
/// The upload is raw multipart (see <see cref="WebPluginsTestData"/>).
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginUploadTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Upload_MemberWithoutPortalSettingsRight_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(_webApiClient, "upload-denied", TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert
        status.Should().Be(403, body);
        body.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Upload_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(_webApiClient, "upload-anonymous", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Upload_DocSpaceAdmin_InstallsThePlugin()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(_webApiClient, "upload-admin", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        (await GetPluginAsync("upload-admin")).CreateBy!.Id.Should().Be(admin.Id);
    }

    [Fact]
    public async Task Upload_ValidPackage_ReturnsThePluginSwitchedOnWithItsScriptUrl()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(_webApiClient, "upload-body", TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert: the answer is read raw because the SDK operation cannot carry the file at all
        status.Should().Be(200, body);
        using var json = JsonDocument.Parse(body);
        var plugin = json.RootElement.GetProperty("response");
        plugin.GetProperty("name").GetString().Should().Be("upload-body");
        plugin.GetProperty("enabled").GetBoolean().Should().BeTrue();
        plugin.GetProperty("system").GetBoolean().Should().BeFalse();
        plugin.GetProperty("url").GetString().Should().EndWith("/upload-body/plugin.js?hash=1.0.0");
    }

    [Fact]
    public async Task Upload_FormWithoutFile_ReturnsNoInputFile()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        using var content = new MultipartFormDataContent { { new StringContent("value"), "field" } };

        // Act
        using var response = await _webApiClient.PostAsync(WebPluginsTestData.UploadPath, content, TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert
        status.Should().Be(400, body);
        body.Should().Contain("No input file");
    }

    [Fact]
    public async Task Upload_TwoFiles_ReturnsTooManyInputFiles()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(WebPluginsTestData.CreatePluginZip("upload-first")), "file", "first.zip" },
            { new ByteArrayContent(WebPluginsTestData.CreatePluginZip("upload-second")), "file2", "second.zip" }
        };

        // Act
        using var response = await _webApiClient.PostAsync(WebPluginsTestData.UploadPath, content, TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert
        status.Should().Be(400, body);
        body.Should().Contain("Too many input files");
    }

    [Fact]
    public async Task Upload_RequestThatIsNotAForm_ReturnsForbidden()
    {
        // Arrange: a JSON body is a transport-level case the SDK cannot send
        await _webApiClient.Authenticate(Owner);
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await _webApiClient.PostAsync(WebPluginsTestData.UploadPath, content, TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(403);
    }

    [Fact]
    public async Task Upload_FileWithAnotherExtension_ReturnsWrongFileExtension()
    {
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip("upload-ext"), 400, "Wrong file extension", "plugin.txt");
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsNoInputFile()
    {
        await AssertUploadRefusedAsync([], 400, "No input file");
    }

    [Fact]
    public async Task Upload_FileLargerThanFiveMegabytes_ReturnsFileSizeExceedsLimit()
    {
        var bytes = new byte[(5 * 1024 * 1024) + 1];
        Random.Shared.NextBytes(bytes);

        await AssertUploadRefusedAsync(bytes, 400, "File size exceeds limit");
    }

    [Fact]
    public async Task Upload_FileThatIsNotAZip_ReturnsWrongPluginArchive()
    {
        await AssertUploadRefusedAsync(WebPluginsTestData.Text("not a zip archive"), 400, "Wrong plugin archive");
    }
}
