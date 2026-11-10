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


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// backupFileUpload.ashx, the chunked upload of an archive to restore from, which the backup service
/// serves outside the REST API and therefore outside the SDK. The handler answers every request with 200
/// and a JSON object whose <c>success</c> says whether it was taken; a refusal carries the reason in
/// <c>message</c>. Uploading only writes the portal's own file in the temporary backup store; no restore
/// is started by it.
/// </summary>
public class BackupFileUploadTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    private const string UploadPath = "backupFileUpload.ashx";

    private static readonly JsonSerializerOptions _answerOptions = new() { PropertyNameCaseInsensitive = true };

    private async Task<UploadAnswer> InitAsync(string query)
    {
        using var response = await _backupClient.GetAsync($"{UploadPath}?{query}", TestContext.Current.CancellationToken);

        return await ReadAnswerAsync(response);
    }

    private async Task<UploadAnswer> SendChunkAsync(int length)
    {
        using var form = new MultipartFormDataContent();
        using var chunk = new ByteArrayContent(new byte[length]);
        form.Add(chunk, "file", "chunk");

        using var response = await _backupClient.PostAsync(UploadPath, form, TestContext.Current.CancellationToken);

        return await ReadAnswerAsync(response);
    }

    private static async Task<UploadAnswer> ReadAnswerAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(200, body);

        return JsonSerializer.Deserialize<UploadAnswer>(body, _answerOptions)
            ?? throw new InvalidOperationException($"empty answer of {UploadPath}");
    }

    [Fact]
    public async Task Upload_InitWithoutTotalSize_IsRefused()
    {
        // Act
        var answer = await InitAsync("Init=true&extension=tar.gz");

        // Assert
        answer.Success.Should().BeFalse();
        answer.Message.Should().Be("Total size must be greater than 0.");
    }

    [Fact]
    public async Task Upload_ChunkWithoutInit_IsRefused()
    {
        // Act
        var answer = await SendChunkAsync(4);

        // Assert
        answer.Success.Should().BeFalse();
        answer.Message.Should().Be("Need init upload.");
    }

    [Fact]
    public async Task Upload_ChunksUpToTheTotalSize_EndTheUploadOnTheLastChunk()
    {
        // Act
        var init = await InitAsync("Init=true&totalSize=8&extension=tar");
        var first = await SendChunkAsync(4);
        var last = await SendChunkAsync(4);

        // Assert
        init.Success.Should().BeTrue(init.Message);
        init.ChunkSize.Should().BeGreaterThan(0);
        init.EndUpload.Should().BeFalse();

        first.Success.Should().BeTrue(first.Message);
        first.EndUpload.Should().BeFalse();

        last.Success.Should().BeTrue(last.Message);
        last.EndUpload.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_NewInit_ReplacesTheEarlierFile()
    {
        // Arrange - a finished upload of 4 bytes
        (await InitAsync("Init=true&totalSize=4&extension=tar.gz")).Success.Should().BeTrue();
        (await SendChunkAsync(4)).EndUpload.Should().BeTrue();

        // Act - a new upload of 8 bytes gets its first 4
        (await InitAsync("Init=true&totalSize=8&extension=tar.gz")).Success.Should().BeTrue();
        var answer = await SendChunkAsync(4);

        // Assert - had the earlier 4 bytes been kept, the file would already hold 8 and the upload would end
        answer.Success.Should().BeTrue(answer.Message);
        answer.EndUpload.Should().BeFalse();
    }

    [Fact]
    public async Task Upload_ChunkLargerThanTheChunkSize_IsRefused()
    {
        // Arrange
        var init = await InitAsync("Init=true&totalSize=1&extension=tar.gz");
        init.Success.Should().BeTrue(init.Message);
        var oversized = checked((int)init.ChunkSize + 1);
        (await InitAsync($"Init=true&totalSize={oversized}&extension=tar.gz")).Success.Should().BeTrue();

        // Act
        var answer = await SendChunkAsync(oversized);

        // Assert
        answer.Success.Should().BeFalse();
        answer.Message.Should().Be("chunkSize more then maxChunkUploadSize");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Upload_NonAdmin_IsRefusedWithAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act
        var answer = await InitAsync("Init=true&totalSize=4&extension=tar.gz");

        // Assert
        answer.Success.Should().BeFalse();
        // The permission check names the caller and the missing right
        answer.Message.Should().Contain("access denied \"Edit Portal Settings\"");
    }

    [Fact]
    public async Task Upload_DocSpaceAdmin_IsAccepted()
    {
        // Arrange
        await AuthenticateAsMemberAsync(EmployeeType.DocSpaceAdmin);

        // Act
        var answer = await InitAsync("Init=true&totalSize=4&extension=tar.gz");

        // Assert
        answer.Success.Should().BeTrue(answer.Message);
    }

    private sealed record UploadAnswer(bool Success, string? Message, long ChunkSize, bool EndUpload);
}
