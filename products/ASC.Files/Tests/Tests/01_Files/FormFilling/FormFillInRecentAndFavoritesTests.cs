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
/// Recent and Favorites are views of entries kept elsewhere: a PDF form listed there carries the
/// fill right it has in its own folder, so the client offers Fill there as well. Covers a form in
/// My documents (the Files section) and a started form in a form filling room (the Forms section).
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormFillInRecentAndFavoritesTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    [Fact]
    public async Task Recent_FormInMyDocuments_CanBeFilled()
    {
        await _filesClient.Authenticate(Owner);
        var form = await CreateFileInMy("Autotest Recent Form.pdf", Owner);
        await AssertFillableInItsFolder(form.Id);

        await _filesApi.AddFileToRecentAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken);

        var listed = await PollRecentFor(form.Id);
        listed.Security.FillForms.Should().BeTrue("a form listed in Recent keeps the fill right of its own folder");
    }

    [Fact]
    public async Task Recent_StartedFormInFormFillingRoom_CanBeFilled()
    {
        var (_, formId) = await CreateStartedForm("Autotest Recent Forms Room");
        await AssertFillableInItsFolder(formId);

        await _filesApi.AddFileToRecentAsync(formId, cancellationToken: TestContext.Current.CancellationToken);

        var listed = await PollRecentFor(formId, [FolderType.FillingFormsRoom]);
        listed.Security.FillForms.Should().BeTrue("a form listed in Forms → Recent keeps the fill right of its room");
    }

    [Fact]
    public async Task Favorites_FormInMyDocuments_CanBeFilled()
    {
        await _filesClient.Authenticate(Owner);
        var form = await CreateFileInMy("Autotest Favorite Form.pdf", Owner);
        await AssertFillableInItsFolder(form.Id);

        await _filesApi.ToggleFileFavoriteAsync(form.Id, true, TestContext.Current.CancellationToken);

        var listed = await PollFavoritesFor(form.Id);
        listed.Security.FillForms.Should().BeTrue("a form listed in Favorites keeps the fill right of its own folder");
    }

    [Fact]
    public async Task Favorites_StartedFormInFormFillingRoom_CanBeFilled()
    {
        var (_, formId) = await CreateStartedForm("Autotest Favorite Forms Room");
        await AssertFillableInItsFolder(formId);

        await _filesApi.ToggleFileFavoriteAsync(formId, true, TestContext.Current.CancellationToken);

        var listed = await PollFavoritesFor(formId);
        listed.Security.FillForms.Should().BeTrue("a form listed in Forms → Favorites keeps the fill right of its room");
    }

    /// <summary>The precondition: outside Recent and Favorites the caller may fill the form.</summary>
    private async Task AssertFillableInItsFolder(int formId)
    {
        var file = (await _filesApi.GetFileInfoAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        file.IsForm.Should().BeTrue();
        file.Security.FillForms.Should().BeTrue("the caller may fill the form in its own folder");
    }

    /// <summary>Polls Recent on a deadline until the form is listed; adding to Recent is applied asynchronously.</summary>
    private async Task<FileDto> PollRecentFor(int formId, List<FolderType>? folderType = null)
    {
        var recentId = await GetFolderIdAsync(FolderType.Recent, Owner);
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var recent = (await _foldersApi.GetFolderByFolderIdAsync(
                recentId,
                folderType: folderType?.Select(r => (int)r).ToList(),
                cancellationToken: TestContext.Current.CancellationToken)).Response;

            var listed = recent.Files.OfType<FileDto>().FirstOrDefault(f => f.Id == formId);
            if (listed != null)
            {
                return listed;
            }

            DateTime.UtcNow.Should().BeBefore(deadline, "the form must appear in Recent within the deadline");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Polls Favorites on a deadline until the form is listed; the section reads the search index, written asynchronously.</summary>
    private async Task<FileDto> PollFavoritesFor(int formId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var favorites = (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            var listed = favorites.Files.OfType<FileDto>().FirstOrDefault(f => f.Id == formId);
            if (listed != null)
            {
                return listed;
            }

            DateTime.UtcNow.Should().BeBefore(deadline, "the form must appear in Favorites within the deadline");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
