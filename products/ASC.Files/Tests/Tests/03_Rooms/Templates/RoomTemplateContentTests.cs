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

using QuotaSettingsRequestsDto = DocSpace.API.SDK.Model.QuotaSettingsRequestsDto;

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

/// <summary>
/// What POST /files/roomtemplate carries into the created template: DTO fields (color, cover,
/// tags, quota, public, share, groups), the copied source-room content, title sanitisation, and who
/// can see the result in the catalogue. Lifecycle/validation lives in
/// <see cref="RoomTemplateCreateTests"/>, the public flag's own read/write endpoints in
/// <see cref="RoomTemplateShareTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTemplateContentTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomTemplate_WithColor_ReflectsInLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Color Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Color Template", color: "FF5733"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Logo?.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task CreateRoomTemplate_WithCover_ReflectsInLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Cover Template", cover: coverId),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Logo?.Cover?.Id.Should().Be(coverId);
    }

    [Fact]
    public async Task CreateRoomTemplate_WithTags_ReflectsInRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Tags Source");
        List<string> tags = ["TmplTagAlpha", "TmplTagBeta"];

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Tags Template", tags: tags),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        (info.Tags ?? []).Should().Contain(tags);
    }

    [Fact]
    public async Task CreateRoomTemplate_WithQuota_ReflectsInRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(100 * 1024 * 1024)),
            TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Quota Source");
        const long myQuota = 10 * 1024 * 1024;

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Quota Template", quota: myQuota),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.QuotaLimit.Should().Be(myQuota);
    }

    [Fact]
    public async Task CreateRoomTemplate_WithPublicTrueAtCreation_IsPublic()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest PublicAtCreate Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest PublicAtCreate Template", @public: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoomTemplate_WithShareUserList_GrantsAccessToSharedUser()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sharedUser = await InviteMember(EmployeeType.DocSpaceAdmin);
        var room = await CreateCustomRoom("Autotest Share Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Share Template", share: [sharedUser.Email]),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        await _filesClient.Authenticate(sharedUser);
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task CreateRoomTemplate_WithGroupsList_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([user.Id], user.Id, $"Autotest Tmpl Group {Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;
        var room = await CreateCustomRoom("Autotest Groups Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Groups Template", groups: [group.Id]),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_FromEmptySourceRoom_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Empty Src");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Empty Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_FromSourceWithNestedFolders_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Nested Src");
        var parent = await CreateFolder("Parent", room.Id);
        await CreateFolder("Child", parent.Id);

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Nested Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_FromSourceWithFiles_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Files Src");
        await CreateFile("TmplSource.docx", room.Id);

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Files Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_AppearsInTemplatesList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Catalog Source");
        const string templateTitle = "Autotest Catalog Template";

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, templateTitle),
            TestContext.Current.CancellationToken);
        await WaitForRoomTemplate();

        // Assert
        var titles = await GetTemplateTitles();
        titles.Should().Contain(templateTitle);
    }

    [Fact]
    public async Task CreateRoomTemplate_NonPublicTemplate_NotVisibleToUnrelatedUser()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest NotShared Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest NotShared Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var user = await InviteMember(EmployeeType.User);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // Public templates are visible only to admin-level roles, not to a regular User/Guest —
    // "public: true" does not mean "any authenticated user".
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateRoomTemplate_PublicTemplate_VisibleToAdminRoles(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest PublicVisible {employeeType} Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, $"Autotest PublicVisible {employeeType} Template", @public: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(templateId);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateRoomTemplate_PublicTemplate_NotVisibleToNonAdminRoles(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest PublicHidden {employeeType} Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, $"Autotest PublicHidden {employeeType} Template", @public: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateRoomTemplate_CyrillicTitle_AcceptedAsIs()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cyrillic Source");
        const string title = "Шаблон Кириллица";

        // Act
        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(room.Id, title), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(title);
    }

    [Fact]
    public async Task CreateRoomTemplate_EmojiInTitle_SanitizedToUnderscore()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Emoji Source");
        const string rawTitle = "Template 🚀 Emoji";

        // Act
        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(room.Id, rawTitle), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().NotContain("🚀");
        info.Title.Should().Contain("_");
    }

    [Fact]
    public async Task CreateRoomTemplate_ForbiddenCharsInTitle_SanitizedToUnderscore()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Forbidden Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Bad\" \\ < > / Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().NotContainAny("\"", "\\", "<", ">", "/");
        info.Title.Should().Contain("_");
    }

    [Fact]
    public async Task CreateRoomTemplate_DuplicateTitles_Allowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Dup A");
        var roomB = await CreateCustomRoom("Autotest Dup B");
        const string title = "Duplicate Template Title";

        // Act
        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(roomA.Id, title), TestContext.Current.CancellationToken);
        var templateAId = await WaitForRoomTemplate();

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(roomB.Id, title), TestContext.Current.CancellationToken);
        var templateBId = await WaitForRoomTemplate();

        // Assert
        templateAId.Should().NotBe(templateBId);
        var infoA = (await _roomsApi.GetRoomInfoAsync(templateAId, TestContext.Current.CancellationToken)).Response;
        var infoB = (await _roomsApi.GetRoomInfoAsync(templateBId, TestContext.Current.CancellationToken)).Response;
        infoA.Title.Should().Be(title);
        infoB.Title.Should().Be(title);
    }
}
