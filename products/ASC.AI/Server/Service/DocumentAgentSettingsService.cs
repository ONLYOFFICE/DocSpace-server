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
public class DocumentAgentSettingsService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    ProfileReader profileReader,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway gateway,
    MessageService messageService) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, gateway)
{
    private static readonly EmployeeType[] _writeTypes = [EmployeeType.DocSpaceAdmin];
    private static readonly EmployeeType[] _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async Task<DocumentAgentSettings> ReadAsync()
    {
        await AssertUserHasAccessAsync(_readTypes);

        var tenantId = tenantManager.GetCurrentTenantId();
        var settings = await settingsManager.LoadAsync<DocumentAgentSettings>(tenantId);

        var profileId = settings.ProfileId is { } id && await profileReader.ReadByIdAsync(tenantId, id) is not null
            ? settings.ProfileId
            : null;

        var taskLogFolderId = settings.TaskLogFolderId is { } folderId && await FindFolderAsync(folderId) is not null
            ? settings.TaskLogFolderId
            : null;

        return settings with { ProfileId = profileId, TaskLogFolderId = taskLogFolderId };
    }

    internal async Task<bool> IsEnabledVerifiedAsync()
    {
        var settings = await settingsManager.LoadAsync<DocumentAgentSettings>(tenantManager.GetCurrentTenantId());

        return settings.Enabled;
    }

    public async Task<DocumentAgentSettings> UpdateAsync(DocumentAgentSettings settings)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        if (!DocumentAgentReasoningExtensions.IsDefined(settings.Reasoning))
        {
            throw new ArgumentOutOfRangeException(nameof(settings.Reasoning), settings.Reasoning, null);
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(settings.MaxToolCalls, 1, nameof(settings.MaxToolCalls));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.MaxFileBytes, 1, nameof(settings.MaxFileBytes));

        var tenantId = tenantManager.GetCurrentTenantId();

        if (settings.ProfileId is { } profileId && await profileReader.ReadByIdAsync(tenantId, profileId) is null)
        {
            throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(settings.ProfileId));
        }

        if (settings.TaskLogFolderId is { } folderId)
        {
            var folder = await FindFolderAsync(folderId)
                         ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);

            if (!await FileSecurity.CanCreateAsync(folder))
            {
                throw new SecurityException();
            }
        }

        await settingsManager.SaveAsync(settings, tenantId);

        messageService.Send(MessageAction.AiDocumentAgentSettingsUpdated);

        return settings;
    }

    private async Task<Folder<int>?> FindFolderAsync(int folderId)
    {
        var folder = await DaoFactory.GetFolderDao<int>().GetFolderAsync(folderId);

        return folder is null || folder.RootFolderType == FolderType.TRASH ? null : folder;
    }
}
