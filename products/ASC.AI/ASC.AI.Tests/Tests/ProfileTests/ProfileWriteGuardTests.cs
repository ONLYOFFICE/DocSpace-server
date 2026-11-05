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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// <c>profiles/create</c>, <c>update</c> and <c>delete</c>: the base URL and key guards in front of a write, and who
/// may write at all (a DocSpace administrator only - a refusal must leave the catalogue exactly as it was).
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "AI/Profiles")]
public class ProfileWriteGuardTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://ssrf-attacker.invalid:9999/models")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://user:pass@example.com/v1")]
    [InlineData("http://[fe80::1]/v1")]
    public async Task Create_ForbiddenBaseUrl_IsRefusedBeforeConnectAndNothingIsStored(string baseUrl)
    {
        var input = BuildProfileInput();
        input.BaseUrl = baseUrl;

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty("a profile with a forbidden URL is not persisted");
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://ssrf-attacker.invalid:9999/models")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://user:pass@example.com/v1")]
    [InlineData("http://[fe80::1]/v1")]
    public async Task Update_ForbiddenBaseUrl_IsRefusedBeforeConnectAndTheProfileIsUnchanged(string baseUrl)
    {
        var created = await CreateProfileAsync();
        var update = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        update.BaseUrl = baseUrl;
        // The key-reuse variant of the attack: a new URL and no new key must not send the stored key anywhere.
        update.Key = null!;

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(update, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
        (await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken)).BaseUrl.Should().Be(ModelProvider.BaseUrl);
    }

    /// <summary>Bug 84428: a key that cannot travel in an HTTP header is the caller's input error on create too, not a provider failure.</summary>
    [Fact]
    [Trait("Bug", "84428")]
    public async Task Create_KeyThatCannotBeSentAsAnHttpHeader_Returns400_AndNothingIsStored()
    {
        string[] unsendableKeys = ["ключ-abc", "﻿sk-abc", "sk-abc\nX-A: b"];
        var notRefused = new List<string>();

        foreach (var key in unsendableKeys)
        {
            var input = BuildProfileInput();
            input.Key = key;

            var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken));

            if (outcome.Status != 400 || !outcome.Body.Contains("cannot be sent in an HTTP header"))
            {
                notRefused.Add($"{(int)key[0]:X4}: {outcome.Status} {outcome.Body}");
            }
        }

        notRefused.Should().BeEmpty("keys that were not refused as unsendable");
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateUpdateDelete_MemberBelowDocSpaceAdmin_Returns403_AndTheCatalogueIsUntouched(EmployeeType role)
    {
        var profile = await CreateProfileAsync();
        var before = await CatalogueSnapshotAsync();
        var renamed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        renamed.Name = "Autotest member rename";

        await ActAsAsync(role);
        var created = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(BuildProfileInput("Autotest member create"), TestContext.Current.CancellationToken));
        var updated = await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(renamed, TestContext.Current.CancellationToken));
        var deleted = await OutcomeOfAsync(() => _aiProfiles.AiProfilesDeleteAsync(profile.Id, TestContext.Current.CancellationToken));

        // Side effects first: a write that landed matters more than its status.
        await ActAsOwnerAsync();
        (await CatalogueSnapshotAsync()).Should().Be(before, $"{role} changed the catalogue");

        created.Status.Should().Be(403, $"{role} create: {created.Body}");
        updated.Status.Should().Be(403, $"{role} update: {updated.Body}");
        deleted.Status.Should().Be(403, $"{role} delete: {deleted.Body}");
    }

    [Fact]
    public async Task CreateUpdateDelete_DocSpaceAdmin_MayWrite()
    {
        await ActAsAsync(EmployeeType.DocSpaceAdmin);

        var created = (await _aiProfiles.AiProfilesCreateAsync(BuildProfileInput("Autotest admin create"), TestContext.Current.CancellationToken)).Profile;
        var update = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        update.Name = "Autotest admin rename";
        var updated = await _aiProfiles.AiProfilesUpdateAsync(update, TestContext.Current.CancellationToken);
        var deleted = await _aiProfiles.AiProfilesDeleteAsync(created.Id, TestContext.Current.CancellationToken);

        updated.Success.Should().BeTrue($"{updated.Error}");
        updated.Profile.Name.Should().Be("Autotest admin rename");
        deleted.Success.Should().BeTrue();
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }
}
