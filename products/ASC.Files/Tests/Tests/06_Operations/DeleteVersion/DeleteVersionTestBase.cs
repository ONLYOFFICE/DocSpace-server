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

namespace ASC.Files.Tests.Tests._06_Operations.DeleteVersion;

/// <summary>
/// Shared setup for the <c>PUT /api/2.0/files/fileops/deleteversion</c> suites: request builders,
/// operation polling and the version-history lookup every test needs to observe the outcome.
///
/// Derives from <see cref="RoomsPermissionsTestBase"/> (not <see cref="BaseTest"/> directly) to
/// reuse its <c>InviteMember</c>/<c>InviteToRoom</c>/<c>ArchiveRoom</c> helpers instead of
/// duplicating them here.
/// </summary>
public abstract class DeleteVersionTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Creates a file in the caller's My Documents and bumps it to a second version, which is the
    /// precondition every delete-version test needs.
    /// </summary>
    protected async Task<FileDtoInteger> CreateFileWithSecondVersion(string title, User? user = null)
    {
        var owner = user ?? Owner;
        var file = await CreateFileInMy(title, owner);

        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        return file;
    }

    /// <summary>
    /// Deletes the given file versions and waits for the resulting operation to finish. Returns the
    /// operation the server first reported, so a caller checking its shape does not have to redo the
    /// call.
    /// </summary>
    /// <summary>A version-delete operation as the tests see it.</summary>
    protected sealed record VersionDeleteOperation(string? Id);

    /// <remarks>
    /// Sent over raw HTTP: the endpoint answers with an <b>array</b> of operations, while the
    /// generated <c>DeleteFileVersionsAsync</c> is typed to a single <c>FileOperationDto</c>
    /// (<c>FileOperationWrapper</c>), so every successful call dies in the client's deserializer —
    /// an SDK/OpenAPI generation defect, not a preference for raw HTTP.
    /// </remarks>
    protected async Task<VersionDeleteOperation?> DeleteVersionsAndWait(int fileId, List<int> versions, bool returnSingleOperation = false)
    {
        var payload = JsonSerializer.Serialize(new { fileId, versions, returnSingleOperation });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _filesClient.PutAsync("api/2.0/files/fileops/deleteversion", content, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Error calling DeleteFileVersions: {body}");
        }

        using var json = JsonDocument.Parse(body);
        var operations = json.RootElement.GetProperty("response");

        var operation = operations.ValueKind == JsonValueKind.Array && operations.GetArrayLength() > 0
            ? new VersionDeleteOperation(operations[0].TryGetProperty("id", out var id) ? id.GetString() : null)
            : null;

        await WaitLongOperation(operation?.Id);

        return operation;
    }

    /// <summary>Reads the version numbers still present in a file's history.</summary>
    protected async Task<List<int>> GetVersionNumbers(int fileId)
    {
        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return versions.ConvertAll(v => v.Version);
    }

    /// <summary>
    /// Sends the delete-version request over raw HTTP with an explicit <c>null</c> "versions" array.
    /// <see cref="DeleteVersionBatchRequestDto"/>'s only public constructor rejects a null
    /// <c>versions</c> client-side (it is declared as a required, non-nullable constructor
    /// parameter), so the typed SDK can never send this body - this is the carve-out tests.md
    /// documents for exactly that case.
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteVersionsWithNullVersionsRaw(int fileId)
    {
        var payload = JsonSerializer.Serialize(new { fileId, versions = (int[]?)null });
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/2.0/files/fileops/deleteversion")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
