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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// <c>PUT</c> and <c>GET /api/2.0/files/file/{fileId}/checkconversion</c> for a file that has to be
/// converted: the job is queued and reported as a running entry. Whether it later ends with a file is
/// the document server's business (see <see cref="FileConversionTestBase"/>); a job that must end with
/// an error does so with or without one.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileConversionStartTests(
    AspireAppFixture fixture)
    : FileConversionTestBase(fixture)
{
    [Fact]
    public async Task StartFileConversion_RtfFile_QueuesOneRunningEntryForTheSourceVersion()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var id = await Put(myDocsFolderId, "Autotest Convert Basic.rtf", Rtf("Hello Conversion Probe"));

        // Nothing is known before the start.
        (await ReadEntries(id)).Should().BeEmpty();

        // Act
        var started = await Start(id, new CheckConversionRequestDto(startConvert: true));

        // Assert
        var entry = started.Should().ContainSingle().Which;
        entry.Operation.Should().Be(FileOperationType.Convert);
        entry.Progress.Should().Be(0);
        entry.Error.Should().BeNullOrEmpty();

        using var source = JsonDocument.Parse(entry.Source);
        source.RootElement.GetProperty("id").GetInt32().Should().Be(id);
        source.RootElement.GetProperty("version").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CheckConversionStatus_StartTrue_StartsTheConversionAsWell()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var id = await Put(myDocsFolderId, "Autotest Convert StartFlag.rtf", Rtf("Flag start"));

        // Act
        var entries = await ReadEntries(id, start: true);

        // Assert
        entries.Should().ContainSingle().Which.Operation.Should().Be(FileOperationType.Convert);
    }

    [Theory]
    [InlineData("FlagFalse")]
    [InlineData("EmptyBody")]
    public async Task StartFileConversion_BodyWithoutStartConvert_StillStartsTheConversion(string variant)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var id = await Put(myDocsFolderId, $"Autotest Convert {variant}.rtf", Rtf($"Body {variant}"));

        // Act
        var queued = (await Start(id, variant == "FlagFalse" ? new CheckConversionRequestDto(startConvert: false) : new CheckConversionRequestDto())).Count;

        // Assert - the flag is set by the operation itself
        queued.Should().Be(1);
    }

    [Fact]
    public async Task StartFileConversion_VersionThatDoesNotExist_QueuesNothing()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert Version", myDocsFolderId);
        var id = await OperationFileBytes.UploadVersionsAsync(
            _filesOperationsApi, dir.Id, "Autotest Convert Version.rtf", [Rtf("Version One"), Rtf("Version Two")], TestContext.Current.CancellationToken);
        var before = await FileTitles(dir.Id);

        // Act
        var started = await Start(id, new CheckConversionRequestDto(startConvert: true, version: 99, createNewIfExist: true));

        // Assert
        started.Should().BeEmpty();
        (await ReadEntries(id)).Should().BeEmpty();
        (await FileTitles(dir.Id)).Should().Equal(before);
    }

    [Fact]
    public async Task StartFileConversion_XmlFile_IsQueuedAndEndsWithAnErrorNoResultAndAnUnchangedSource()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert Unsupported", myDocsFolderId);
        var bytes = Encoding.UTF8.GetBytes("<a>x</a>");
        var id = await Put(dir.Id, "Autotest Convert Unsupported.xml", bytes);
        var before = await FileTitles(dir.Id);

        // Act
        var started = await Start(id);
        started.Should().HaveCount(1);
        var done = await WaitForFinishedEntry(id);

        // Assert
        done.Should().NotBeNull("the failed job is reported once");
        done!.Progress.Should().Be(100, "progress reaches 100 for a failure as well");
        done.Error.Should().NotBeNullOrEmpty("the failure is reported in error");
        done.Result.Should().BeNull();
        (await FileTitles(dir.Id)).Should().Equal(before);
        await AssertSourceIntact(id, bytes);

        // The finished entry is handed out once, and a new attempt is possible.
        (await ReadEntries(id)).Should().BeEmpty();
        (await Start(id)).Should().HaveCount(1);
        (await WaitForFinishedEntry(id))?.Error.Should().NotBeNullOrEmpty();
        (await FileTitles(dir.Id)).Should().Equal(before);
    }

    /// <summary>
    /// Polls until the entry reaches 100 (that read hands it out) and returns it, or the last observed
    /// state when the deadline passes, so the assertion shows what the job was doing.
    /// </summary>
    private async Task<ConversationResultDto?> WaitForFinishedEntry(int fileId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);

        while (true)
        {
            var entries = await ReadEntries(fileId);
            var entry = entries.SingleOrDefault();

            if (entry is { Progress: 100 } || DateTime.UtcNow >= deadline)
            {
                return entry;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }
}
