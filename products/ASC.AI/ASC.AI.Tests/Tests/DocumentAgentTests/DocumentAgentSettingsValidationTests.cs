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

[Trait("Category", "Validation")]
[Trait("Feature", "AI/DocumentAgent")]
public class DocumentAgentSettingsValidationTests(AspireAppFixture fixture) : DocumentAgentSettingsTestBase(fixture)
{
    [Theory]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(int.MaxValue, HttpStatusCode.OK)]
    public async Task Update_MaxToolCalls_MustBePositive(int maxToolCalls, HttpStatusCode expected)
    {
        using var response = await PutSettingsAsync(BuildSettings(maxToolCalls: maxToolCalls));

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(int.MaxValue, HttpStatusCode.OK)]
    public async Task Update_MaxFileBytes_MustBePositive(int maxFileBytes, HttpStatusCode expected)
    {
        using var response = await PutSettingsAsync(BuildSettings(maxFileBytes: maxFileBytes));

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("\"xhigh\"")]
    [InlineData("99")]
    public async Task Update_UnknownReasoning_Returns400(string reasoning)
    {
        using var response = await PutRawSettingsAsync(BuildRawSettings(reasoning: reasoning));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_UnknownProfile_Returns400()
    {
        using var response = await PutSettingsAsync(BuildSettings(profileId: Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_UnknownFolder_Returns404()
    {
        using var response = await PutSettingsAsync(BuildSettings(taskLogFolderId: 999999999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_FolderInTrash_Returns404()
    {
        var folderId = await CreateFolderAsync();
        await DeleteFolderAsync(folderId, immediately: false);

        using var response = await PutSettingsAsync(BuildSettings(taskLogFolderId: folderId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ThirdPartyFolderId_Returns400()
    {
        using var response = await PutRawSettingsAsync(BuildRawSettings(taskLogFolderId: "\"box-1-abc\""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_FolderIdAsString_IsAccepted()
    {
        var folderId = await CreateFolderAsync();

        using var response = await PutRawSettingsAsync(BuildRawSettings(taskLogFolderId: $"\"{folderId}\""));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadSettingsAsync()).TaskLogFolderId.Should().Be(folderId.ToString());
    }

    [Fact]
    public async Task Update_FolderOfAnotherUser_Returns403()
    {
        var user = await InviteMemberAsync(EmployeeType.User);
        await FilesHttpClient.Authenticate(user);
        var userFolderId = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        using var response = await PutSettingsAsync(BuildSettings(taskLogFolderId: userFolderId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Rejected_KeepsPreviousSettings()
    {
        await UpdateSettingsAsync(BuildSettings(enabled: false, maxToolCalls: 10));

        using var response = await PutSettingsAsync(BuildSettings(profileId: Guid.NewGuid()));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var settings = await ReadSettingsAsync();

        settings.Enabled.Should().BeFalse();
        settings.MaxToolCalls.Should().Be(10);
    }
}
