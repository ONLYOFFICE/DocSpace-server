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

namespace ASC.AI.Tests.Tests.DocumentAgentTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/DocumentAgent")]
public class DocumentAgentSettingsReadTests(AspireAppFixture fixture) : DocumentAgentSettingsTestBase(fixture)
{
    [Fact]
    public async Task Read_FreshPortal_ReturnsDefaults()
    {
        var settings = await ReadSettingsAsync();

        settings.Enabled.Should().BeTrue();
        settings.ProfileId.Should().BeNull();
        settings.Reasoning.Should().Be(DocumentAgentReasoning.Default);
        settings.MaxToolCalls.Should().Be(DefaultMaxToolCalls);
        settings.MaxFileBytes.Should().Be(DefaultMaxFileBytes);
        settings.TaskLog.Should().BeFalse();
        settings.TaskLogFolderId.Should().BeNull();
    }

    [Fact]
    public async Task Update_AllFields_AreReadBack()
    {
        var profile = await CreateProfileInternalAsync();
        var folderId = await CreateFolderAsync();

        var updated = await UpdateSettingsAsync(BuildSettings(
            profileId: profile.Id,
            reasoning: DocumentAgentReasoning.High,
            maxToolCalls: 20,
            maxFileBytes: 1024,
            taskLog: true,
            taskLogFolderId: folderId));

        var settings = await ReadSettingsAsync();

        settings.Should().BeEquivalentTo(updated);
        settings.Enabled.Should().BeTrue();
        settings.ProfileId.Should().Be(profile.Id);
        settings.Reasoning.Should().Be(DocumentAgentReasoning.High);
        settings.MaxToolCalls.Should().Be(20);
        settings.MaxFileBytes.Should().Be(1024);
        settings.TaskLog.Should().BeTrue();
        settings.TaskLogFolderId.Should().Be(folderId.ToString());
    }

    [Fact]
    public async Task Update_Disabled_StaysDisabled()
    {
        await UpdateSettingsAsync(BuildSettings(enabled: false));

        (await ReadSettingsAsync()).Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_TaskLogWithoutFolder_IsAccepted()
    {
        await UpdateSettingsAsync(BuildSettings(taskLog: true));

        var settings = await ReadSettingsAsync();

        settings.TaskLog.Should().BeTrue();
        settings.TaskLogFolderId.Should().BeNull();
    }

    [Fact]
    public async Task Update_NullReferences_ClearsProfileAndFolder()
    {
        var profile = await CreateProfileInternalAsync();
        var folderId = await CreateFolderAsync();
        await UpdateSettingsAsync(BuildSettings(profileId: profile.Id, taskLogFolderId: folderId));

        await UpdateSettingsAsync(BuildSettings());

        var settings = await ReadSettingsAsync();

        settings.ProfileId.Should().BeNull();
        settings.TaskLogFolderId.Should().BeNull();
    }

    [Fact]
    public async Task Read_ProfileDeleted_ReturnsNullProfile()
    {
        var profile = await CreateProfileInternalAsync();
        await UpdateSettingsAsync(BuildSettings(profileId: profile.Id));

        await DeleteProfileInternalAsync(profile.Id);

        (await ReadSettingsAsync()).ProfileId.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_FolderDeleted_ReturnsNullFolder(bool immediately)
    {
        var folderId = await CreateFolderAsync();
        await UpdateSettingsAsync(BuildSettings(taskLog: true, taskLogFolderId: folderId));

        await DeleteFolderAsync(folderId, immediately);

        var settings = await ReadSettingsAsync();

        settings.TaskLogFolderId.Should().BeNull();
        settings.TaskLog.Should().BeTrue();
    }
}
