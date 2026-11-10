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

namespace ASC.Files.Tests.Tests._01_Files.Docs;

/// <summary>
/// <c>POST /api/2.0/docs/builder</c>: what is refused in advance - access, script syntax, one script at a time.
/// The script names portal files as a QUOTED string id: <c>builder.OpenFile("1234")</c>, the bare number of the
/// SDK example is rejected. A script that really runs needs the document server and is not covered here.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Docs")]
public class DocsBuilderTests(
    AspireAppFixture fixture)
    : DocsTestBase(fixture)
{
    [Fact]
    public async Task RunBuilderScript_UserWithoutEditAccessToTheOpenedFile_Returns403()
    {
        // Arrange
        var (room, file) = await CreateRoomWithDocx("Docs Builder No Access");
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.RunBuilderScriptAsync(
                new RunBuilderScriptRequestDto(BuilderScript(file.Id), outputs: BuilderOutputs(room.Id)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RunBuilderScript_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.RunBuilderScriptAsync(new RunBuilderScriptRequestDto("builder.OpenFile(\"1\");"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task RunBuilderScript_UnquotedNumericFileId_Returns400()
    {
        // Arrange
        var (_, file) = await CreateRoomWithDocx("Docs Builder Bad Syntax");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.RunBuilderScriptAsync(
                new RunBuilderScriptRequestDto($"builder.OpenFile({file.Id});\nbuilder.CloseFile();"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task RunBuilderScript_EmptyScript_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.RunBuilderScriptAsync(new RunBuilderScriptRequestDto(string.Empty), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // The first run is only queued: the call answers before the build starts, so the second request meets a script
    // that is still queued or running. The operation is then waited out so it does not outlive the test.
    [Fact]
    public async Task RunBuilderScript_SecondCallWhileTheFirstIsRunning_Returns403()
    {
        // Arrange
        var (room, file) = await CreateRoomWithDocx("Docs Builder Concurrent");
        var request = new RunBuilderScriptRequestDto(BuilderScript(file.Id), outputs: BuilderOutputs(room.Id));
        await DocsApi.RunBuilderScriptAsync(request, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.RunBuilderScriptAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await WaitLongOperation();
    }
}
