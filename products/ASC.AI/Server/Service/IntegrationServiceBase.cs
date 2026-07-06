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

public abstract class IntegrationServiceBase(
    UserManager userManager,
    AuthContext authContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway aiGateway)
{
    protected Guid CurrentUserId => authContext.CurrentAccount.ID;

    protected async Task<int?> AssertUserHasAccessAsync(IEnumerable<EmployeeType> types, string? entityId = null)
    {
        var type = await userManager.GetUserTypeAsync(CurrentUserId);
        if (!types.Contains(type))
        {
            throw new SecurityException();
        }

        if (entityId == null)
        {
            return null;
        }

        if (!int.TryParse(entityId, out var parsed))
        {
            throw new ArgumentException($"entityId must be a numeric folder id, got '{entityId}'");
        }

        int? entryId = parsed;

        var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(entryId.Value)
                     ?? throw new ItemNotFoundException();

        if (!await fileSecurity.CanUseAiAsync(folder))
        {
            throw new SecurityException();
        }

        return entryId;
    }

    protected void AssertGatewayNotConfigured()
    {
        if (aiGateway.Configured)
        {
            throw new SecurityException("Profile modification is not allowed when the AI Gateway is configured");
        }
    }
}
