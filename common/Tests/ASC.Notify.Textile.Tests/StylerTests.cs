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

#if (DEBUG)
namespace ASC.Notify.Textile
{
    using ASC.Core;
    using ASC.Notify.Messages;
    using ASC.Security.Cryptography;

    using Microsoft.Extensions.Configuration;

    using NUnit.Framework;

    [TestFixture]
    public class StylerTests
    {
        private CoreBaseSettings CoreBaseSettings { get; set; }
        private IConfiguration Configuration { get; set; }
        private InstanceCrypto InstanceCrypto { get; set; }

        private readonly string pattern = "h1.New Post in Forum Topic: \"==Project(%: \"Sample Title\"==\":\"==http://sssp.teamlab.com==\"" + System.Environment.NewLine +
            "25/1/2022 \"Jim\":\"http://sssp.teamlab.com/myp.aspx\"" + System.Environment.NewLine +
            "has created a new post in topic:" + System.Environment.NewLine +
            "==<b>- The text!&nbsp;</b>==" + System.Environment.NewLine +
            "\"Read More\":\"http://sssp.teamlab.com/forum/post?id=4345\"" + System.Environment.NewLine +
            "Your portal address: \"http://sssp.teamlab.com\":\"http://teamlab.com\" " + System.Environment.NewLine +
            "\"Edit subscription settings\":\"http://sssp.teamlab.com/subscribe.aspx\"";

        [Test]
        public void TestJabberStyler()
        {
            var message = new NoticeMessage() { Body = pattern };
            new JabberStyler().ApplyFormating(message);
        }

        [Test]
        public void TestTextileStyler()
        {
            var message = new NoticeMessage() { Body = pattern };
            new TextileStyler(CoreBaseSettings, Configuration, InstanceCrypto, null).ApplyFormating(message);
        }
    }
}
#endif