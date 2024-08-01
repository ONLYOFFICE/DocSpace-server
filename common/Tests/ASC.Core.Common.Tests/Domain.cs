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

using ASC.Common.Security.Authentication;
using ASC.Common.Security.Authorizing;

using AzAction = ASC.Common.Security.Authorizing.Action;

namespace ASC.Common.Tests.Security.Authorizing
{

    static class Domain
    {
        public static readonly Guid accountAlientID = Guid.NewGuid();
        public static readonly IAccount accountAlient = new UserAccount(accountAlientID, "Djohn Doo");

        public static readonly Guid accountLevID = Guid.NewGuid();
        public static readonly IAccount accountLev = new UserAccount(accountLevID, "Lev");

        public static readonly Guid accountAntonID = Guid.NewGuid();
        public static readonly IAccount accountAnton = new UserAccount(accountAntonID, "anton");

        public static readonly Guid accountNikID = Guid.NewGuid();
        public static readonly IAccount accountNik = new UserAccount(accountNikID, "nikolay");

        public static readonly Guid accountValeryID = Guid.NewGuid();
        public static readonly IAccount accountValery = new UserAccount(accountValeryID, "Schumaher");

        public static readonly Guid accountKatID = Guid.NewGuid();
        public static readonly IAccount accountKat = new UserAccount(accountKatID, "Kat");

        public static readonly Guid accountMessangerServiceID = Guid.NewGuid();
        public static readonly IAccount accountMessangerService = new AccountS(accountMessangerServiceID, "Messanger Service");


        public static readonly Guid roleAVSID = Guid.NewGuid();
        public static readonly IRole roleAVS = new Role(roleAVSID, "AVS");

        public static readonly Guid roleAdministrationID = Guid.NewGuid();
        public static readonly IRole roleAdministration = new Role(roleAdministrationID, "administration");

        public static readonly Guid roleNETID = Guid.NewGuid();
        public static readonly IRole roleNET = new Role(roleNETID, ".NET Deparment");

        public static readonly Guid roleHRID = Guid.NewGuid();
        public static readonly IRole roleHR = new Role(roleHRID, "hr");


        public static readonly RoleFactory RoleProvider = new RoleFactory();

        public static readonly Guid actionAddUserID = Guid.NewGuid();
        public static readonly AzAction actionAddUser = new AzAction(actionAddUserID, "add user");

        public static readonly Guid actionDeleteUserID = Guid.NewGuid();
        public static readonly AzAction actionDeleteUser = new AzAction(actionDeleteUserID, "delete user");

        public static readonly Guid actionChangeSelfContactInfoID = Guid.NewGuid();
        public static readonly AzAction actionChangeSelfContactInfo = new AzAction(actionChangeSelfContactInfoID, "change self contact info");

        public static readonly Guid actionViewInfoID = Guid.NewGuid();
        public static readonly AzAction actionViewInfo = new AzAction(actionViewInfoID, "view user info");

        public static readonly Guid categoryUserManagerID = Guid.NewGuid();
        public static readonly IAction[] categoryUserManager = { actionAddUser, actionDeleteUser, actionChangeSelfContactInfo, actionViewInfo };

        public static readonly Guid actionHREditInfoID = Guid.NewGuid();
        public static readonly AzAction actionHREditInfo = new AzAction(actionHREditInfoID, "edit HR info");

        public static readonly Guid actionHRViewInfoID = Guid.NewGuid();
        public static readonly AzAction actionHRViewInfo = new AzAction(actionHRViewInfoID, "view HR info");

        public static readonly Guid categoryHRID = Guid.NewGuid();
        public static readonly IAction[] categoryHR = new[] { actionHREditInfo, actionHRViewInfo };

        public static readonly Guid actionNETCommitID = Guid.NewGuid();
        public static readonly AzAction actionNETCommit = new AzAction(actionNETCommitID, "SVN Commit");

        public static readonly Guid actionNETViewID = Guid.NewGuid();
        public static readonly AzAction actionNETView = new AzAction(actionNETViewID, "SVN View");

        public static readonly Guid categoryNETID = Guid.NewGuid();
        public static readonly IAction[] categoryNET = new[] { actionNETCommit, actionNETView };

        public static readonly PermissionFactory PermissionProvider = new PermissionFactory();


        static Domain()
        {
            //++ explicit allow, + ancestor of ++
            //-- explicit deny, - ancestor of --
            //aa - explicit audit, a ancestor of aa

            //                           action

            //Owner						  ++

            //Self						  ++

            //EveryOne                     

            //User                                  

            //roleAVS                     ++        

            //    roleHR                   +aa         
            //        accountKat           +a          
            //        accountLev           -a        

            //    roleNET                  +         
            //        accountAnton         +         
            //        accountNik           +         
            //        accountValery       --         

            //    roleAdministration      --          
            //        accountLev           -         

            //    accountLev               -a          
            //    accountAnton             +          
            //    accountNik               +          
            //    accountValery            -         
            //    accountKat               +a          

            RoleProvider.AddAccountInRole(accountLev, roleAVS);
            RoleProvider.AddAccountInRole(accountAnton, roleAVS);
            RoleProvider.AddAccountInRole(accountNik, roleAVS);
            RoleProvider.AddAccountInRole(accountValery, roleAVS);
            RoleProvider.AddAccountInRole(accountKat, roleAVS);

            RoleProvider.AddAccountInRole(roleHR, roleAVS);
            RoleProvider.AddAccountInRole(roleNET, roleAVS);
            RoleProvider.AddAccountInRole(roleAdministration, roleAVS);

            RoleProvider.AddAccountInRole(accountLev, roleAdministration);

            RoleProvider.AddAccountInRole(accountKat, roleHR);
            RoleProvider.AddAccountInRole(accountLev, roleHR);

            RoleProvider.AddAccountInRole(accountAnton, roleNET);
            RoleProvider.AddAccountInRole(accountNik, roleNET);
            RoleProvider.AddAccountInRole(accountValery, roleNET);

            PermissionProvider.AddAce(Domain.roleAVS, actionAddUser, AceType.Allow);
            PermissionProvider.AddAce(Domain.roleAdministration, actionAddUser, AceType.Deny);
            PermissionProvider.AddAce(Domain.accountValery, actionAddUser, AceType.Deny);

            PermissionProvider.AddAce(Constants.Owner, actionAddUser, AceType.Allow);
            PermissionProvider.AddAce(Constants.Self, actionAddUser, AceType.Allow);
        }
    }
}
#endif