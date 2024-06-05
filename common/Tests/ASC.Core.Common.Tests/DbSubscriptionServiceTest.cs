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
    using System.Linq;

    using ASC.Core.Data;

    using NUnit.Framework;

    [TestFixture]
    class DbSubscriptionServiceTest : DbBaseTest<DbSubscriptionService>
    {
        [OneTimeSetUp]
        public void ClearData()
        {
            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = this.Tenant, Source = "sourceId", Action = "actionId", Recipient = "recipientId", });
            Service.RemoveSubscriptions(Tenant, "sourceId", "actionId");
            Service.RemoveSubscriptions(Tenants.Tenant.DefaultTenant, "Good", "Bad", "Ugly");
            Service.RemoveSubscriptions(this.Tenant, "Good", "Bad", "Ugly");
            Service.RemoveSubscriptions(this.Tenant, "Good", "Bad", "NotUgly");
            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = this.Tenant, Source = "Good", Action = "Bad", Recipient = "Rec1", Methods = null });
            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = Tenants.Tenant.DefaultTenant, Source = "Good", Action = "Bad", Recipient = "Rec1", Methods = null });
        }

        [Test]
        public void SubscriptionMethod()
        {
            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = this.Tenant, Source = "sourceId", Action = "actionId", Recipient = "recipientId", Methods = new[] { "email.sender" } });
            var m = Service.GetSubscriptionMethods(Tenant, "sourceId", "actionId", "recipientId").First();
            Assert.AreEqual(m.Tenant, Tenant);
            Assert.AreEqual(m.Source, "sourceId");
            Assert.AreEqual(m.Action, "actionId");
            Assert.AreEqual(m.Recipient, "recipientId");
            CollectionAssert.AreEquivalent(new[] { "email.sender" }, m.Methods);

            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = this.Tenant, Source = "sourceId", Action = "actionId", Recipient = "recipientId", Methods = null });
            Assert.IsNull(Service.GetSubscriptionMethods(Tenant, "sourceId", "actionId", "recipientId").FirstOrDefault());

            Service.SaveSubscription(new SubscriptionRecord { Tenant = this.Tenant, SourceId = "sourceId", ActionId = "actionId", ObjectId = "object1Id", RecipientId = "recipientId", Subscribed = false });
            Service.SaveSubscription(new SubscriptionRecord { Tenant = this.Tenant, SourceId = "sourceId", ActionId = "actionId", ObjectId = "object2Id", RecipientId = "recipientId", Subscribed = true });
            var subs = Service.GetSubscriptions(Tenant, "sourceId", "actionId", "recipientId", null);
            Assert.AreEqual(subs.Count(), 2);
            subs = Service.GetSubscriptions(Tenant, "sourceId", "actionId", null, "object1Id");
            Assert.AreEqual(subs.Count(), 1);
            subs = Service.GetSubscriptions(Tenant, "sourceId", "actionId", null, "object1Id");
            Assert.AreEqual(subs.Count(), 1);

            Service.RemoveSubscriptions(Tenant, "sourceId", "actionId");
            subs = Service.GetSubscriptions(Tenant, "sourceId", "actionId", "recipientId", null);
            Assert.AreEqual(0, subs.Count());

            Service.SaveSubscription(new SubscriptionRecord { Tenant = this.Tenant, SourceId = "sourceId", ActionId = "actionId", ObjectId = "objectId", RecipientId = "recipientId", Subscribed = true });
            Service.RemoveSubscriptions(Tenant, "sourceId", "actionId", "objectId");
            subs = Service.GetSubscriptions(Tenant, "sourceId", "actionId", "recipientId", null);
            Assert.AreEqual(0, subs.Count());

            Service.SaveSubscription(new SubscriptionRecord { Tenant = Tenants.Tenant.DefaultTenant, SourceId = "Good", ActionId = "Bad", RecipientId = "Rec1", ObjectId = "Ugly", Subscribed = true });
            subs = Service.GetSubscriptions(this.Tenant, "Good", "Bad", null, "Ugly");
            Assert.AreEqual(subs.Count(), 1);

            Service.SaveSubscription(new SubscriptionRecord { Tenant = Tenants.Tenant.DefaultTenant, SourceId = "Good", ActionId = "Bad", RecipientId = "Rec2", ObjectId = "Ugly", Subscribed = true });
            subs = Service.GetSubscriptions(this.Tenant, "Good", "Bad", null, "Ugly");
            Assert.AreEqual(subs.Count(), 2);

            Service.SaveSubscription(new SubscriptionRecord { Tenant = this.Tenant, SourceId = "Good", ActionId = "Bad", RecipientId = "Rec2", ObjectId = "Ugly", Subscribed = true });
            subs = Service.GetSubscriptions(this.Tenant, "Good", "Bad", null, "Ugly");
            Assert.AreEqual(subs.Count(), 2);

            Service.SaveSubscription(new SubscriptionRecord { Tenant = this.Tenant, SourceId = "Good", ActionId = "Bad", RecipientId = "Rec3", ObjectId = "NotUgly", Subscribed = true });
            subs = Service.GetSubscriptions(this.Tenant, "Good", "Bad", null, "Ugly");
            Assert.AreEqual(subs.Count(), 2);

            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = Tenants.Tenant.DefaultTenant, Source = "Good", Action = "Bad", Recipient = "Rec1", Methods = new[] { "s1" } });
            Service.SetSubscriptionMethod(new SubscriptionMethod { Tenant = this.Tenant, Source = "Good", Action = "Bad", Recipient = "Rec1", Methods = new[] { "s2" } });
            var methods = Service.GetSubscriptionMethods(this.Tenant, "Good", "Bad", "Rec1");
            Assert.AreEqual(methods.Count(), 1);
            CollectionAssert.AreEquivalent(new[] { "s2" }, methods.ToArray()[0].Methods);
        }
    }
}
#endif
