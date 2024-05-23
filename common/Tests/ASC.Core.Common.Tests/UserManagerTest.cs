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
namespace ASC.Core.Common.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using ASC.Core.Users;

    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;

    [TestFixture]
    public class UserManagerTest
    {
        IServiceProvider ServiceProvider { get; set; }

        [Test]
        public void SearchUsers()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var tenant = tenantManager.SetCurrentTenant(0);

            var users = userManager.Search(null, EmployeeStatus.Active);
            Assert.AreEqual(0, users.Length);

            users = userManager.Search("", EmployeeStatus.Active);
            Assert.AreEqual(0, users.Length);

            users = userManager.Search("  ", EmployeeStatus.Active);
            Assert.AreEqual(0, users.Length);

            users = userManager.Search("АбРаМсКй", EmployeeStatus.Active);
            Assert.AreEqual(0, users.Length);

            users = userManager.Search("АбРаМсКий", EmployeeStatus.Active);
            Assert.AreEqual(0, users.Length);//Абрамский уволился

            users = userManager.Search("АбРаМсКий", EmployeeStatus.All);
            Assert.AreNotEqual(0, users.Length);

            users = userManager.Search("иванов николай", EmployeeStatus.Active);
            Assert.AreNotEqual(0, users.Length);

            users = userManager.Search("ведущий програм", EmployeeStatus.Active);
            Assert.AreNotEqual(0, users.Length);

            users = userManager.Search("баннов лев", EmployeeStatus.Active, new Guid("613fc896-3ddd-4de1-a567-edbbc6cf1fc8"));
            Assert.AreNotEqual(0, users.Length);

            users = userManager.Search("иванов николай", EmployeeStatus.Active, new Guid("613fc896-3ddd-4de1-a567-edbbc6cf1fc8"));
            Assert.AreEqual(0, users);
        }

        [Test]
        public void DepartmentManagers()
        {
            using var scope = ServiceProvider.CreateScope();
            var scopeClass = scope.ServiceProvider.GetService<UserManagerTestScope>();
            var (userManager, tenantManager) = scopeClass;
            var tenant = tenantManager.SetCurrentTenant(1024);

            var deps = userManager.GetDepartments();
            var users = userManager.GetUsers();

            var g1 = deps[0];
            var ceo = users[0];
            var u1 = users[1];
            var u2 = users[2];
            userManager.GetCompanyCEO();
            userManager.SetCompanyCEO(ceo.Id);
            var ceoTemp = userManager.GetCompanyCEO();
            Assert.AreEqual(ceo, ceoTemp);

            Thread.Sleep(TimeSpan.FromSeconds(6));
            ceoTemp = userManager.GetCompanyCEO();
            Assert.AreEqual(ceo, ceoTemp);

            userManager.SetDepartmentManager(g1.ID, u1.Id);

            userManager.SetDepartmentManager(g1.ID, u2.Id);
        }

        [Test]
        public void UserGroupsPerformanceTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var scopeClass = scope.ServiceProvider.GetService<UserManagerTestScope>();
            (var userManager, var tenantManager) = scopeClass;
            var tenant = tenantManager.SetCurrentTenant(0);

            foreach (var u in userManager.GetUsers())
            {
                var groups = userManager.GetGroups(Guid.Empty);
                Assert.IsNotNull(groups);
                foreach (var g in userManager.GetUserGroups(u.Id))
                {
                    var manager = userManager.GetUsers(userManager.GetDepartmentManager(g.ID)).UserName;
                }
            }
            var stopwatch = Stopwatch.StartNew();
            foreach (var u in userManager.GetUsers())
            {
                var groups = userManager.GetGroups(Guid.Empty);
                Assert.IsNotNull(groups);
                foreach (var g in userManager.GetUserGroups(u.Id))
                {
                    var manager = userManager.GetUsers(userManager.GetDepartmentManager(g.ID)).UserName;
                }
            }
            stopwatch.Stop();

            stopwatch.Restart();
            var users = userManager.GetUsersByGroup(Constants.GroupUser.ID);
            var visitors = userManager.GetUsersByGroup(Constants.GroupVisitor.ID);
            var all = userManager.GetUsers();
            Assert.IsNotNull(users);
            Assert.IsNotNull(visitors);
            Assert.IsNotNull(all);
            stopwatch.Stop();
        }
    }

    public class UserManagerTestScope
    {
        private UserManager UserManager { get; }
        private TenantManager TenantManager { get; }

        public UserManagerTestScope(UserManager userManager, TenantManager tenantManager)
        {
            UserManager = userManager;
            TenantManager = tenantManager;
        }

        public void Deconstruct(out UserManager userManager, out TenantManager tenantManager)
        {
            userManager = UserManager;
            tenantManager = TenantManager;
        }
    }
}
#endif
