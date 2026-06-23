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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Core.Security.Authorizing;

[Scope(typeof(IPermissionResolver))]
internal class PermissionResolver(AzManager azManager) : IPermissionResolver
{
    private readonly AzManager _azManager = azManager ?? throw new ArgumentNullException(nameof(azManager));

    public async Task<bool> CheckAsync(ISubject subject, IAction action)
    {
        return await CheckAsync(subject, null, null, action);
    }

    public async Task<bool> CheckAsync(ISubject subject, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider, IAction action)
    {
        var denyActions = await GetDenyActionsAsync(subject, action, objectId, securityObjProvider);
        return denyActions == null;
    }

    public async Task DemandAsync(ISubject subject, IAction action)
    {
        await DemandAsync(subject, null, null, action);
    }

    public async Task DemandAsync(ISubject subject, IAction action1, IAction action2)
    {
        IAction[] actions = [action1, action2];

        var denyActions = await GetDenyActionsAsync(subject, actions, null, null);
        if (denyActions.Length > 0)
        {
            throw new AuthorizingException(
                subject,
                Array.ConvertAll(denyActions, r => r.TargetAction),
                Array.ConvertAll(denyActions, r => r.Acl?.DenySubject),
                Array.ConvertAll(denyActions, r => r.Acl?.DenyAction));
        }
    }

    public async Task DemandAsync(ISubject subject, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider, IAction action)
    {
        var denyActions = await GetDenyActionsAsync(subject, action, objectId, securityObjProvider);
        if (denyActions != null)
        {
            throw new AuthorizingException(
                subject,
                denyActions.TargetAction,
                denyActions.Acl?.DenySubject,
                denyActions.Acl?.DenyAction);
        }
    }

    private async Task<DenyResult[]> GetDenyActionsAsync(ISubject subject, IAction[] actions, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider)
    {
        actions ??= [];

        if (subject == null)
        {
            return actions.Select(a => new DenyResult(a)).ToArray();
        }

        if (subject is ISystemAccount && subject.ID == Constants.CoreSystem.ID)
        {
            return [];
        }

        var denyActions = new List<DenyResult>();
        foreach (var action in actions)
        {
            var acl = await _azManager.CheckPermissionAsync(subject, action, objectId, securityObjProvider);
            if (acl.IsAllow)
            {
                continue;
            }

            denyActions.Add(new DenyResult(action, acl));
            break;
        }

        return denyActions.ToArray();
    }

    private async Task<DenyResult> GetDenyActionsAsync(ISubject subject, IAction action, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider)
    {
        if (subject == null)
        {
            return new DenyResult(action);
        }

        if (subject is ISystemAccount && subject.ID == Constants.CoreSystem.ID)
        {
            return null;
        }

        var acl = await _azManager.CheckPermissionAsync(subject, action, objectId, securityObjProvider);
        return !acl.IsAllow ? new DenyResult(action, acl) : null;
    }

    private record DenyResult(IAction TargetAction, AzManagerAcl Acl = null);
}