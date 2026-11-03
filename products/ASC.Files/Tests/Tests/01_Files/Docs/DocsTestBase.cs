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
/// Shared setup for the <c>/api/2.0/docs</c> suites (<c>converter</c>, <c>builder</c>). Only the checks that answer
/// before the document server is reached are covered: the integration-test host's document server cannot download
/// the portal's files, so a conversion or a script that really runs fails there.
/// </summary>
public abstract class DocsTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // DocsApi is not part of the shared PortalClients bundle, so it is built over the same authenticated client.
    protected DocSpace.API.SDK.Api.Docs.DocsApi DocsApi => new(
        _filesClient,
        new Configuration { BasePath = _filesClient.BaseAddress!.ToString().TrimEnd('/') });

    /// <summary>A custom room owned by the owner with an empty docx in it.</summary>
    protected async Task<(FolderDto Room, FileDto File)> CreateRoomWithDocx(string title)
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom(title + " " + Guid.NewGuid().ToString()[..8]);
        var file = await CreateFile(title + ".docx", room.Id);

        return (room, file);
    }

    protected static string BuilderScript(int fileId)
    {
        return $"builder.OpenFile(\"{fileId}\");\nbuilder.SaveFile(\"docx\", \"result.docx\");\nbuilder.CloseFile();";
    }

    protected static Dictionary<string, RunBuilderScriptOutputRequest> BuilderOutputs(int folderId)
    {
        return new() { ["result.docx"] = new RunBuilderScriptOutputRequest(folderId: folderId) };
    }
}
