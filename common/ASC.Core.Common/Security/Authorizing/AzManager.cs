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

namespace ASC.Common.Security.Authorizing;

[Scope]
public class AzManager(IRoleProvider roleProvider, IPermissionProvider permissionProvider)
{
    private readonly IPermissionProvider _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));
    private readonly IRoleProvider _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));

    internal async Task<AzManagerAcl> CheckPermissionAsync(ISubject subject, IAction action, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(subject);

        if (action.AdministratorAlwaysAllow && (AuthConstants.DocSpaceAdmin.ID == subject.ID || await _roleProvider.IsSubjectInRoleAsync(subject, AuthConstants.DocSpaceAdmin)
            || (objectId is SecurityObject obj && await obj.IsMatchDefaultRulesAsync(subject, action, _roleProvider))))
        {
            return AzManagerAcl.Allow;
        }

        var acl = AzManagerAcl.Default;
        var exit = false;

        foreach (var s in await GetSubjectsAsync(subject, objectId, securityObjProvider))
        {
            var aceList = await _permissionProvider.GetAclAsync(s, action, objectId, securityObjProvider);
            foreach (var reaction in aceList.Select(r => r.Reaction))
            {
                if (reaction == AceType.Deny)
                {
                    acl.IsAllow = false;
                    acl.DenySubject = s;
                    acl.DenyAction = action;
                    exit = true;
                }
                if (reaction == AceType.Allow && !exit)
                {
                    acl.IsAllow = true;
                    if (!action.Conjunction)
                    {
                        // disjunction: first allow and exit
                        exit = true;
                    }
                }
                if (exit)
                {
                    break;
                }
            }
            if (exit)
            {
                break;
            }
        }

        return acl;
    }

    private async Task<IEnumerable<ISubject>> GetSubjectsAsync(ISubject subject, ISecurityObjectId objectId, ISecurityObjectProvider securityObjProvider)
    {
        var subjects = new List<ISubject>
            {
                subject
            };
        subjects.AddRange((await _roleProvider.GetRolesAsync(subject)).ConvertAll(ISubject (r) => r));
        if (objectId != null)
        {
            var secObjProviderHelper = new AzObjectSecurityProviderHelper(objectId, securityObjProvider);
            do
            {
                if (!secObjProviderHelper.ObjectRolesSupported)
                {
                    continue;
                }

                foreach (var role in secObjProviderHelper.GetObjectRoles(subject))
                {
                    if (!subjects.Contains(role))
                    {
                        subjects.Add(role);
                    }
                }
            } while (secObjProviderHelper.NextInherit());
        }

        return subjects;
    }
}

internal class AzManagerAcl
{
    public IAction DenyAction { get; set; }
    public ISubject DenySubject { get; set; }
    public bool IsAllow { get; set; }
    public static AzManagerAcl Allow => new() { IsAllow = true };
    public static AzManagerAcl Default => new() { IsAllow = false };
}