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

namespace ASC.Files.Tests.Tests._07_Settings.Storage;

/// <summary>
/// Shared setup for the storage-related file settings suites (thirdparty access, forcesave,
/// display extension/recent, auto-cleanup, store original/forcesave). Every one of these settings
/// is stored per user, not per portal, which is why the suites keep testing isolation between
/// portal members alongside the plain toggle behaviour.
/// </summary>
public abstract class StorageSettingsTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Sends a raw PUT with an explicit <c>Content-Type: application/json</c> header and an empty
    /// JSON object body. The generated SDK drops the header together with the body when the request
    /// DTO is left null, which ASP.NET then refuses with 415 before the controller runs — a real
    /// "no body" caller still sends the header, so this is what a "no body" case has to exercise.
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawEmptyBodyPut(string path)
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        return await _filesClient.PutAsync(path, content, TestContext.Current.CancellationToken);
    }
}
