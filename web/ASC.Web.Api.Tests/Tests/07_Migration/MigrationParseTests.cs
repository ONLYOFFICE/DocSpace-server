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
/// The parse pass over an uploaded backup (<c>POST api/2.0/migration/init/{migratorName}</c>), run by the
/// Web.Studio host on a hand-made Nextcloud backup (<see cref="NextcloudBackup"/>), and what the status, the
/// log, cancel and finish do with the parsed job.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationParseTests(
    AspireAppFixture fixture)
    : MigrationTestBase(fixture)
{
    [Fact]
    public async Task ParseNextcloud_ValidBackup_SortsUsersIntoNewExistingAndWithoutEmail()
    {
        // Arrange
        var existing = await InviteContact(EmployeeType.User);
        var terminated = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, new UpdateMembersRequestDto([terminated.Id]), TestContext.Current.CancellationToken);
        var email = NewEmail();
        var zip = new NextcloudBackup()
            .Account("alice", "Alice Import", email, "readme.txt", "Docs/note.txt")
            .Account("bob", "Bob Existing", existing.Email)
            .Account("carol", "Carol", null)
            .Account("dave", "Dave Disabled", terminated.Email)
            .Group("importteam", "alice", "bob")
            .ToZip();
        await UploadBackup("nc.zip", zip);

        // Act
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Progress.Should().Be(100);
        status.Error.Should().BeEmpty();
        var result = status.ParseResult;
        result.MigratorName.Should().Be("Nextcloud");
        result.Operation.Should().Be("parse");
        result.Files.Should().Equal("nc.zip");
        result.FailedArchives.Should().BeEmpty();

        var alice = result.Users.Should().ContainSingle().Subject;
        alice.Key.Should().Be("alice");
        alice.Email.Should().Be(email);
        alice.FirstName.Should().Be("Alice");
        alice.LastName.Should().Be("Import");
        alice.UserType.Should().Be(EmployeeType.User);
        alice.ShouldImport.Should().BeFalse();
        alice.MigratingFiles.FilesCount.Should().Be(2);

        result.ExistUsers.Should().ContainSingle(u => u.Key == "bob" && u.Email == existing.Email);
        result.WithoutEmailUsers.Should().ContainSingle(u => u.Key == "carol" && u.Email == null);
        result.Users.Concat(result.ExistUsers).Concat(result.WithoutEmailUsers).Should().NotContain(u => u.Key == "dave");

        var group = result.Groups.Should().ContainSingle().Subject;
        group.GroupName.Should().Be("importteam");
        group.UserUidList.Should().BeEquivalentTo("alice", "bob");
    }

    [Fact]
    public async Task ParseNextcloud_MigratorNameInAnotherCase_IsResolved()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Act
        var status = await Parse("NEXTCLOUD");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().BeEmpty();
        status.ParseResult.MigratorName.Should().Be("Nextcloud");
    }

    [Fact]
    public async Task ParseNextcloud_NotAZip_FailsAndListsTheFile()
    {
        // Arrange
        await UploadBackup("backup.txt", Encoding.UTF8.GetBytes("not an archive"));

        // Act
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be("Archive must be .zip");
        status.ParseResult.FailedArchives.Should().ContainSingle(f => f.EndsWith("backup.txt"));
    }

    [Fact]
    public async Task ParseNextcloud_ZipWithoutDump_FailsWithCouldNotParseArchive()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail(), "readme.txt").ToZip(withDump: false));

        // Act
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be("Couldn't parse nc archive");
        status.ParseResult.FailedArchives.Should().Equal("nc.zip");
    }

    [Fact]
    public async Task ParseWorkspace_Zip_FailsWithArchiveMustBeGzOrTar()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Act
        var status = await Parse("Workspace");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be("Archive must be .gz or .tar");
        status.ParseResult.FailedArchives.Should().ContainSingle(f => f.EndsWith("nc.zip"));
    }

    [Fact]
    public async Task ParseGoogleWorkspace_ZipWithoutTakeout_FailsWithCouldNotParseArchives()
    {
        // Arrange
        await UploadBackup("takeout.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Act
        var status = await Parse("GoogleWorkspace");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be("Couldn't parse archives");
        status.ParseResult.FailedArchives.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseNextcloud_AfterAFailedParse_UploadIsGone()
    {
        // Arrange
        await UploadBackup("backup.txt", Encoding.UTF8.GetBytes("not an archive"));
        var failed = await Parse("Nextcloud");
        failed!.Error.Should().Be("Archive must be .zip");

        // Act
        var status = await Parse("Nextcloud", s => s.Error != failed.Error);

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be(FileNotFoundMessage);
    }

    [Fact]
    public async Task ParseNextcloud_AfterACompletedJob_ReplacesIt()
    {
        // Arrange
        await _migrationApi.UploadAndInitializeMigrationAsync("TotallyNotARealMigrator", TestContext.Current.CancellationToken);
        (await WaitForStatus(s => s?.IsCompleted == true))!.Error.Should().NotBeEmpty();
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());

        // Act
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().BeEmpty();
        status.ParseResult.MigratorName.Should().Be("Nextcloud");
    }

    [Fact]
    public async Task CancelMigration_AfterAParse_RemovesTheJobAndTheUpload()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());
        (await Parse("Nextcloud"))!.Error.Should().BeEmpty();

        // Act
        await _migrationApi.CancelMigrationAsync(TestContext.Current.CancellationToken);
        var afterCancel = await WaitForStatus(s => s == null);
        var reparse = await Parse("Nextcloud");

        // Assert
        afterCancel.Should().BeNull();
        reparse.Should().NotBeNull();
        reparse!.Error.Should().Be(FileNotFoundMessage);
    }

    [Fact]
    public async Task GetMigrationLogs_AfterAParse_ReturnsTimestampedPlainTextAttachment()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());
        await Parse("Nextcloud");

        // Act
        // raw HTTP: the SDK's GetMigrationLogs parses the text/plain body as JSON and throws on the first line
        using var response = await _webApiClient.GetAsync("api/2.0/migration/logs", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        response.Content.Headers.ContentType.CharSet.Should().BeEquivalentTo("utf-8");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName!.Trim('"').Should().Be("migration.log");
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().NotBeEmpty();
        lines.Should().OnlyContain(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}: "));
    }

    /// <summary>
    /// With <c>isSendWelcomeEmail</c> after a parse-only job the documented answer is 500 "No migration is in
    /// progress"; the job has no imported-user list, so the loop throws a NullReferenceException instead (M3).
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinishMigration_WelcomeEmailAfterAParse_AnswersNoMigrationInProgress()
    {
        // Arrange
        await UploadBackup("nc.zip", new NextcloudBackup().Account("alice", "Alice Import", NewEmail()).ToZip());
        await Parse("Nextcloud");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.FinishMigrationAsync(new FinishDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
        exception.ErrorContent!.ToString().Should().Contain("No migration is in progress");
    }

    [Fact]
    public async Task FinishMigration_WelcomeEmailWithoutAJob_AnswersNoMigrationInProgress()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.FinishMigrationAsync(new FinishDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
        exception.ErrorContent!.ToString().Should().Contain("No migration is in progress");
    }
}
