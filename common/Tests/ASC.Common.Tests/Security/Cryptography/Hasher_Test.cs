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
using System.Security.Cryptography;
using System.Text;

using ASC.Security.Cryptography;

using NUnit.Framework;

namespace ASC.Common.Tests.Security.Cryptography
{
    [TestFixture]
    public class Hasher_Test
    {
        [Test]
        public void DoHash()
        {
            var str = "Hello, Jhon!";

            using var md5 = MD5.Create();
            Assert.AreEqual(
                Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(str))),
                Hasher.Base64Hash(str, HashAlg.MD5)
                );

            using var sha1 = SHA1.Create();
            Assert.AreEqual(
               Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(str))),
               Hasher.Base64Hash(str, HashAlg.SHA1)
               );

            using var sha256 = SHA256.Create();
            Assert.AreEqual(
               Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(str))),
               Hasher.Base64Hash(str, HashAlg.SHA256)
               );

            using var sha512 = SHA512.Create();
            Assert.AreEqual(
               Convert.ToBase64String(sha512.ComputeHash(Encoding.UTF8.GetBytes(str))),
               Hasher.Base64Hash(str, HashAlg.SHA512)
               );

            Assert.AreEqual(
              Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(str))),
              Hasher.Base64Hash(str) //DEFAULT
              );
        }
    }
}
#endif