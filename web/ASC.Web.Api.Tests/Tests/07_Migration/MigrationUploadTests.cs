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


namespace ASC.Web.Api.Tests.Tests._07_Migration;

/// <summary>
/// The upload handler <c>migrationFileUpload.ashx</c> of the Web.Api: <c>Init=true</c> opens a new upload
/// folder, a later POST appends <c>Form.Files[0]</c> to the file named by <c>Name</c>. It answers JSON
/// <c>{Success, Message, ChunkSize}</c>; whether a chunk landed is proven by parsing what was uploaded.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationUploadTests(
    AspireAppFixture fixture)
    : MigrationTestBase(fixture)
{
    [Fact]
    public async Task Upload_InitAsOwner_ReturnsSuccessAndDefaultChunkSize()
    {
        // Act
        var answer = await PostUpload("?Init=true");

        // Assert
        answer.Status.Should().Be(HttpStatusCode.OK);
        answer.Success.Should().BeTrue();
        answer.Message.Should().BeNull();
        answer.ChunkSize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public async Task Upload_InitAsDocSpaceAdmin_ReturnsSuccess()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var answer = await PostUpload("?Init=true");

        // Assert
        answer.Success.Should().BeTrue(answer.Message);
    }

    /// <summary>
    /// The handler refuses a member who is not a DocSpace admin with <c>Success=false</c> and
    /// "Access denied.", but answers that with HTTP 200 (M1): a client checking the status sees nothing wrong.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [Trait("Category", "Bug")]
    public async Task Upload_InitAsNonAdmin_AnswersAccessDeniedWith403(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var answer = await PostUpload("?Init=true");

        // Assert
        answer.Success.Should().BeFalse();
        answer.Message.Should().Be("Access denied.");
        answer.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// A chunk sent before any <c>Init</c> has no upload folder; the handler reports the raw exception message
    /// with <c>Success=false</c>, but with HTTP 200 (M1).
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task Upload_ChunkWithoutInit_AnswersAnErrorStatus()
    {
        // Act
        var answer = await PostChunk("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Assert
        answer.Success.Should().BeFalse();
        answer.Message.Should().NotBeNullOrEmpty();
        ((int)answer.Status).Should().BeInRange(400, 599);
    }

    [Fact]
    public async Task Upload_BackupInThreeChunks_IsReassembledIntoOneArchive()
    {
        // Arrange
        var email = NewEmail();
        var zip = new NextcloudBackup().Account("alice", "Alice Import", email, "readme.txt").ToZip();

        // Act
        await UploadBackup("nc.zip", zip, chunks: 3);
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().BeEmpty();
        status.ParseResult.Files.Should().Equal("nc.zip");
        status.ParseResult.Users.Should().ContainSingle(u => u.Key == "alice" && u.Email == email);
    }

    [Fact]
    public async Task Upload_InitAgain_DiscardsThePreviousUpload()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Act
        var init = await PostUpload("?Init=true");
        var status = await Parse("Nextcloud");

        // Assert
        init.Success.Should().BeTrue();
        status.Should().NotBeNull();
        status!.Error.Should().Be("Archive must be .zip");
        status.ParseResult.FailedArchives.Should().BeEmpty();
    }
}
