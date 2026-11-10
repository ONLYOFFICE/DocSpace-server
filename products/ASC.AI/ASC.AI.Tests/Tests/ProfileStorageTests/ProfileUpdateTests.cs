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

namespace ASC.AI.Tests.Tests.ProfileStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Profiles")]
public class ProfileUpdateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Update_Owner_PersistsChanges()
    {
        var created = await CreateProfileAsync();

        // The provider, base URL and model stay on the stub: the chat service re-checks the key against the
        // provider on every update, and re-probes the model when any of the three changes.
        created.Name = "renamed-profile";

        var result = await _aiProfiles.AiProfilesUpdateAsync(created, TestContext.Current.CancellationToken);

        // Described only on a refusal: the provider's log holds every request of the run.
        if (!result.Success)
        {
            result.Success.Should().BeTrue($"profiles/update rejected the profile: {result.Error}\n{ModelProvider.Describe()}");
        }

        result.Profile.Id.Should().Be(created.Id);
        result.Profile.Name.Should().Be("renamed-profile");
        result.Profile.ProviderType.GetString().Should().Be("openai");
        result.Profile.BaseUrl.Should().Be(ModelProvider.BaseUrl);
        result.Profile.ModelId.Should().Be(StubModelProvider.ModelId);

        var fetched = await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken);

        fetched.Name.Should().Be("renamed-profile");
        fetched.ProviderType.GetString().Should().Be("openai");
        fetched.BaseUrl.Should().Be(ModelProvider.BaseUrl);
        fetched.ModelId.Should().Be(StubModelProvider.ModelId);
        fetched.UseResponsesApi.Should().Be(created.UseResponsesApi);
        fetched.CanUseTool.Should().Be(created.CanUseTool);

        // The key is only on the list (the single read drops it), and only for an administrator.
        var listed = await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken);
        listed.Should().ContainSingle(p => p.Id == created.Id).Which.Key.Should().Be(StubModelProvider.Key);
    }
}
