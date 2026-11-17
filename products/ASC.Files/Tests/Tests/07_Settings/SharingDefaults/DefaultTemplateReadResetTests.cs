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
/// <c>GET</c>/<c>DELETE /files/settings/defaulttemplate</c> and the storing side of <c>PUT</c>/<c>POST</c>:
/// who may read the set, that a chosen file is copied and the previous custom blank deleted, that an
/// extension the built-in set does not cover is a silent no-op, and that a reset drops the custom blank.
/// The setting is portal-wide, so every test changes it only on its own portal.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class DefaultTemplateReadResetTests(AspireAppFixture fixture) : SharingDefaultsTestBase(fixture)
{
    private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(null)]
    public async Task GetDefaultTemplates_OwnerOrAdmin_ReadsEveryExtensionWithoutCustomBlank(EmployeeType? role)
    {
        // Arrange - null stands for the portal owner
        if (role is not null)
        {
            await _filesClient.Authenticate(await InviteContact(role.Value));
        }

        // Act
        var settings = (await _filesSettingsApi.GetDefaultTemplatesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Items.Should().NotBeEmpty();
        settings.Items.Select(i => i.FileExtension).Should().Contain(".docx").And.OnlyHaveUniqueItems();
        settings.Items.Should().OnlyContain(i => i.SelectedFile == null);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetDefaultTemplates_WithoutPortalSettingsRight_ReturnsForbidden(EmployeeType role)
    {
        // Arrange
        var user = role == EmployeeType.Guest ? await InviteGuest() : await InviteContact(role);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.GetDefaultTemplatesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetDefaultTemplate_ExistingFile_StoresACopyAndLeavesTheOriginal()
    {
        // Arrange
        var source = await CreateFileInMy("Autotest Template Source.docx", Owner);

        // Act
        var settings = (await SetTemplate(source.Id, ".docx")).Response;

        // Assert - the entry points at a new file, the source is still there
        var item = settings.Items.Single(i => i.FileExtension == ".docx");
        item.SelectedFile.Should().NotBeNull().And.NotBe(source.Id);
        item.FileTitle.Should().NotBeNullOrEmpty();
        (await _filesApi.GetFileInfoAsync(source.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Id.Should().Be(source.Id);
    }

    [Fact]
    public async Task SetDefaultTemplate_Replaced_DeletesThePreviousCustomBlank()
    {
        // Arrange
        var first = await CreateFileInMy("Autotest Template First.docx", Owner);
        var second = await CreateFileInMy("Autotest Template Second.docx", Owner);
        var previous = (await SetTemplate(first.Id, ".docx")).Response.Items.Single(i => i.FileExtension == ".docx").SelectedFile!.Value;
        (await FileStatus(previous)).Should().NotBe(404);

        // Act
        var current = (await SetTemplate(second.Id, ".docx")).Response.Items.Single(i => i.FileExtension == ".docx").SelectedFile;

        // Assert
        current.Should().NotBeNull().And.NotBe(previous);
        (await FileStatus(previous)).Should().Be(404);
    }

    [Fact]
    public async Task SetDefaultTemplate_ExtensionOutsideBuiltInSet_SucceedsAndChangesNothing()
    {
        // Arrange - the extension is matched before the file is looked up, so even an unknown id passes
        var before = (await _filesSettingsApi.GetDefaultTemplatesAsync(TestContext.Current.CancellationToken)).Response;

        // Act
        var after = (await SetTemplate(9999999, ".autotestext")).Response;

        // Assert
        after.Items.Select(i => i.FileExtension).Should().BeEquivalentTo(before.Items.Select(i => i.FileExtension));
        after.Items.Should().OnlyContain(i => i.SelectedFile == null);
    }

    [Fact]
    public async Task UploadDefaultTemplate_PdfThatIsNotAForm_ReturnsForbidden()
    {
        // Arrange - a minimal PDF without form fields
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n"));
        var file = new FileParameter("template.pdf", "application/pdf", stream);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.UploadDefaultTemplateAsync(".pdf", file, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await _filesSettingsApi.GetDefaultTemplatesAsync(TestContext.Current.CancellationToken)).Response
            .Items.Should().OnlyContain(i => i.SelectedFile == null);
    }

    [Fact]
    public async Task UploadDefaultTemplate_Replaced_StoresTheUploadAndDeletesThePreviousBlank()
    {
        // Arrange
        var previous = (await Upload("first.docx")).Response.Items.Single(i => i.FileExtension == ".docx").SelectedFile;
        previous.Should().NotBeNull();

        // Act
        var item = (await Upload("second.docx")).Response.Items.Single(i => i.FileExtension == ".docx");

        // Assert
        item.SelectedFile.Should().NotBeNull().And.NotBe(previous!.Value);
        item.FileTitle.Should().Be("second.docx");
        (await FileStatus(previous.Value)).Should().Be(404);
    }

    [Fact]
    public async Task ResetDefaultTemplate_CustomBlank_ClearsTheEntryAndDeletesTheBlank()
    {
        // Arrange
        var source = await CreateFileInMy("Autotest Template Reset.docx", Owner);
        var template = (await SetTemplate(source.Id, ".docx")).Response.Items.Single(i => i.FileExtension == ".docx").SelectedFile!.Value;

        // Act
        var settings = (await _filesSettingsApi.ResetDefaultTemplateAsync(
            new DefaultTemplateSettingsResetRequestDto(".docx"), TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Items.Single(i => i.FileExtension == ".docx").SelectedFile.Should().BeNull();
        (await FileStatus(template)).Should().Be(404);
        (await _filesApi.GetFileInfoAsync(source.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Id.Should().Be(source.Id);
    }

    [Fact]
    public async Task ResetDefaultTemplate_NoCustomBlank_SucceedsAndChangesNothing()
    {
        // Act
        var first = (await _filesSettingsApi.ResetDefaultTemplateAsync(
            new DefaultTemplateSettingsResetRequestDto(".docx"), TestContext.Current.CancellationToken)).Response;
        var second = (await _filesSettingsApi.ResetDefaultTemplateAsync(
            new DefaultTemplateSettingsResetRequestDto(".docx"), TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Items.Should().OnlyContain(i => i.SelectedFile == null);
        second.Items.Select(i => i.FileExtension).Should().BeEquivalentTo(first.Items.Select(i => i.FileExtension));
    }

    [Fact]
    public async Task ResetDefaultTemplate_RoomAdmin_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(await InviteContact(EmployeeType.RoomAdmin));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ResetDefaultTemplateAsync(
                new DefaultTemplateSettingsResetRequestDto(".docx"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private Task<DefaultTemplateSettingsWrapper> SetTemplate(int fileId, string extension)
    {
        return _filesSettingsApi.SetDefaultTemplateAsync(
            new DefaultTemplateSettingsRequestDto(new DefaultTemplateSettingsRequestDtoSelectedFile(fileId), extension),
            TestContext.Current.CancellationToken);
    }

    private async Task<DefaultTemplateSettingsWrapper> Upload(string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("autotest template " + fileName));
        var file = new FileParameter(fileName, DocxMime, stream);

        return await _filesSettingsApi.UploadDefaultTemplateAsync(".docx", file, TestContext.Current.CancellationToken);
    }

    /// <summary>The status the owner gets for a file: 200, or the error code the read was refused with.</summary>
    private async Task<int> FileStatus(int fileId)
    {
        try
        {
            await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }
}
