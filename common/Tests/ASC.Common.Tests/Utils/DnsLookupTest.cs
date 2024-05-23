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
namespace ASC.Common.Tests.Utils
{
    using System;
    using System.Linq;

    using ASC.Common.Utils;

    using NUnit.Framework;

    [TestFixture]
    public class DnsLookupTest
    {
        [Test]
        public void DomainNameEmptyExists()
        {
            const string domain = "";

            var dnsLoopup = new DnsLookup();

            Assert.Throws<ArgumentException>(() => dnsLoopup.IsDomainExists(domain), "domainName");
        }

        [Test]
        public void DomainNameInvalidExists()
        {
            const string domain = "/.";

            var dnsLoopup = new DnsLookup();

            Assert.Throws<ArgumentException>(() => dnsLoopup.IsDomainExists(domain), "Domain name could not be parsed");
        }

        [Test]
        public void DomainExists()
        {
            const string domain = "onlyoffice.com";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainExists(domain);

            Assert.IsTrue(exists);
        }

        [Test]
        public void DomainNotExists()
        {
            const string domain = "sdkjskytt111hdhdhwooo.ttt";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainExists(domain);

            Assert.IsFalse(exists);
        }

        [Test]
        public void MxExists()
        {
            const string domain = "onlyoffice.com";
            const string mx_record = "mx1.onlyoffice.com";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainMxRecordExists(domain, mx_record);

            Assert.IsTrue(exists);
        }

        [Test]
        public void DkimExists()
        {
            const string domain = "onlyoffice.com";
            const string dkim_record = "v=DKIM1; p=MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDiblqlVejxACSfc3Y0OzRzyFFtnUHgkw65k+QGjG4WvjmJJQNcfdJNaaLo9xKPIfw9vTRVigZa78KgeYFymGlqXtR0z323EwiHaNh82Qo1oBICOZT2AVjWpPjBUGwD6qTorulmLnY9+YKn1bV8B7mt964ewpPHDDsqaHddhV7hqQIDAQAB";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainDkimRecordExists(domain, "dkim", dkim_record);

            Assert.IsTrue(exists);
        }

        [Test]
        public void TxtSpfExists()
        {
            const string domain = "onlyoffice.com";
            const string txt_record = "v=spf1 a mx mx:avsmedia.net a:smtp1.uservoice.com a:qamail.teamlab.info include:amazonses.com -all";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainTxtRecordExists(domain, txt_record);

            Assert.IsTrue(exists);
        }

        [Test]
        public void GetMxRecords()
        {
            const string domain = "onlyoffice.com";

            var dnsLoopup = new DnsLookup();

            var mxRecords = dnsLoopup.GetDomainMxRecords(domain);

            Assert.IsTrue(mxRecords.Any());
        }

        [Test]
        public void GetARecords()
        {
            const string domain = "onlyoffice.com";

            var dnsLoopup = new DnsLookup();

            var aRecords = dnsLoopup.GetDomainARecords(domain);

            Assert.IsTrue(aRecords.Any());
        }

        [Test]
        public void GetIPs()
        {
            const string domain = "onlyoffice.com";

            var dnsLoopup = new DnsLookup();

            var ips = dnsLoopup.GetDomainIPs(domain);

            Assert.IsTrue(ips.Any());
        }

        [Test]
        public void GetPtr()
        {
            const string domain = "mx1.onlyoffice.com";
            const string ip = "54.244.95.25";

            var dnsLoopup = new DnsLookup();

            var exists = dnsLoopup.IsDomainPtrRecordExists(ip, domain);

            Assert.IsTrue(exists);
        }


        [Test]
        public void GetNoexistedDomainMx()
        {
            const string domain = "taramparam.tk";

            var dnsLoopup = new DnsLookup();

            var mxRecords = dnsLoopup.GetDomainMxRecords(domain);

            Assert.IsTrue(!mxRecords.Any());
        }
    }
}
#endif