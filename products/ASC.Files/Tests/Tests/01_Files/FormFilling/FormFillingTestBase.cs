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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// Shared setup for the form filling suites (<c>startfilling</c>, <c>saveediting</c>,
/// <c>checkfillformdraft</c>, <c>manageformfilling</c>). Inherits
/// <c>RoomsPermissionsTestBase</c> (namespace <c>ASC.Files.Tests.Tests._03_Rooms</c>, already
/// brought in through the project's <c>GlobalUsings.cs</c>) purely to reuse its
/// <c>InviteMember</c> / <c>InviteToRoom</c> helpers, the same way <c>RecentTestBase</c> under
/// <c>01_Files/Recent</c> does.
/// </summary>
public abstract class FormFillingTestBase(AspireAppFixture fixture) : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Creates a PDF file directly inside the given room. DocSpace's blank ".pdf" template is
    /// itself an extended (AcroForm) PDF - <c>FileUploader</c>/<c>CreateNewFileAsync</c> run every
    /// new PDF through <c>FileChecker.CheckExtendedPDFstream</c>, and the built-in template passes
    /// that check - so a file created this way is a genuine form (<c>IsForm == true</c>) without
    /// needing a live document server to convert one.
    /// </summary>
    protected async Task<FileDtoInteger> CreateFormInRoom(int roomId, string title = "Autotest Form.pdf")
    {
        return await CreateFile(title, roomId);
    }

    /// <summary>Runs <see cref="FormFillingManageAction.Start"/> on the given form.</summary>
    protected async Task StartFormFilling(int formId)
    {
        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDtoInteger(formId, FormFillingManageAction.Start),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Runs <see cref="FormFillingManageAction.Stop"/> on the given form.</summary>
    protected async Task StopFormFilling(int formId)
    {
        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDtoInteger(formId, FormFillingManageAction.Stop),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Builds a small in-memory "submitted form" payload for <c>saveediting</c>, the same way the
    /// TS suite posts <c>oo-form-submitted.pdf</c> as raw multipart content. The bytes themselves
    /// are never form-checked by <c>SaveEditingAsync</c> - only the resulting content length and the
    /// version bump are observable - so arbitrary content is enough to exercise the endpoint.
    /// </summary>
    protected static FileParameter BuildSubmittedFormFile(int length = 256)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);

        return new FileParameter("oo-form-submitted.pdf", "application/pdf", new MemoryStream(bytes));
    }
}
