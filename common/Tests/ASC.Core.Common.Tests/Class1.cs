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
using ASC.Common.Security.Authorizing;

namespace ASC.Common.Tests.Security.Authorizing
{

    public class Class1
    {

        public int Id
        {
            get;
            set;
        }

        public Class1() { }

        public Class1(int id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is Class1 class1 && Equals(class1.Id, Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class Class1SecurityProvider : ISecurityObjectProvider
    {

        private readonly Type type1 = typeof(Class1);

        #region ISecurityObjectProvider Members

        public bool InheritSupported
        {
            get { return true; }
        }

        public ISecurityObjectId InheritFrom(ISecurityObjectId objectId)
        {
            if (objectId.ObjectType == type1)
            {
                if (objectId.SecurityId.Equals(2)) return new SecurityObjectId(1, type1);
            }

            return null;
        }

        public bool ObjectRolesSupported
        {
            get { return true; }
        }

        public IEnumerable<IRole> GetObjectRoles(ISubject account, ISecurityObjectId objectId, SecurityCallContext callContext)
        {
            var roles = new List<IRole>();

            if (objectId.ObjectType == type1)
            {
                if (objectId.SecurityId.Equals(1) && account.Equals(Domain.accountNik))
                {
                    roles.Add(Constants.Owner);
                    roles.Add(Constants.Self);
                }
                if (objectId.SecurityId.Equals(3) && account.Equals(Domain.accountAnton))
                {
                    roles.Add(Constants.Owner);
                }
            }

            return roles;
        }

        #endregion
    }
}
#endif