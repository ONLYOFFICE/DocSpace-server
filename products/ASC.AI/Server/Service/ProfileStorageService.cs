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

namespace ASC.AI.Service;

[Scope]
public class ProfileStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    ProfileStorage storage,
    IDistributedLockProvider distributedLockProvider,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway aiGateway,
    BaseCommonLinkUtility linkUtility) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity)
{
    private static readonly IEnumerable<EmployeeType> _writeTypes = [EmployeeType.DocSpaceAdmin];
    private static readonly IEnumerable<EmployeeType> _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    private const string ImageModality = "image";
    private const string ToolsCapability = "tools";

    public async Task<Profile> CreateAsync(ProfileData profile)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        return await storage.CreateAsync(tenantManager.GetCurrentTenantId(), profile);
    }

    public async Task<IReadOnlyList<Profile>> CreateManyAsync(IReadOnlyList<ProfileData> profiles)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        return await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), profiles);
    }

    public async Task<Profile> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_readTypes);

        if (aiGateway.Configured)
        {
            var profiles = await GetGatewayProfilesAsync();
            return profiles.FirstOrDefault(p => p.Id == id) ?? throw new ItemNotFoundException();
        }

        var profile = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), id);

        return profile ?? throw new ItemNotFoundException();
    }

    public async Task<List<Profile>> ReadAllAsync()
    {
        await AssertUserHasAccessAsync(_readTypes);

        if (!aiGateway.Configured)
        {
            return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId());
        }

        var profiles = await GetGatewayProfilesAsync();
        return profiles.ToList();
    }

    public async Task<Profile> UpdateAsync(Profile profile)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        return await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), profile);
    }

    public async Task DeleteAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(ProfileStorage.GetLockKey(tenantId, id)))
        {
            await storage.DeleteAsync(tenantId, id);
        }
    }

    private void AssertGatewayNotConfigured()
    {
        if (aiGateway.Configured)
        {
            throw new SecurityException("Profile modification is not allowed when the AI Gateway is configured");
        }
    }

    private async Task<IEnumerable<Profile>> GetGatewayProfilesAsync()
    {
        var response = await aiGateway.GetModelsAsync();

        return response.Data
            .Where(m => !string.Equals(m.Type, "embedding", StringComparison.OrdinalIgnoreCase))
            .Select(m => new Profile
            {
                Id = m.RevisionId,
                Name = m.Alias,
                ProviderType = "openaicompatible",
                BaseUrl = linkUtility.GetFullAbsolutePath("api/2.0/ai/gateway"),
                ModelId = m.Id,
                Reasoning = HasCapability(m, "reasoning"),
                CanUseTool = HasCapability(m, ToolsCapability),
                Capabilities = MapCapabilities(m),
                UseResponsesApi = false,
                Key = "onlyoffice"
            });
    }

    private static Capabilities MapCapabilities(Model model)
    {
        var capabilities = Capabilities.None;

        if (string.Equals(model.Type, "chat", StringComparison.OrdinalIgnoreCase))
        {
            capabilities |= Capabilities.Chat;
        }
        else if (string.Equals(model.Type, "image", StringComparison.OrdinalIgnoreCase))
        {
            capabilities |= Capabilities.Image;
        }

        if (HasModality(model.InputModalities, ImageModality))
        {
            capabilities |= Capabilities.Vision;
        }

        if (HasModality(model.OutputModalities, ImageModality))
        {
            capabilities |= Capabilities.Image;
        }

        if (HasCapability(model, ToolsCapability))
        {
            capabilities |= Capabilities.Tools;
        }

        return capabilities;
    }

    private static bool HasCapability(Model model, string capability)
    {
        return model.Capabilities?.Contains(capability, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static bool HasModality(IEnumerable<string> modalities, string modality)
    {
        return modalities.Contains(modality, StringComparer.OrdinalIgnoreCase);
    }
}
