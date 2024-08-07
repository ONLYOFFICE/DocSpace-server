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

/*
#if DEBUG
namespace ASC.Core.Common.Tests
{
    using System;
    using System.Linq;

    using ASC.Core.Data;
    using ASC.Core.Tenants;
    using ASC.Core.Users;
    using ASC.Security.Cryptography;

    using NUnit.Framework;

    [TestFixture]
    public class DbTenantServiceTest : DbBaseTest<DbTenantService>
    {
        private readonly EFUserService userService;


        public DbTenantServiceTest()
        {
            userService = new EFUserService();
        }


        //[ClassInitialize]
        public void ClearData()
        {
            foreach (var t in Service.GetTenants(default))
            {
                if (t.Name == "nct5nct5" || t.Name == "google5" || t.TenantId == Tenant) Service.RemoveTenant(t.TenantId);
            }
            foreach (var u in userService.GetUsers(Tenant, default))
            {
                userService.RemoveUser(Tenant, u.Value.ID, true);
            }
            Service.SetTenantSettings(Tenant, "key1", null);
            Service.SetTenantSettings(Tenant, "key2", null);
            Service.SetTenantSettings(Tenant, "key3", null);
        }

        [Test]
        public void TenantTest()
        {
            var t1 = new Tenant("nct5nct5");
            var t2 = new Tenant("google5") { MappedDomain = "domain" };
            t2.TrustedDomains.Add(null);
            t2.TrustedDomains.Add("microsoft");

            Service.SaveTenant(null, t1);
            Service.SaveTenant(null, t2);

            var tenants = Service.GetTenants(default);
            CollectionAssert.Contains(tenants.ToList(), t1);
            CollectionAssert.Contains(tenants.ToList(), t2);

            var t = Service.GetTenant(t1.TenantId);
            CompareTenants(t, t1);

            t1.Version = 2;
            Service.SaveTenant(null, t1);
            t = Service.GetTenant(t1.TenantId);
            CompareTenants(t, t1);

            Assert.AreEqual(0, t.TrustedDomains.Count);
            CollectionAssert.AreEquivalent(new[] { "microsoft" }, Service.GetTenant(t2.TenantId).TrustedDomains);

            Service.RemoveTenant(t1.TenantId);
            Assert.IsNull(Service.GetTenant(t1.TenantId));

            Service.RemoveTenant(t2.TenantId);
            Assert.IsNull(Service.GetTenant(t2.MappedDomain));


            t1 = new Tenant("nct5nct5");
            Service.SaveTenant(null, t1);

            var user = new UserInfo
            {
                UserName = "username",
                FirstName = "first name",
                LastName = "last name",
                Email = "user@mail.ru"
            };
            userService.SaveUser(t1.TenantId, user);

            var password = "password";
            //userService.SetUserPassword(t1.TenantId, user.ID, password);

            tenants = Service.GetTenants(user.Email, Hasher.Base64Hash(password, HashAlg.SHA256));
            CollectionAssert.AreEqual(new[] { t1 }, tenants.ToList());

            tenants = Service.GetTenants(user.Email, null);
            CollectionAssert.Contains(tenants.ToList(), t1);

            Service.RemoveTenant(t1.TenantId);
            tenants = Service.GetTenants(user.Email, Hasher.Base64Hash(password, HashAlg.SHA256));
            Assert.AreEqual(0, tenants.Count());

            userService.RemoveUser(Tenant, user.ID, true);
        }

        [Test]
        public void ValidateDomain()
        {
            ValidateDomain("12345", typeof(TenantTooShortException));
            ValidateDomain("123456", null);
            ValidateDomain("трала   лалала", typeof(TenantIncorrectCharsException));
            ValidateDomain("abc.defg", typeof(TenantIncorrectCharsException));
            ValidateDomain("abcdef", null);

            var t = new Tenant("nct5nct5") { MappedDomain = "nct5nct6" };
            t = Service.SaveTenant(null, t);
            ValidateDomain("nct5nct5", typeof(TenantAlreadyExistsException));
            ValidateDomain("NCT5NCT5", typeof(TenantAlreadyExistsException));
            ValidateDomain("nct5nct6", typeof(TenantAlreadyExistsException));
            ValidateDomain("NCT5NCT6", typeof(TenantAlreadyExistsException));
            ValidateDomain("nct5nct7", null);
            try
            {
                Service.ValidateDomain("nct5nct5");
            }
            catch (TenantAlreadyExistsException e)
            {
                CollectionAssert.AreEquivalent(e.ExistsTenants.ToList(), new[] { "nct5nct5", "nct5nct6" });
            }

            t.MappedDomain = "abc.defg";
            Service.SaveTenant(null, t);
            Service.RemoveTenant(Tenant);
        }

        [Test]
        public void TenantSettings()
        {
            Service.SetTenantSettings(Tenant, "key1", null);
            Assert.IsNull(Service.GetTenantSettings(Tenant, "key1"));

            Service.SetTenantSettings(Tenant, "key2", new byte[] { });
            Assert.IsNull(Service.GetTenantSettings(Tenant, "key2"));

            var data = new byte[] { 0 };
            Service.SetTenantSettings(Tenant, "key3", data);
            CollectionAssert.AreEquivalent(data, Service.GetTenantSettings(Tenant, "key3"));

            Service.SetTenantSettings(Tenant, "key3", null);
            Assert.IsNull(Service.GetTenantSettings(Tenant, "key3"));
        }

        private void CompareTenants(Tenant t1, Tenant t2)
        {
            Assert.AreEqual(t1.Language, t2.Language);
            Assert.AreEqual(t1.MappedDomain, t2.MappedDomain);
            Assert.AreEqual(t1.Name, t2.Name);
            Assert.AreEqual(t1.OwnerId, t2.OwnerId);
            Assert.AreEqual(t1.PartnerId, t2.PartnerId);
            Assert.AreEqual(t1.PaymentId, t2.PaymentId);
            Assert.AreEqual(t1.Status, t2.Status);
            Assert.AreEqual(t1.TenantAlias, t2.TenantAlias);
            Assert.AreEqual(t1.GetTenantDomain(null), t2.GetTenantDomain(null));
            Assert.AreEqual(t1.TenantId, t2.TenantId);
            Assert.AreEqual(t1.TrustedDomains, t2.TrustedDomains);
            Assert.AreEqual(t1.TrustedDomainsType, t2.TrustedDomainsType);
            Assert.AreEqual(t1.TimeZone, t2.TimeZone);
            Assert.AreEqual(t1.Version, t2.Version);
            Assert.AreEqual(t1.VersionChanged, t2.VersionChanged);
        }

        private void ValidateDomain(string domain, Type expectException)
        {
            try
            {
                Service.ValidateDomain(domain);
            }
            catch (Exception ex)
            {
                if (expectException == null || !ex.GetType().Equals(expectException)) throw;
            }
        }
    }
}
#endif
*/