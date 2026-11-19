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

namespace ASC.Files.Tests.Tests._07_Settings.Editor;

/// <summary>Not run in parallel: xUnit runs it after the parallel collections, so nothing else is running then.</summary>
[CollectionDefinition(nameof(InstallationSettingsCollection), DisableParallelization = true)]
public class InstallationSettingsCollection;

/// <summary>
/// <c>PUT /files/docservice</c> with an address where no Document Server answers: the check fails, the previous
/// settings come back, and the call returns 502 - a failure of the server behind the portal. The controller wrapped
/// the failed check in a plain exception, and a 500 came back.
/// </summary>
/// <remarks>
/// The test stack is a standalone installation, where the addresses are stored for the whole installation, not per
/// portal. Until the rollback, every portal of the run would talk to the dead address, so the class runs alone.
/// </remarks>
[Trait("Category", "Settings")]
[Collection(nameof(InstallationSettingsCollection))]
public class CheckDocServiceUrlUnreachableTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CheckDocServiceUrl_NothingAnswersAtAddress_Returns502AndKeepsSettings()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var before = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act - nothing listens on port 1 of the host the Files service runs on, so every check request is refused.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.CheckDocServiceUrlAsync(
                new CheckDocServiceUrlRequestDto(docServiceUrl: "http://127.0.0.1:1/"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);

        var after = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.DocServiceUrl.Should().Be(before.DocServiceUrl);
        after.DocServiceUrlInternal.Should().Be(before.DocServiceUrlInternal);
        after.IsDefault.Should().Be(before.IsDefault);
    }
}
