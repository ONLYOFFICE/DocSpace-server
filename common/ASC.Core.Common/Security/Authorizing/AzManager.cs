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
            foreach (var reaction in aceList.Select(r=> r.Reaction))
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
