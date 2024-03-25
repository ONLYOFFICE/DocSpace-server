// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Core.Security.Authorizing;

[Scope]
class PermissionResolver(AzManager azManager) : IPermissionResolver
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
        actions ??= Array.Empty<IAction>();

        if (subject == null)
        {
            return actions.Select(a => new DenyResult(a)).ToArray();
        }

        if (subject is ISystemAccount && subject.ID == Constants.CoreSystem.ID)
        {
            return Array.Empty<DenyResult>();
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
