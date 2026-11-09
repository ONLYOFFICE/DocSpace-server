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
    ByokKeyStorage byokKeyStorage,
    IDistributedLockProvider distributedLockProvider,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway aiGateway,
    ProfileReader profileReader,
    MessageService messageService) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, aiGateway)
{
    private readonly AiGateway _aiGateway = aiGateway;

    private static readonly IEnumerable<EmployeeType> _writeTypes = [EmployeeType.DocSpaceAdmin];
    private static readonly IEnumerable<EmployeeType> _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async Task<Profile> CreateAsync(ProfileData profile)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        var created = await storage.CreateAsync(tenantManager.GetCurrentTenantId(), profile);

        messageService.Send(MessageAction.AiProfileCreated, MessageTarget.Create(created.Id), created.Name);

        return created;
    }

    public async Task<IReadOnlyList<Profile>> CreateManyAsync(IReadOnlyList<ProfileData> profiles)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        var created = await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), profiles);

        if (created.Count > 0)
        {
            messageService.Send(MessageAction.AiProfileCreated, MessageTarget.Create(created.Select(p => p.Id)), created.Select(p => p.Name));
        }

        return created;
    }

    public async Task<ByokKeyDto> SetByokKeyAsync(Guid id, string provider, string key)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayConfigured();

        var model = await FindByokModelAsync(id) ?? throw new ItemNotFoundException();
        var type = GetByokType(model) ?? throw new ItemNotFoundException();

        if (!model.AcceptsByokProvider(provider))
        {
            throw new ArgumentException(ErrorMessages.ModelNotAvailableForProvider);
        }

        if (!await _aiGateway.VerifyByokKeyAsync(provider, key))
        {
            throw new ArgumentException(ErrorMessages.InvalidKey);
        }

        await byokKeyStorage.SetAsync(tenantManager.GetCurrentTenantId(), id, type, provider, key);

        messageService.Send(MessageAction.AiByokKeySet, MessageTarget.Create(id), model.Alias);

        return new ByokKeyDto { Id = id, Name = model.Alias, Provider = provider, Image = AiModelImages.Get(model.OwnedBy) };
    }

    public async Task DeleteByokKeyAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayConfigured();

        var name = await TryFindByokModelNameAsync(id) ?? id.ToString();

        if (!await byokKeyStorage.DeleteAsync(tenantManager.GetCurrentTenantId(), id))
        {
            throw new ItemNotFoundException();
        }

        messageService.Send(MessageAction.AiByokKeyDeleted, MessageTarget.Create(id), name);
    }

    public async Task<List<ByokKeyDto>> ReadByokKeysAsync()
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayConfigured();

        var keys = await byokKeyStorage.ReadAllAsync(tenantManager.GetCurrentTenantId());
        if (keys.Count == 0)
        {
            return [];
        }

        var models = (await _aiGateway.GetAllModelsAsync()).Data
            .DistinctBy(m => m.RevisionId)
            .ToDictionary(m => m.RevisionId);

        return
        [
            .. keys
                .Select(k =>
                {
                    var model = models.GetValueOrDefault(k.Id);

                    return new ByokKeyDto
                    {
                        Id = k.Id,
                        Name = model?.Alias,
                        Provider = k.Provider,
                        Image = AiModelImages.Get(model?.OwnedBy)
                    };
                })
        ];
    }

    public async Task<List<ByokProviderDto>> ReadByokProvidersAsync()
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayConfigured();

        var models = (await _aiGateway.GetAllModelsAsync()).Data;
        var overridden = (await byokKeyStorage.ReadAllAsync(tenantManager.GetCurrentTenantId())).Select(k => k.Id).ToHashSet();

        return
        [
            .. models
                .Where(m => GetByokType(m).HasValue && !overridden.Contains(m.RevisionId))
                .SelectMany(m => (m.ByokProviders ?? []).Select(p => (Provider: p, Model: m)))
                .GroupBy(x => x.Provider.Id)
                .Select(g => new ByokProviderDto
                {
                    Id = g.Key,
                    Name = g.First().Provider.Name,
                    Image = AiModelImages.Get(GetSingleOwner(g.Select(x => x.Model))),
                    Models = [.. g.Select(x => new ByokModelDto { Id = x.Model.RevisionId, Name = x.Model.Alias })]
                })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public async Task<Profile> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_readTypes);

        var profile = await profileReader.ReadByIdAsync(tenantManager.GetCurrentTenantId(), id);

        return profile ?? throw new ItemNotFoundException();
    }

    public async Task<List<Profile>> ReadAllAsync()
    {
        await AssertUserHasAccessAsync(_readTypes);

        return await ReadAllVerifiedAsync();
    }

    public async Task<Profile> UpdateAsync(Profile profile)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        var updated = await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), profile);

        messageService.Send(MessageAction.AiProfileUpdated, MessageTarget.Create(updated.Id), updated.Name);

        return updated;
    }

    public async Task DeleteAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_writeTypes);
        AssertGatewayNotConfigured();

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(ProfileStorage.GetLockKey(tenantId, id)))
        {
            var profile = await storage.ReadByIdAsync(tenantId, id);

            await storage.DeleteAsync(tenantId, id);

            if (profile is not null)
            {
                messageService.Send(MessageAction.AiProfileDeleted, MessageTarget.Create(profile.Id), profile.Name);
            }
        }
    }

    internal async Task<List<Profile>> ReadAllVerifiedAsync(List<Model>? models = null)
    {
        return await profileReader.ReadAllAsync(tenantManager.GetCurrentTenantId(), models);
    }

    internal async Task<ByokCredentials> ReadByokCredentialsAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_readTypes);
        AssertGatewayConfigured();

        return await byokKeyStorage.ReadCredentialsAsync(tenantManager.GetCurrentTenantId(), id) ?? throw new ItemNotFoundException();
    }

    private void AssertGatewayConfigured()
    {
        if (!_aiGateway.Configured)
        {
            throw new ItemNotFoundException();
        }
    }

    private async Task<Model?> FindByokModelAsync(Guid id)
    {
        var models = (await _aiGateway.GetAllModelsAsync()).Data;

        return models.FirstOrDefault(m => m.RevisionId == id);
    }

    private async Task<string?> TryFindByokModelNameAsync(Guid id)
    {
        try
        {
            return (await FindByokModelAsync(id))?.Alias;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or TimeoutException or JsonException)
        {
            return null;
        }
    }

    private static string? GetSingleOwner(IEnumerable<Model> models)
    {
        var owners = models.Select(m => m.OwnedBy).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).ToList();
        return owners.Count == 1 ? owners[0] : null;
    }

    private static ByokKeyType? GetByokType(Model model)
    {
        return ByokKeyTypeExtensions.TryParse(model.Type, ignoreCase: true, out var type) ? type : null;
    }
}
