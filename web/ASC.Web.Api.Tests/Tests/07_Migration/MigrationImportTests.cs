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
/// The import (<c>POST api/2.0/migration/migrate</c>) of a parsed Nextcloud backup into the test's own portal:
/// which accounts are created and how, groups, personal files, and what clear and finish do afterwards.
/// Each test uploads and parses the same small backup: a new user with files (alice), an existing portal
/// member with files (bob), a user without e-mail (carol), a new user left unselected (erin), one group.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationImportTests(
    AspireAppFixture fixture)
    : MigrationTestBase(fixture)
{
    private const string GroupName = "importteam";

    private User _bob = null!;
    private string _aliceEmail = null!;
    private string _erinEmail = null!;

    [Fact]
    public async Task StartMigration_SelectedUsers_CreatesPendingAccountsOfTheChosenType()
    {
        // Arrange
        await UploadAndParse();
        var carolEmail = NewEmail();

        // Act
        var status = await Import(Info(false, false, Select("alice", _aliceEmail), Select("carol", carolEmail, EmployeeType.RoomAdmin)));

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().BeEmpty();
        status.Progress.Should().Be(100);
        status.ParseResult.SuccessedUsers.Should().Be(2);
        status.ParseResult.FailedUsers.Should().Be(0);

        await _peopleClient.Authenticate(Owner);
        var alice = (await _profilesApi.GetProfileByEmailAsync(_aliceEmail, cancellationToken: TestContext.Current.CancellationToken)).Response;
        alice.ActivationStatus.Should().Be(EmployeeActivationStatus.Pending);
        alice.FirstName.Should().Be("Alice");
        alice.IsRoomAdmin.Should().BeFalse();
        alice.IsAdmin.Should().BeFalse();

        var carol = (await _profilesApi.GetProfileByEmailAsync(carolEmail, cancellationToken: TestContext.Current.CancellationToken)).Response;
        carol.ActivationStatus.Should().Be(EmployeeActivationStatus.Pending);
        carol.IsRoomAdmin.Should().BeTrue();
        carol.LastName.Should().Be("Carol");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetProfileByEmailAsync(_erinEmail, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task StartMigration_ImportGroups_CreatesTheGroupWithTheImportedMembersOnly()
    {
        // Arrange
        await UploadAndParse();

        // Act
        var status = await Import(Info(true, false, Select("alice", _aliceEmail), Select("bob", _bob.Email)));

        // Assert
        status!.Error.Should().BeEmpty();
        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.GetGroupsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response
            .Should().ContainSingle(g => g.Name == GroupName).Subject;
        var members = (await _groupApi.GetGroupAsync(group.Id, true, TestContext.Current.CancellationToken)).Response.Members;
        members.Select(m => m.Email).Should().BeEquivalentTo(_aliceEmail, _bob.Email);
    }

    [Fact]
    public async Task StartMigration_ImportPersonalFiles_PutsTheFilesIntoAMigrationFolderInMyDocuments()
    {
        // Arrange
        await UploadAndParse();

        // Act
        var status = await Import(Info(false, true, Select("bob", _bob.Email)));

        // Assert
        status!.Error.Should().BeEmpty();
        var folder = (await MigrationFolders()).Should().ContainSingle().Subject;
        (await FileTitlesUnder(folder.Id)).Should().BeEquivalentTo("bob.txt", "note.txt");
    }

    [Fact]
    public async Task StartMigration_UsersOnly_CreatesNoGroupAndNoFiles()
    {
        // Arrange
        await UploadAndParse();

        // Act
        var status = await Import(Info(false, false, Select("alice", _aliceEmail), Select("bob", _bob.Email)));

        // Assert
        status!.Error.Should().BeEmpty();
        status.ParseResult.SuccessedUsers.Should().Be(2);
        await _peopleClient.Authenticate(Owner);
        (await _groupApi.GetGroupsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response
            .Should().NotContain(g => g.Name == GroupName);
        (await MigrationFolders()).Should().BeEmpty();
    }

    [Fact]
    public async Task StartMigration_AfterAnImport_UploadIsGone()
    {
        // Arrange
        await UploadAndParse();
        (await Import(Info(false, false, Select("alice", _aliceEmail))))!.Error.Should().BeEmpty();

        // Act
        var status = await Parse("Nextcloud");

        // Assert
        status.Should().NotBeNull();
        status!.Error.Should().Be(FileNotFoundMessage);
    }

    [Fact]
    public async Task ClearMigration_AfterAnImport_RemovesTheJob()
    {
        // Arrange
        await UploadAndParse();
        (await Import(Info(false, false, Select("alice", _aliceEmail))))!.Error.Should().BeEmpty();

        // Act
        await _migrationApi.ClearMigrationAsync(TestContext.Current.CancellationToken);

        // Assert
        (await WaitForStatus(s => s == null)).Should().BeNull();
    }

    [Fact]
    public async Task FinishMigration_WelcomeEmailAfterAnImport_SucceedsAndClearsTheJob()
    {
        // Arrange
        await UploadAndParse();
        (await Import(Info(false, false, Select("alice", _aliceEmail))))!.Error.Should().BeEmpty();

        // Act
        await _migrationApi.FinishMigrationAsync(new FinishDto(true), TestContext.Current.CancellationToken);

        // Assert
        (await WaitForStatus(s => s == null)).Should().BeNull();
    }

    private async Task UploadAndParse()
    {
        _bob = await InviteContact(EmployeeType.User);
        _aliceEmail = NewEmail();
        _erinEmail = NewEmail();

        var zip = new NextcloudBackup()
            .Account("alice", "Alice Import", _aliceEmail, "readme.txt")
            .Account("bob", "Bob Existing", _bob.Email, "bob.txt", "Docs/note.txt")
            .Account("carol", "Carol", null)
            .Account("erin", "Erin Skipped", _erinEmail)
            .Group(GroupName, "alice", "bob", "erin")
            .ToZip();

        await UploadBackup("nc.zip", zip);
        var parsed = await Parse("Nextcloud");
        parsed!.Error.Should().BeEmpty();
    }

    private static MigrationApiInfo Info(bool importGroups, bool importPersonalFiles, params MigratingApiUser[] users) =>
        new(migratorName: "Nextcloud", users: [.. users], importGroups: importGroups, importPersonalFiles: importPersonalFiles);

    private async Task<List<FolderDto>> MigrationFolders()
    {
        await _filesClient.Authenticate(_bob);
        var content = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Folders ?? []).OfType<FolderDto>().Where(f => f.Title.StartsWith("ASC migration files ")).ToList();
    }

    private async Task<List<string>> FileTitlesUnder(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var titles = (content.Files ?? []).Select(f => f.Title).ToList();

        foreach (var folder in (content.Folders ?? []).OfType<FolderDto>())
        {
            titles.AddRange(await FileTitlesUnder(folder.Id));
        }

        return titles;
    }
}
