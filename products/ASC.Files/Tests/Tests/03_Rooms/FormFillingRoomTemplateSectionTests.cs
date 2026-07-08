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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// Verifies that room templates are split between the Virtual Rooms and Form Filling Rooms
/// template sections by the template's own folder type.
///
/// All room templates physically live under the same RoomTemplates root, and a template inherits
/// the folder type of its source room. The split is applied at query time: SearchArea.Templates
/// returns only non-form room templates, while SearchArea.FormTemplates returns only
/// <see cref="FolderType.FillingFormsRoom"/> templates. Because the split is based purely on the
/// stored folder type, it applies equally to templates created before the split was introduced
/// and to newly created ones.
/// </summary>
[Trait("Category", "Rooms")]
public class FormFillingRoomTemplateSectionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task FormRoomTemplate_AppearsInFormTemplatesSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var templateTitle = "Form Template " + Guid.NewGuid().ToString()[..8];

        // Act
        await CreateRoomTemplateAndWait(formRoom.Id, templateTitle);

        var formTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.FormTemplates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        formTemplates.Folders.Should().Contain(r => r.Title == templateTitle,
            "form filling room templates must be listed in the dedicated form templates section");
    }

    [Fact]
    public async Task FormRoomTemplate_DoesNotAppearInRoomTemplatesSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var templateTitle = "Form Template " + Guid.NewGuid().ToString()[..8];

        // Act
        await CreateRoomTemplateAndWait(formRoom.Id, templateTitle);

        var roomTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Templates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        roomTemplates.Folders.Should().NotContain(r => r.Title == templateTitle,
            "form filling room templates must no longer be listed in the Virtual Rooms templates section");
    }

    [Fact]
    public async Task RegularRoomTemplate_AppearsInTemplatesSection_ButNotInFormTemplates()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);
        var templateTitle = "Custom Template " + Guid.NewGuid().ToString()[..8];

        // Act
        await CreateRoomTemplateAndWait(customRoom.Id, templateTitle);

        var roomTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Templates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var formTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.FormTemplates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        roomTemplates.Folders.Should().Contain(r => r.Title == templateTitle,
            "regular room templates remain visible in the Virtual Rooms templates section");
        formTemplates.Folders.Should().NotContain(r => r.Title == templateTitle,
            "regular room templates must not leak into the form templates section");
    }

    [Fact]
    public async Task FormAndRegularRoomTemplates_AreSplitBetweenSections()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);

        var formTemplateTitle = "Form Template " + Guid.NewGuid().ToString()[..8];
        var customTemplateTitle = "Custom Template " + Guid.NewGuid().ToString()[..8];

        // Act
        await CreateRoomTemplateAndWait(formRoom.Id, formTemplateTitle);
        await CreateRoomTemplateAndWait(customRoom.Id, customTemplateTitle);

        var roomTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Templates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var formTemplates = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.FormTemplates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert - the form room template is only in the form templates section
        formTemplates.Folders.Should().Contain(r => r.Title == formTemplateTitle);
        roomTemplates.Folders.Should().NotContain(r => r.Title == formTemplateTitle);

        // Assert - the regular room template is only in the Virtual Rooms templates section
        roomTemplates.Folders.Should().Contain(r => r.Title == customTemplateTitle);
        formTemplates.Folders.Should().NotContain(r => r.Title == customTemplateTitle);
    }

    [Fact]
    public async Task BrowseRoomTemplatesRootFolder_ById_ExcludesFormRoomTemplates()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);

        var formTemplateTitle = "Form Template " + Guid.NewGuid().ToString()[..8];
        var customTemplateTitle = "Custom Template " + Guid.NewGuid().ToString()[..8];

        await CreateRoomTemplateAndWait(customRoom.Id, customTemplateTitle);
        await CreateRoomTemplateAndWait(formRoom.Id, formTemplateTitle);

        var templatesRootId = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Templates,
            cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        // Act - browse the RoomTemplates root folder directly by its id (defaults to the Templates area)
        var templatesContent = (await _foldersApi.GetFolderByFolderIdAsync(
            templatesRootId,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        templatesContent.Should().NotBeNull();
        templatesContent.Folders.Should().Contain(r => r.Title == customTemplateTitle,
            "browsing the RoomTemplates root folder must list regular room templates");
        templatesContent.Folders.Should().NotContain(r => r.Title == formTemplateTitle,
            "browsing the RoomTemplates root folder must not list form filling room templates");
    }

    [Fact]
    public async Task RoomCreatedFromFormRoomTemplate_AppearsInFormsSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var templateTitle = "Form Template " + Guid.NewGuid().ToString()[..8];
        var templateId = await CreateRoomTemplateAndWait(formRoom.Id, templateTitle);

        var newRoomTitle = "Room From Form Template " + Guid.NewGuid().ToString()[..8];

        // Act - a room created from a form room template inherits the FillingFormsRoom type
        await CreateRoomFromTemplateAndWait(templateId, newRoomTitle);

        var formsSection = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Forms,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var virtualRooms = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        formsSection.Folders.Should().Contain(r => r.Title == newRoomTitle,
            "a room created from a form room template must be surfaced in the Forms section");
        virtualRooms.Folders.Should().NotContain(r => r.Title == newRoomTitle,
            "a room created from a form room template must not appear in the Virtual Rooms section");
    }

    private async Task<int> CreateRoomTemplateAndWait(int roomId, string title)
    {
        var status = (await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(roomId: roomId, title: title),
            TestContext.Current.CancellationToken)).Response;

        if (status is { IsCompleted: true })
        {
            status.Error.Should().BeNullOrEmpty("room template creation must succeed");
            return status.TemplateId;
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            TestContext.Current.CancellationToken);

        while (true)
        {
            status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(linkedCts.Token)).Response;

            if (status is { IsCompleted: true })
            {
                status.Error.Should().BeNullOrEmpty("room template creation must succeed");
                return status.TemplateId;
            }

            await Task.Delay(100, linkedCts.Token);
        }
    }

    private async Task<int> CreateRoomFromTemplateAndWait(int templateId, string title)
    {
        var status = (await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId: templateId, title: title),
            TestContext.Current.CancellationToken)).Response;

        if (status is { IsCompleted: true })
        {
            status.Error.Should().BeNullOrEmpty("room template creation must succeed");
            return status.RoomId;
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            TestContext.Current.CancellationToken);

        while (true)
        {
            status = (await _roomsApi.GetRoomCreatingStatusAsync(linkedCts.Token)).Response;

            if (status is { IsCompleted: true })
            {
                status.Error.Should().BeNullOrEmpty("room creation from a template must succeed");
                return status.RoomId;
            }

            await Task.Delay(100, linkedCts.Token);
        }
    }
}
