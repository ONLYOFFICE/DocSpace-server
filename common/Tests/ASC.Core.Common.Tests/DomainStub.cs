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

#if DEBUG
using System;
using System.Collections.Generic;

using ASC.Common.Security;
using ASC.Common.Security.Authentication;
using ASC.Common.Security.Authorizing;

namespace ASC.Common.Tests.Security.Authorizing
{
    class UserAccount : Account, IUserAccount
    {
        public UserAccount(Guid id, string name)
            : base(id, name, true)
        {
        }

        public string FirstName
        {
            get;
            private set;
        }

        public string LastName
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public string Department
        {
            get;
            private set;
        }

        public string Email
        {
            get;
            private set;
        }

        public int Tenant
        {
            get;
            private set;
        }
    }

    class AccountS : UserAccount
    {

        public AccountS(Guid id, string name)
            : base(id, name)
        {
        }
    }

    class Role : IRole
    {
        public Role(Guid id, string name)
        {
            ID = id;
            Name = name;
        }

        public Guid ID
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public bool IsAuthenticated
        {
            get;
            private set;
        }

        public string AuthenticationType
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("Role: {0}", Name);
        }
    }


    sealed class RoleFactory : IRoleProvider
    {

        public readonly Dictionary<ISubject, List<IRole>> AccountRoles = new Dictionary<ISubject, List<IRole>>(10);

        public readonly Dictionary<IRole, List<ISubject>> RoleAccounts = new Dictionary<IRole, List<ISubject>>(10);

        public void AddAccountInRole(ISubject account, IRole role)
        {
            if (!AccountRoles.TryGetValue(account, out var roles))
            {
                roles = new List<IRole>(10);
                AccountRoles.Add(account, roles);
            }
            if (!roles.Contains(role)) roles.Add(role);

            if (!RoleAccounts.TryGetValue(role, out var accounts))
            {
                accounts = new List<ISubject>(10);
                RoleAccounts.Add(role, accounts);
            }
            if (!accounts.Contains(account)) accounts.Add(account);
        }

        #region IRoleProvider Members

        public List<IRole> GetRoles(ISubject account)
        {
            if (!AccountRoles.TryGetValue(account, out var roles)) roles = new List<IRole>();
            return roles;
        }

        public List<ISubject> GetSubjects(IRole role)
        {
            if (!RoleAccounts.TryGetValue(role, out var accounts)) accounts = new List<ISubject>();
            return accounts;
        }

        public bool IsSubjectInRole(ISubject account, IRole role)
        {
            var roles = GetRoles(account);
            return roles.Contains(role);
        }



        #endregion
    }

    sealed class PermissionFactory : IPermissionProvider
    {

        private readonly Dictionary<string, PermissionRecord> permRecords = new Dictionary<string, PermissionRecord>();

        private readonly Dictionary<string, bool> inheritAces = new Dictionary<string, bool>();


        public void AddAce(ISubject subject, IAction action, AceType reaction)
        {
            AddAce(subject, action, null, reaction);
        }

        public void AddAce(ISubject subject, IAction action, ISecurityObjectId objectId, AceType reaction)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (action == null) throw new ArgumentNullException("action");

            var r = new PermissionRecord(subject.ID, action.ID, AzObjectIdHelper.GetFullObjectId(objectId), reaction);
            if (!permRecords.ContainsKey(r.Id))
            {
                permRecords.Add(r.Id, r);
            }
        }

        public void RemoveAce<T>(ISubject subject, IAction action, ISecurityObjectId objectId, AceType reaction)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (action == null) throw new ArgumentNullException("action");

            var r = new PermissionRecord(subject.ID, action.ID, AzObjectIdHelper.GetFullObjectId(objectId), reaction);
            if (permRecords.ContainsKey(r.Id))
            {
                permRecords.Remove(r.Id);
            }
        }

        public void SetObjectAcesInheritance(ISecurityObjectId objectId, bool inherit)
        {
            var fullObjectId = AzObjectIdHelper.GetFullObjectId(objectId);
            inheritAces[fullObjectId] = inherit;
        }

        public bool GetObjectAcesInheritance(ISecurityObjectId objectId)
        {
            var fullObjectId = AzObjectIdHelper.GetFullObjectId(objectId);
            return !inheritAces.ContainsKey(fullObjectId) || inheritAces[fullObjectId];
        }

        #region IPermissionProvider Members

        public IEnumerable<Ace> GetAcl(ISubject subject, IAction action)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (action == null) throw new ArgumentNullException("action");

            var acl = new List<Ace>();
            foreach (var entry in permRecords)
            {
                var pr = entry.Value;
                if (pr.SubjectId == subject.ID && pr.ActionId == action.ID && pr.ObjectId == null)
                {
                    acl.Add(new Ace(action.ID, pr.Reaction));
                }
            }
            return acl;
        }

        public IEnumerable<Ace> GetAcl(ISubject subject, IAction action, ISecurityObjectId objectId, ISecurityObjectProvider secObjProvider)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (action == null) throw new ArgumentNullException("action");
            if (objectId == null) throw new ArgumentNullException("objectId");

            var allAces = new List<Ace>();
            var fullObjectId = AzObjectIdHelper.GetFullObjectId(objectId);

            allAces.AddRange(GetAcl(subject, action, fullObjectId));

            var inherit = GetObjectAcesInheritance(objectId);
            if (inherit)
            {
                var providerHelper = new AzObjectSecurityProviderHelper(objectId, secObjProvider);
                while (providerHelper.NextInherit())
                {
                    allAces.AddRange(GetAcl(subject, action, AzObjectIdHelper.GetFullObjectId(providerHelper.CurrentObjectId)));
                }
                allAces.AddRange(GetAcl(subject, action));
            }

            var aces = new List<Ace>();
            var aclKeys = new List<string>();
            foreach (var ace in allAces)
            {
                var key = string.Format("{0}{1:D}", ace.ActionId, ace.Reaction);
                if (!aclKeys.Contains(key))
                {
                    aces.Add(ace);
                    aclKeys.Add(key);
                }
            }

            return aces;
        }

        public ASC.Common.Security.Authorizing.Action GetActionBySysName(string sysname)
        {
            throw new NotImplementedException();
        }



        #endregion

        private List<Ace> GetAcl(ISubject subject, IAction action, string fullObjectId)
        {
            var acl = new List<Ace>();
            foreach (var entry in permRecords)
            {
                var pr = entry.Value;
                if (pr.SubjectId == subject.ID && pr.ActionId == action.ID && pr.ObjectId == fullObjectId)
                {
                    acl.Add(new Ace(action.ID, pr.Reaction));
                }
            }
            return acl;
        }

        class PermissionRecord
        {
            public string Id;

            public Guid SubjectId;
            public Guid ActionId;
            public string ObjectId;
            public AceType Reaction;

            public PermissionRecord(Guid subjectId, Guid actionId, string objectId, AceType reaction)
            {
                SubjectId = subjectId;
                ActionId = actionId;
                ObjectId = objectId;
                Reaction = reaction;
                Id = string.Format("{0}{1}{2}{3:D}", SubjectId, ActionId, ObjectId, Reaction);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is PermissionRecord p && Id == p.Id;
            }
        }
    }
}
#endif